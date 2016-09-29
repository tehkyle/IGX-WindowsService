using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ingeniux.Service;
using System.Diagnostics;
using System.Data.SqlClient;

namespace Tests
{
	[TestClass]
	public class ApiActionTests
	{
		string storeUrl = @"";
		string xmlPath = @"";
		string userId = @"";
		string connectionStr = @"";
		string commandStr = @"";

		IgxActions Actions;

		[TestMethod]
		public void MainTest()
		{
			EventLog log = new EventLog();
			if (!System.Diagnostics.EventLog.SourceExists("Ingeniux"))
				EventLog.CreateEventSource("Ingeniux", "IGXLog");
			log.Source = "Ingeniux";
			log.Log = "IGXLog";

			using (SqlConnection conn = new SqlConnection(connectionStr))
			{
				conn.Open();
				ActionProperties props = new ActionProperties()
				{
					contentStoreUrl = storeUrl,
					xmlPath = xmlPath,
					userId = userId,
					log = log,
					connection = conn,
					commandStr = commandStr
				};

				Actions = new IgxActions(props);
				
				Assert.IsNotNull(Actions);
				Actions.ExecuteTaxonomySync();
				conn.Close();
			}
		}

		[TestMethod]
		public void NestedSessionTest()
		{
			EventLog log = new EventLog();
			if (!System.Diagnostics.EventLog.SourceExists("Ingeniux"))
				EventLog.CreateEventSource("Ingeniux", "IGXLog");
			log.Source = "Ingeniux";
			log.Log = "IGXLog";
			
			ActionProperties props = new ActionProperties()
			{
				contentStoreUrl = storeUrl,
				xmlPath = xmlPath,
				userId = userId,
				log = log
			};

			Actions = new IgxActions(props);

			Assert.IsNotNull(Actions);

			Actions.NestedSessionExample();
		}

		[TestMethod]
		public void RavenQueryTest()
		{
			EventLog log = new EventLog();
			if (!System.Diagnostics.EventLog.SourceExists("Ingeniux"))
				EventLog.CreateEventSource("Ingeniux", "IGXLog");
			log.Source = "Ingeniux";
			log.Log = "IGXLog";

			ActionProperties props = new ActionProperties()
			{
				contentStoreUrl = storeUrl,
				xmlPath = xmlPath,
				userId = userId,
				log = log
			};

			Actions = new IgxActions(props);

			Assert.IsNotNull(Actions);

			Actions.RavenQueryExample("Lorem");
		}
	}
}
