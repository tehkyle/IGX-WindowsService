using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Collections.Concurrent;
using System.Diagnostics;
using Ingeniux.CMS;
using System.Data.SqlClient;
using System.Xml;

namespace Ingeniux.Service
{
	public class ActionProperties
	{
		public string contentStoreUrl;
		public string xmlPath;
		public string userId;
		public EventLog log;
		public SqlConnection connection;
		public string commandStr;
	}

	public class SqlDoc
	{
		public string id;
		public string name;
		public string desc;
		public string entityId = "";
	}

	public class SyncPackage
	{
		public IEnumerable<string> categoryIdsToUpdate;
		public IEnumerable<string> categoryIdsToRemove;
		public IEnumerable<SqlDoc> docsToAdd;
	}

    public class IgxActions : IDisposable
    {
		ContentStore Store;
		IReadonlyUser User;
		SqlConnection Connection;
		string CommandString;
		EventLog log;
		private string RootCategoryId;

		public IgxActions(ActionProperties props)
		{
			log = props.log;
			try
			{
				Store = new ContentStore(props.contentStoreUrl, props.xmlPath);
				User = Store.GetStartingUser(props.userId);
				Connection = props.connection;
				CommandString = props.commandStr;
			}
			catch (Exception e)
			{
				log.WriteEntry(string.Format("Error initializing action class: {0} -- {1}", e.Message, e.StackTrace), EventLogEntryType.Error);
				throw e;
			}
		}

		public void InitSql(ActionProperties props)
		{
			Connection = props.connection;
			CommandString = props.commandStr;
		}

		public void ExecuteTaxonomySync()
		{
			List<SqlDoc> updatedDocs = new List<SqlDoc>();
			if (Connection != null && !string.IsNullOrWhiteSpace(CommandString))
				updatedDocs = RefetchSqlQuery();

			using (var session = Store.OpenWriteSession(User))
			{
				log.WriteEntry(string.Format("Sql Docs: {0}", updatedDocs.Select(d => d.name).Join(", ")));
				CreateSqlRootCategory();
				SyncPackage package = GenerateSyncPackage(updatedDocs);
				if (CategorySync(package, updatedDocs))
					log.WriteEntry(string.Format("Sql Sync Completed"));
				else
					log.WriteEntry(string.Format("Sql Sync Failed"));
			}
		}

		public List<SqlDoc> RefetchSqlQuery()
		{
			List<SqlDoc> updatedDocs = new List<SqlDoc>();

			using (SqlCommand command = new SqlCommand(CommandString, Connection))
			{
				SqlDataReader reader = command.ExecuteReader();
				if (reader.HasRows)
					while (reader.Read())
					{
						var newDoc = new SqlDoc()
						{
							id = reader.GetString(0),
							name = reader.GetString(1),
							desc = reader.GetString(2)
						};
						updatedDocs.Add(newDoc);
					}
			}

			return updatedDocs;
		}
		
		/// <summary>
		/// In this example we use the Taxonomy Manager (on the session) to look for a root
		/// category that has the name "SQL" that we will use for our category sync. This will
		/// require fetching all the root categories to query through but means we don't need to
		/// hard-code any category IDs in our service.
		/// </summary>
		public void CreateSqlRootCategory() // Manager-Entity example
		{
			using (var session = Store.OpenWriteSession(User))
			{
				int count;
				// Collect all root categories and look for one called "SQL" for use in the sync
				ICategoryNode rootSqlCategory = session.TaxonomyManager.RootCategories(out count)
					.Where(c => c.Name == "SQL").FirstOrDefault();

				// If there isn't one, create the entity
				if (rootSqlCategory == null)
					rootSqlCategory = session.TaxonomyManager.CreateRootCategory("SQL", "Categories synced from sql", "", "");

				// Save the ID, once this session is closed we don't have access to the object anymore and will need to refetch later.
				RootCategoryId = rootSqlCategory.Id;
			}
		}

		/// <summary>
		/// In this example we take a list of sql docs from our external DB and compare them against
		/// the taxonomy categories under the 'SQL' root using the 'ExternalID' values. From this we 
		/// build a collection of categories and sql doc objects that need updating, deleting, or adding.
		/// </summary>
		/// <param name="sqlDocs">Documents retrieved from SQL</param>
		/// /// <returns>A formatted sync package of ids to sync in a later session</returns>
		public SyncPackage GenerateSyncPackage(List<SqlDoc> sqlDocs)
		{
			SyncPackage package = new SyncPackage();

			HashSet<string> SqlDocDict = new HashSet<string>(sqlDocs.Select(doc => doc.id));

			int count;

			if (string.IsNullOrWhiteSpace(RootCategoryId))
				CreateSqlRootCategory();

			using (var session = Store.OpenReadSession(User))
			{
				ICategoryNode sqlRootCategory = session.TaxonomyManager.Category(RootCategoryId);
				if (sqlRootCategory != null)
				{
					IEnumerable<ICategoryNode> sqlCategories = sqlRootCategory.Children(out count);
					HashSet<string> existingExternalIds = new HashSet<string>(sqlCategories.Select(cat => cat.ExternalID));
					log.WriteEntry(string.Format("Sql Root Id: {0}", RootCategoryId));

					package.categoryIdsToUpdate = sqlCategories
						.Where(c => SqlDocDict.Contains(c.ExternalID))
						.Select(c => c.Id);
					package.categoryIdsToRemove = sqlCategories.Where(c => !SqlDocDict.Contains(c.ExternalID)).Select(c => c.Id);
					package.docsToAdd = sqlDocs.Where(doc => !existingExternalIds.Contains(doc.id));
				}
			}

			return package;
		}

		/// <summary>
		/// In this method we take the package generated earlier, and the list of source documents from SQL
		/// and perform the sync operation. There are three steps:
		/// 1) Delete deprecated categories - Use the lower level DeleteByIds so we don't need to refetch
		/// 2) Update the existing categories - Refetch these documents so their context is accurate
		/// 3) Add new categories - Using the manager, create new categories from the sql documents under the root
		/// </summary>
		/// <param name="package">Sync package we generated in GenerateSyncPackage()</param>
		/// <param name="sqlDocs">Documents retrieved from SQL</param>
		/// <returns>false if the package is null</returns>
		public bool CategorySync(SyncPackage package, List<SqlDoc> sqlDocs)
		{
			if (package == null)
				return false;

			Dictionary<string, SqlDoc> allDocs = sqlDocs.Distinct(doc => doc.id).ToDictionary(doc => doc.id);

			using (var session = Store.OpenWriteSession(User))
			{
				// Mark all the removal ids for deletion. Use lower level DeleteByIds to avoid re-fetching the delete categories
				(session.TaxonomyManager as TransactionalEntity).DeleteByIds(package.categoryIdsToRemove.ToArray());

				// Fetch the categories to update. We have to refetch by ID because of the new session scope.
				IEnumerable<ICategoryNode> categoriesToUpdate = session.TaxonomyManager.Categories(package.categoryIdsToUpdate.ToArray());
				foreach (ICategoryNode category in categoriesToUpdate)
				{
					SqlDoc sourceDoc = allDocs[category.ExternalID];
					category.Name = sourceDoc.name;
					category.Description = sourceDoc.desc;
				}

				// Create new categories for the new docs from sql
				ICategoryNode sqlRootCategory = session.TaxonomyManager.Category(RootCategoryId);
				foreach (SqlDoc newDoc in package.docsToAdd)
					session.TaxonomyManager.CreateCategory(newDoc.name, newDoc.desc, newDoc.id, "", sqlRootCategory);

				return true;
			}
		}

		public void CMSLoggingExample(string msg = "")
		{
			using (var session = Store.OpenReadSession(User))
			{
				if (string.IsNullOrWhiteSpace(msg))
					session.Debug("This is a debug-level log!");
				else
					session.Info(msg); // This one in info-level

				session.Log(NLog.LogLevel.Warn, "You can also use this method for passed in log levels.");
			}
		}

		public void IndexQueryExample()
		{
			using (var session = Store.OpenWriteSession(User))
			{

			}
		}

		// This example exhibits the behavior of nested sessions.
		public void NestedSessionExample()
		{
			// Following this...
			using (var session = Store.OpenWriteSession(User))
			{
				IPage siteRoot = session.Site.SiteRoot();
				siteRoot.Name = "First Session " + new Random().Next(1, 100);
				using (var innerSession = Store.OpenWriteSession(User))
				{
					IPage innerSiteRoot = innerSession.Site.SiteRoot();
					innerSiteRoot.Name = "Second Session " + new Random().Next(1, 100);
				}
			}

			// What is the site root's name now?
			using (var session = Store.OpenReadSession(User))
			{
				IPage siteRoot = session.Site.SiteRoot();
				log.WriteEntry(string.Format("The site name is: {0}", siteRoot.Name));
			}
		}

		public void Dispose()
		{
			Store.Dispose();
		}
	}
}