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
			var iniString = File.ReadAllText("SearchDaemon.ini");
			_settings = new ConfigurationBuilder<ISearchSettings>()
				.UseIniString(iniString)
				.Build();

			SearchStartType = _settings.SearchStartType;
			TimerInterval = _settings.TimerInterval * 60 * 1000;
			Crontab = _settings.Crontab;
			SearchDirectory = _settings.SearchDirectory;
			SearchMask = _settings.SearchMask;
			SearchMethod = _settings.SearchMethod;
			SearchParallel = _settings.SearchParallel;
			SearchOption = _settings.SearchOption;
			OutputFilePath = _settings.OutputFilePath;
			DeleteFiles = _settings.DeleteFiles;

			var excludedDirectories = new List<string>();
			foreach (var dir in _settings.ExcludeDirectory)
			{
				var expanded = Environment.ExpandEnvironmentVariables(dir);
				excludedDirectories.Add(expanded);
			}
			ExcludeDirectory = excludedDirectories.ToArray();

			Loaded = true;
		}


		public static SearchStartType SearchStartType { get; set; }

		public static int TimerInterval { get; set; }

		public static string Crontab { get; set; }


		public static string[] SearchDirectory { get; set; }

		public static string[] ExcludeDirectory { get; set; }

		public static string[] SearchMask { get; set; }

		public static SearchMethod SearchMethod { get; set; } 

		public static bool SearchParallel { get; set; }

		public static SearchOption SearchOption { get; set; }


		public static string OutputFilePath { get; set; }

		public static bool DeleteFiles { get; set; }



		public static bool Loaded { get; set; }
	}
}
