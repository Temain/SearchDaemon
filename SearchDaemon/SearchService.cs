using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using SearchDaemon.Handlers;
using SearchDaemon.Models;

namespace SearchDaemon
{
	public partial class SearchService : ServiceBase
	{
		private Settings _settings;
		private SearchHandler _searchHandler;

		public SearchService()
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
		}

		protected override void OnStart(string[] args)
		{
			Start();
		}

		public void Start()
		{
			EventLog.WriteEntry("SearchDaemon запущен.");

			LoadSettings();
			if (!_settings.Loaded)
			{
				EventLog.WriteEntry("Ошибка при получении настроек сервиса. ", EventLogEntryType.Error);
				Stop();
			}

			try
			{
				_searchHandler = new SearchHandler(_settings, EventLog);
				_searchHandler.StartTimer();
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
				_searchHandler.CloseTimer();
			}

			EventLog.WriteEntry("SearchDaemon остановлен.");
		}

		private void LoadSettings()
		{
			_settings = new Settings();

			try
			{
				_settings.TimerInterval = int.Parse(ConfigurationManager.AppSettings["timerInterval"]) * 60 * 1000;
				_settings.SearchDirectory = ConfigurationManager.AppSettings["searchDirectory"]?.Split('|');
				_settings.ExceptDirectory = ConfigurationManager.AppSettings["exceptDirectory"]?.ToLower().Split('|');
				_settings.SearchOption = (SearchOption)int.Parse(ConfigurationManager.AppSettings["searchOption"]);
				_settings.SearchMask = ConfigurationManager.AppSettings["searchMask"]?.Split('|');
				_settings.OutputFilePath = ConfigurationManager.AppSettings["outputFilePath"];
				_settings.SearchMethod = (SearchMethod)int.Parse(ConfigurationManager.AppSettings["searchMethod"]);
				_settings.SearchParallel = ConfigurationManager.AppSettings["searchParallel"]?.Trim() == "1";

				_settings.Loaded = true;
			}
			catch (Exception ex)
			{
				EventLog.WriteEntry("Ошибка при получении настроек сервиса. " + ex.Message + ex.StackTrace, EventLogEntryType.Error);
			}
		}
	}
}
