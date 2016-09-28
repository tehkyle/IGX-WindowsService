using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Ingeniux.Service
{
    public class IgxActions : IDisposable
    {
		public IgxActions(EventLog log)
		{
			this.log = log;
			try
			{
				
			}
			catch (Exception e)
			{
				log.WriteEntry(string.Format("Error initializing action class: {0} -- {1}", e.Message, e.StackTrace), EventLogEntryType.Error);
				throw e;
			}
		}

		EventLog log;

		public void Dispose()
		{

		}
	}
}
