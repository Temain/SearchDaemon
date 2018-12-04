using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using CodeProject;
using Opulos.Core.IO;
using SearchDaemon.Extensions;
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
				StartSearch();
			}
			catch (Exception ex)
			{
				_eventLog.WriteEntry("Ошибка при поиске файлов: " + ex.StackTrace, EventLogEntryType.Warning);
			}

			_eventLog.WriteEntry("Поиск успешно завершена.", EventLogEntryType.Information);
			_timer.Start();
		}

		private void StartSearch()
		{
			var searchDirectories = _settings.SearchDirectory.Split('|');
			var searchPatterns = _settings.SearchMask.Split('|');

			var output = new List<string>();
			output.Add("Начало поиска в " + DateTime.Now);

			foreach (var searchDirectory in searchDirectories)
			{
				output.Add("Директория " + searchDirectory);
				output.Add("Шаблон поиска " + _settings.SearchMask);

				var stopwatch = new Stopwatch();
				stopwatch.Start();

				Search(searchDirectory, searchPatterns);

				output.Add("");
			}

			output.Add("Окончание поиска в " + DateTime.Now);
			File.WriteAllLines(_settings.OutputFilePath, output);
		}

		private List<string> Search(string searchDirectory, string[] searchPatterns)
		{
			var result = new ConcurrentBag<string>();

			switch (_settings.SearchMethod)
			{
				case SearchMethod.DIRECTORY_ENUMERATE_FILES:
					if (_settings.SearchParallel)
					{
						 result.AddRange(searchPatterns.AsParallel()
							.SelectMany(searchPattern => GetFilesDotNet(searchDirectory, searchPattern)));
					}
					else
					{
						foreach (var searchPattern in searchPatterns)
						{
							result.AddRange(GetFilesDotNet(searchDirectory, searchPattern));
						}
					}
					break;
				case SearchMethod.FAST_DIRECTORY_ENUMERATOR:
					if (_settings.SearchParallel)
					{
						result.AddRange(searchPatterns.AsParallel()
							.SelectMany(searchPattern => GetFilesCodeProject(searchDirectory, searchPattern)));
					}
					else
					{
						foreach (var searchPattern in searchPatterns)
						{
							result.AddRange(GetFilesCodeProject(searchDirectory, searchPattern));
						}
					}
					break;
				case SearchMethod.FAST_FILE_INFO:
					if (_settings.SearchParallel)
					{
						result.AddRange(searchPatterns.AsParallel()
							.SelectMany(searchPattern => GetFilesFastInfo(searchDirectory, searchPattern)));
					}
					else
					{
						foreach (var searchPattern in searchPatterns)
						{
							result.AddRange(GetFilesFastInfo(searchDirectory, searchPattern));
						}
					}
					break;
				default:
					throw new ArgumentException("Недопустимый метод поиска.");
			}

			return result.ToList();
		}

		private List<string> GetFilesDotNet(string path, string pattern)
		{
			var files = new List<string>();

			try
			{
				files.AddRange(Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly)
					.Where(f => !new FileInfo(f).DirectoryName.ToLower().Contains(_settings.ExceptDirectory)));
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
					.Where(f => !f.Path.Contains(_settings.ExceptDirectory))
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

		private IEnumerable<string> GetFilesFastInfo(string path, string pattern)
		{
			var files = new List<string>();

			try
			{
				files.AddRange(FastFileInfo.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly)
					.Where(f => !f.DirectoryName.Contains(_settings.ExceptDirectory))
					.Select(f => f.FullName));
				if (_settings.SearchOption == SearchOption.AllDirectories)
				{
					foreach (var directory in Directory.GetDirectories(path))
						files.AddRange(GetFilesFastInfo(directory, pattern));
				}
			}
			catch (UnauthorizedAccessException) { }

			return files;
		}
	}
}