using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using CodeProject;
using SearchDaemon.Models;

namespace SearchDaemon.Handlers
{
	public class SearchHandler
	{
		private readonly EventLog _eventLog;
		private readonly Settings _settings;
		private readonly System.Timers.Timer _timer;

		public SearchHandler(Settings settings, EventLog eventLog)
		{
			_eventLog = eventLog;
			_settings = settings;

			var interval = _settings.TimerInterval;
			_timer = new System.Timers.Timer(interval);
		}

		public void StartTimer()
		{
			_timer.Elapsed += StartSearch;
			_timer.AutoReset = false;

			// Принудительно выполнить проверку, не дожидаясь таймера
			ThreadPool.QueueUserWorkItem((_) => StartSearch(null, null));
		}

		public void CloseTimer()
		{
			_timer.Stop();
			_timer.Dispose();
		}

		private void StartSearch(Object sender, ElapsedEventArgs e)
		{
			_eventLog.WriteEntry("Поиск файлов...", EventLogEntryType.Information);

			try
			{
				SearchFiles();
			}
			catch (Exception ex)
			{
				_eventLog.WriteEntry("Ошибка при поиске файлов: " + ex.StackTrace, EventLogEntryType.Warning);
			}

			_eventLog.WriteEntry("Поиск успешно завершена.", EventLogEntryType.Information);
			_timer.Start();
		}

		private void SearchFiles()
		{
			var searchDirectories = _settings.SearchDirectory.Split('|');
			var searchPatterns = _settings.SearchMask.Split('|');

			var result = new List<string>();
			result.Add("Начало поиска в " + DateTime.Now);
			foreach (var searchDirectory in searchDirectories)
			{
				result.Add("Директория " + searchDirectory);
				result.Add("Шаблон поиска " + searchPatterns);
				//result.AddRange(searchPatterns.AsParallel()
				//	.SelectMany(searchPattern => GetFilesDotNet(searchDirectory, searchPattern)));
				result.AddRange(searchPatterns.AsParallel()
					.SelectMany(searchPattern => GetFilesCodeProject(searchDirectory, searchPattern)));
				result.Add("");
			}

			result.Add("Окончание поиска в " + DateTime.Now);
			File.WriteAllLines(_settings.OutputFilePath, result);
		}

		private List<string> GetFilesDotNet(string path, string pattern)
		{
			var files = new List<string>();

			try
			{
				files.AddRange(Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly));
				if (_settings.SearchOption == SearchOption.AllDirectories)
				{
					foreach (var directory in Directory.GetDirectories(path))
						files.AddRange(GetFilesDotNet(directory, pattern));
				}
			}
			catch (UnauthorizedAccessException) { }

			return files;
		}

		private IEnumerable<string> GetFilesCodeProject(string path, string pattern)
		{
			var files = new List<string>();

			try
			{
				files.AddRange(FastDirectoryEnumerator.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly)
					.Where(f => !f.Path.Contains("Windows"))
					.Select(f => f.Path));
				if (_settings.SearchOption == SearchOption.AllDirectories)
				{
					foreach (var directory in Directory.GetDirectories(path))
						files.AddRange(GetFilesCodeProject(directory, pattern));
				}
			}
			catch (UnauthorizedAccessException) { }

			return files;
		}
	}
}