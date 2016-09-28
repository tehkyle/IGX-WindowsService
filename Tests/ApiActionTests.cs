using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ingeniux.Service;
using System.Diagnostics;

namespace Tests
{
	[TestClass]
	public class ApiActionTests
	{
		string storeUrl = @"";
		string xmlPath = @"";
		string userId = "";
		IgxActions Actions;

		[TestMethod]
		public void MainTest()
		{
			EventLog log = new EventLog();
			if (!System.Diagnostics.EventLog.SourceExists("Ingeniux"))
				EventLog.CreateEventSource("Ingeniux", "IGXLog");
			log.Source = "Ingeniux";
			log.Log = "IGXLog";
			Actions = new IgxActions(storeUrl, xmlPath, userId, log);
			Assert.IsNotNull(Actions);
			Actions.Execute();
		}
	}
}
