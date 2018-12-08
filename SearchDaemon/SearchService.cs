using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using SearchDaemon.Core.Models;
using SearchDaemon.Core.Ninject.Factory;
using SearchDaemon.Core.Services.Interfaces;

namespace SearchDaemon
{
	public partial class SearchService : ServiceBase
	{
		private ISearchHandler _searchHandler;

		private Settings _settings;

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

			LoadSettings();
			if (!_settings.Loaded)
			{
				EventLog.WriteEntry("Ошибка при получении настроек сервиса. ", EventLogEntryType.Error);
				Stop();
			}

			var searchEngine = searchFactory.CreateEngine(_settings);
			_searchHandler = searchFactory.CreateHandler(searchEngine, _settings, EventLog);
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
				// _searchHandler = new SearchHandler(_settings, EventLog);
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

		private void LoadSettings()
		{
			_settings = new Settings();

			try
			{
				_settings.SearchStartType = (SearchStartType)int.Parse(ConfigurationManager.AppSettings["startType"]);
				_settings.TimerInterval = int.Parse(ConfigurationManager.AppSettings["timerInterval"]) * 60 * 1000;
				_settings.Crontab = ConfigurationManager.AppSettings["crontab"];
				_settings.SearchDirectory = ConfigurationManager.AppSettings["searchDirectory"]?.Split('|');
				_settings.SearchOption = (SearchOption)int.Parse(ConfigurationManager.AppSettings["searchOption"]);
				_settings.SearchMask = ConfigurationManager.AppSettings["searchMask"]?.Split('|');
				_settings.SearchParallel = ConfigurationManager.AppSettings["searchParallel"]?.Trim() == "1";
				_settings.OutputFilePath = ConfigurationManager.AppSettings["outputFilePath"];
				_settings.SearchMethod = (SearchMethod)int.Parse(ConfigurationManager.AppSettings["searchMethod"]);
				_settings.DeleteFiles = ConfigurationManager.AppSettings["deleteFiles"]?.Trim() == "1";
				_settings.TestMode = ConfigurationManager.AppSettings["testMode"]?.Trim() == "1";

				// Исключенные директории, получение значений системных переменных.
				_settings.ExcludeDirectory = ConfigurationManager.AppSettings["excludeDirectory"]?.Split('|');
				var excludedDirectories = new List<string>();
				foreach (var dir in _settings.ExcludeDirectory)
				{
					var expanded = Environment.ExpandEnvironmentVariables(dir);
					excludedDirectories.Add(expanded);
				}
				_settings.ExcludeDirectory = excludedDirectories.ToArray();

				_settings.Loaded = true;
			}
			catch (Exception ex)
			{
				EventLog.WriteEntry("Ошибка при получении настроек сервиса. " + ex.Message + ex.StackTrace, EventLogEntryType.Error);
			}
		}
	}
}
