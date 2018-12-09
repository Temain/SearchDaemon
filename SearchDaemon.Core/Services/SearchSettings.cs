using System;
using System.Collections.Generic;
using System.IO;
using Config.Net;
using SearchDaemon.Core.Models;
using SearchDaemon.Core.Services.Interfaces;

namespace SearchDaemon.Core.Services
{
	public static class SearchSettings
	{
		private static readonly ISearchSettings _settings;

		static SearchSettings()
		{
			_settings = new ConfigurationBuilder<ISearchSettings>()
				.UseIniFile("SearchDaemon.ini")
				.Build();

			_settings.TimerInterval = _settings.TimerInterval * 60 * 1000;

			var excludedDirectories = new List<string>();
			foreach (var dir in ExcludeDirectory)
			{
				var expanded = Environment.ExpandEnvironmentVariables(dir);
				excludedDirectories.Add(expanded);
			}
			_settings.ExcludeDirectory = excludedDirectories.ToArray();

			Loaded = true;
		}

		public static SearchStartType SearchStartType => _settings.SearchStartType;
		public static int TimerInterval => _settings.TimerInterval;
		public static string Crontab => _settings.Crontab;
		public static string[] SearchDirectory => _settings.SearchDirectory;
		public static bool SearchParallel => _settings.SearchParallel;
		public static string[] ExcludeDirectory => _settings.ExcludeDirectory;
		public static SearchOption SearchOption => _settings.SearchOption;
		public static string[] SearchMask => _settings.SearchMask;
		public static string OutputFilePath => _settings.OutputFilePath;
		public static SearchMethod SearchMethod { get => _settings.SearchMethod; set { } }
		public static bool DeleteFiles => _settings.DeleteFiles;

		public static bool Loaded { get; set; }
	}
}
