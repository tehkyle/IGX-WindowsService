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

		// Required for SQL sync tests.
		string connectionStr = @"";
		string commandStr = @"";

		SqlConnection conn;
		EventLog log;
		ActionProperties props;

		IgxActions Actions;

		[TestInitialize]
		public void Init()
		{
			EventLog log = new EventLog();
			if (!System.Diagnostics.EventLog.SourceExists("Ingeniux"))
				EventLog.CreateEventSource("Ingeniux", "IGXLog");
			log.Source = "Ingeniux";
			log.Log = "IGXLog";

			props = new ActionProperties()
			{
				contentStoreUrl = storeUrl,
				xmlPath = xmlPath,
				userId = userId,
				log = log,
				commandStr = commandStr
			};

			if (!string.IsNullOrWhiteSpace(connectionStr)) {
				conn = new SqlConnection(connectionStr);
				conn.Open();
				props.connection = conn;
			}

			Actions = new IgxActions(props);
			Assert.IsNotNull(Actions);
		}

		[TestCleanup]
		public void cleanup()
		{
			Actions.Dispose();
			conn.Close();
		}

		[TestMethod]
		public void TaxonomySyncTest()
		{
			Actions.ExecuteTaxonomySync();
		}

		[TestMethod]
		public void SessionTest()
		{
			Actions.SessionExample();
		}

		[TestMethod]
		public void NestedSessionTest()
		{
			Actions.NestedSessionExample();
		}

		[TestMethod]
		public void RavenQueryTest()
		{
			Actions.RavenQueryExample("Lorem");
		}
	}
}
