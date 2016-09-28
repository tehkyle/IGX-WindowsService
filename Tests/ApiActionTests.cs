using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ingeniux.Service;
using System.Diagnostics;

namespace Tests
{
	[TestClass]
	public class ApiActionTests
	{
		string repoPath = @"W:\Sites\Stelter-RT-Content";
		string copyPath = @"C:\navCopy";
		string schemaNames = "ToolkitPage";
		IgxActions Actions;

		[TestMethod]
		public void RunAllTest()
		{
			EventLog log = new EventLog();
			if (!System.Diagnostics.EventLog.SourceExists("Ingeniux"))
				EventLog.CreateEventSource("Ingeniux", "IGXLog");
			log.Source = "Ingeniux";
			log.Log = "IGXLog";
			Actions = new IgxActions(log);
			Assert.IsNotNull(Actions);
		}
	}
}
