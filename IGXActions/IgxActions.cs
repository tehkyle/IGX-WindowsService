using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Data.SqlClient;
using Ingeniux.CMS;
using Raven.Client;

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

		/// <summary>
		/// In this example we show the two main ways to log to the CSAPI log. A session object must exist
		/// to log, and the four log levels in increasing severity are Debug, Info, Warn, and Error
		/// </summary>
		/// <param name="msg"></param>
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

		/// <summary>
		/// In this example we show how to use a transactional session to access the database. A session
		/// is the backbone to working with the CSAPI, following a transactional database model that 
		/// aligns 1-to-1 with the underlying Raven document database technology. Our session object
		/// wraps up the RavenDB DocumentSession.
		/// </summary>
		public void SessionExample()
		{
			int count;

			// A session is opened from the ContentStore and given a User identity.
			// We recommend enclosing it in a using block so the commit is handled automatically.
			using (var session = Store.OpenReadSession(User))
			{
				// Managers are ITransactionalEntity objects on the session that are used to do all
				// the work to manipulate the entities in the api: fetching, creating, deleting.
				IPage siteRoot = session.Site.SiteRoot();
				// Note you can get the current user from the session as well. OperatingUser == User in this case.
				var assignedCount = session.Site.PagesAssignedToUserCount(session.OperatingUser);

				// An IPage is an IEntity which corresponds 1-to-1 with a document in the database.
				// They can have additional relative functionality to fetch other documents.
				// Here we get the first 10 children of the siteRoot.
				// NOTE: an IEntity object fetched in the session uses that session to do further loading.
				//		-This means that if the session is closed the Entity can no longer perform database actions.
				IEnumerable<IPage> children = siteRoot.Children(out count, 10);

			}   // Here is where the session commits its changes.

			IUserGroup adminGroup;
			// Here we show what happens when you try to use an entity outside of its session.
			using (var session = Store.OpenReadSession(User))
			{
				adminGroup = session.UserManager.UserGroup(UserManager.ADMINISTRATOR_GROUP_ID);
				IEnumerable<IUser> users = adminGroup.Users(out count);
			}
			try
			{
				adminGroup.Users(out count);
			}
			catch (Exception e)
			{
				// The above call will throw an exception because the adminGroup entity is attempting
				// to use a session that has been closed.
			}

			string childId1;
			string childId2;
			// In this example we use a write session to create two folder pages in two separate
			// but methods with the same outcome.
			using (var session = Store.OpenWriteSession(User))
			{
				IPage siteRoot = session.Site.SiteRoot();
				ISchema schema = session.SchemasManager.SystemSchema(CMS.Enums.EnumSystemSchema.Folder);
				ISchema sameSchema = session.SchemasManager.SchemaByRootName("Folder");

				IPage child1 = siteRoot.CreateChildPage("New Page 1", schema);
				IPage child2 = session.Site.CreatePage(sameSchema, "New Page 2", siteRoot);

				// Save the ids of the created pages so we can fetch and use them in future sessions.
				childId1 = child1.Id;
				childId2 = child2.Id;
			}	// Let this session close to commit the new pages to the DB

			// Using a fresh new write session, fetch the pages we created and delete them in two
			// separate methods with the same result. A page remove is just a move to the recycle folder.
			using (var session = Store.OpenWriteSession(User))
			{
				IPage page1 = session.Site.Page(childId1);
				IPage page2 = session.Site.Page(childId2);
				IPage recycleFolder = session.Site.RecycleFolder();

				session.Site.RemovePage(ref page1);
				session.Site.MovePage(page2, recycleFolder, CMS.Enums.EnumCopyActions.IGX_MAKE_CHILD);
			}
		}

		/// <summary>
		/// In this example we will be using the ravenDB query api that is exposed on the session to
		/// retrieve the first 128 page Ids that have a text field that starts with the search text.
		/// </summary>
		/// <param name="searchText">The text to be searched for</param>
		public void RavenQueryExample(string searchText)
		{
			using (var session = Store.OpenWriteSession(User))
			{
				RavenQueryStatistics stats;
				string escapedTerm = string.Format("*{0}*", searchText.ToLowerInvariant());
				// Create the query
				var query = session.Query<PageFieldIndexableEntry, Ingeniux.CMS.RavenDB.Indexes.FullTextSearchIndex>()
					.Statistics(out stats)
					.Search(entry => entry.FieldValue, escapedTerm, 1, SearchOptions.And, EscapeQueryOptions.AllowAllWildcards) // Search must come before Where if the two are mixed.
					.Where(entry => entry.FieldType == CMS.Enums.EnumElementType.IGX_ELEMENT_TEXT);

				string queryStr = query.ToString();
				IEnumerable <string> results = query.ToArray().Select(result => result.PageId);
				string resultList = results.Join(", ");
				log.WriteEntry(string.Format("{0}: \n{1}", queryStr, resultList));
			}
		}

		/// <summary>
		/// This example exhibits the behavior of nested sessions.
		/// </summary>
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

		/// <summary>
		/// This is to fetch the contents of the sql command. It is done because the sql dependency event
		/// does not contain any of this information.
		/// </summary>
		/// <returns></returns>
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

		public void Dispose()
		{
			Store.Dispose();
		}
	}
}