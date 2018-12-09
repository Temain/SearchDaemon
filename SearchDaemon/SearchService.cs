using System;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using SearchDaemon.Core.Ninject.Factory.Interfaces;
using SearchDaemon.Core.Services;
using SearchDaemon.Core.Services.Interfaces;

namespace SearchDaemon
{
	public partial class SearchService : ServiceBase
	{
		private ISearchHandler _searchHandler;

		#region Constructors

		public SearchService(ISearchFactory searchFactory)
		{
			InitializeComponent();

			// Setup logging
			AutoLog = false;

			((ISupportInitialize)EventLog).BeginInit();
			if (!EventLog.SourceExists(ServiceName))
			{
				EventLog.CreateEventSource(ServiceName, "Application");
			}
			((ISupportInitialize)EventLog).EndInit();

			EventLog.Source = ServiceName;
			EventLog.Log = "Application";

			if (!SearchSettings.Loaded)
			{
				EventLog.WriteEntry("Ошибка при получении настроек сервиса. ", EventLogEntryType.Error);
				Stop();
			}

			var searchEngine = searchFactory.CreateEngine();
			_searchHandler = searchFactory.CreateHandler(searchEngine, EventLog);
		}

		#endregion // Contructors

		protected override void OnStart(string[] args)
		{
			Start();
		}

		public void Start()
		{
			EventLog.WriteEntry("SearchDaemon запущен.");

			try
			{
				_searchHandler.Start();
			}
			catch (Exception ex)
			{
				EventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
			}
		}

		protected override void OnStop()
		{
			if (_searchHandler != null)
			{
				_searchHandler.Stop();
			}

			EventLog.WriteEntry("SearchDaemon остановлен.");
		}
	}
}
