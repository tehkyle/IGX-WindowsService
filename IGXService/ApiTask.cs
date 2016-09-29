using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Ingeniux.Service
{
	public partial class ApiTask : ServiceBase
	{
		public ApiTask()
		{
			InitializeComponent();
			log = new EventLog();
			if (!System.Diagnostics.EventLog.SourceExists("Ingeniux"))
				EventLog.CreateEventSource("Ingeniux", "IGXLog");
			log.Source = "Ingeniux";
			log.Log = "IGXLog";
		}

		bool running = false;
		EventLog log { get; set; }
		string SQLConnectionString { get; set; }
		string ContentStoreConnectionString { get; set; }
		string SQLCommand { get; set; }
		string XmlPath { get; set; }
		string OperatingUserId { get; set; }
		SqlConnection connection { get; set; }

		IgxActions Actions { get; set; }

		protected override void OnStart(string[] args)
		{
			try
			{
				SQLConnectionString = ConfigurationManager.AppSettings["SQLConnectionString"].ToString();
				ContentStoreConnectionString = ConfigurationManager.AppSettings["ContentStoreConnectionString"].ToString();
				XmlPath = ConfigurationManager.AppSettings["XmlPath"].ToString();
				OperatingUserId = ConfigurationManager.AppSettings["OperatingUserId"].ToString();
				SQLCommand = ConfigurationManager.AppSettings["SQLCommand"].ToString();				
				
				log.WriteEntry(string.Format("IGX: Starting SQL Dependency on {0}", SQLConnectionString));

				SqlDependency.Start(SQLConnectionString);
				connection = new SqlConnection(SQLConnectionString);
				connection.Open();

				running = true;

				HookUpSqlDep();

				ActionProperties props = new ActionProperties()
				{
					contentStoreUrl = ContentStoreConnectionString,
					xmlPath = XmlPath,
					userId = OperatingUserId,
					connection = connection,
					commandStr = SQLCommand,
					log = log
				};
				Actions = new IgxActions(props);

				running = false;
			}
			catch (Exception ex)
			{
				log.WriteEntry(string.Format("Error starting igx service: {0} -- {1}", ex.Message, ex.StackTrace), EventLogEntryType.Error);
				throw ex;
			}
		}

		private void HookUpSqlDep()
		{
			using (SqlCommand command = new SqlCommand(
				SQLCommand, connection))
			{
				SqlDependency dep = new SqlDependency(command);
				dep.OnChange += new OnChangeEventHandler(OnChanged);

				using (SqlDataReader reader = command.ExecuteReader())
				{}
			}

		}
		
		protected void OnChanged(object sender, SqlNotificationEventArgs e)
		{
			log.WriteEntry(string.Format("IGX: Received SQL Dependency change event: {0} - {1} - {2}", e.Info.ToString(), e.Source.ToString(), e.Type.ToString()));

			SqlDependency dep = sender as SqlDependency;
			dep.OnChange -= OnChanged;
			HookUpSqlDep();

			if (!running)
			{
				try
				{
					running = true;

					// Initialize and/or run API class actions.
					ActionProperties props = new ActionProperties()
					{
						connection = connection,
						commandStr = SQLCommand
					};

					Actions.InitSql(props);
					Actions.ExecuteTaxonomySync();

					running = false;
				}
				catch (Exception ex)
				{
					log.WriteEntry(string.Format("Error initializing and running igx actions: {0} -- {1}", ex.Message, ex.StackTrace), EventLogEntryType.Error);
					throw ex;
				}
			}
		}

		protected override void OnStop()
		{
			Actions.Dispose();
			connection.Close();
			log.WriteEntry(string.Format("IGX: Service stopped."));
		}

		protected override void OnPause()
		{
			log.WriteEntry(string.Format("IGX: Service paused."));
		}

		protected override void OnContinue()
		{
			log.WriteEntry(string.Format("IGX: Service continued."));
		}

		protected override void OnShutdown()
		{
			Actions.Dispose();
			connection.Close();
			log.WriteEntry(string.Format("IGX: Service shut down."));
		}
	}
}
