using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Collections.Concurrent;
using System.Diagnostics;
using Ingeniux.CMS;

namespace Ingeniux.Service
{
    public class IgxActions : IDisposable
    {
		ContentStore Store;
		IReadonlyUser User;

		public IgxActions(string contentStoreUrl, string xmlPath, string userId, EventLog log)
		{
			this.log = log;
			try
			{
				Store = new ContentStore(contentStoreUrl, xmlPath);
				User = Store.GetStartingUser(userId);
			}
			catch (Exception e)
			{
				log.WriteEntry(string.Format("Error initializing action class: {0} -- {1}", e.Message, e.StackTrace), EventLogEntryType.Error);
				throw e;
			}
		}

		public void Execute()
		{
			using (var session = Store.OpenWriteSession(User))
			{

			}
		}

		public void SessionScopeExample()
		{
			using (var session = Store.OpenWriteSession(User))
			{

			}
		}

		public void ManagerEntityExample()
		{
			using (var session = Store.OpenWriteSession(User))
			{

			}
		}

		public void QueryExample()
		{
			using (var session = Store.OpenWriteSession(User))
			{

			}
		}

		public void IndexQueryExample()
		{
			using (var session = Store.OpenWriteSession(User))
			{

			}
		}

		EventLog log;

		public void Dispose()
		{
			Store.Dispose();
		}
	}
}
