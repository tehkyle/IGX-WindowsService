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
		string UserId;

		public IgxActions(string contentStoreUrl, string xmlPath, string userId, EventLog log)
		{
			this.log = log;
			try
			{
				UserId = userId;
				Store = new ContentStore(contentStoreUrl, xmlPath);
			}
			catch (Exception e)
			{
				log.WriteEntry(string.Format("Error initializing action class: {0} -- {1}", e.Message, e.StackTrace), EventLogEntryType.Error);
				throw e;
			}
		}

		public void Execute()
		{
			using (var session = Store.OpenWriteSession(Store.GetStartingUser(UserId)))
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
