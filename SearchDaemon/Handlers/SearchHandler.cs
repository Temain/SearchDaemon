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

		/// <summary>
		/// Запуск таймера. Первая итерация запускается принудительно, 
		/// иначе она была бы проведена только по истечении заданного в настройках интервала времени.
		/// </summary>
		public void StartTimer()
		{
			_timer.Elapsed += OnSearch;
			_timer.AutoReset = false;

			// Принудительно выполнить проверку, не дожидаясь таймера
			ThreadPool.QueueUserWorkItem((_) => OnSearch(null, null));
		}

		/// <summary>
		/// Остановка таймера.
		/// </summary>
		public void CloseTimer()
		{
			_timer.Stop();
			_timer.Dispose();
		}

		/// <summary>
		/// Производит поиск файлов с заданным промежутком времени, 
		/// следующая итерация не начинается пока не завершена предыдущая.
		/// </summary>
		private void OnSearch(Object sender, ElapsedEventArgs e)
		{
			_eventLog.WriteEntry("Поиск файлов...", EventLogEntryType.Information);

			List<string> searchResult = null;
			try
			{
				searchResult = StartSearch();
				_eventLog.WriteEntry("Поиск успешно завершен.", EventLogEntryType.Information);
			}
			catch (Exception ex)
			{
				_eventLog.WriteEntry("Ошибка при поиске файлов: " + ex.StackTrace, EventLogEntryType.Warning);
			}

			if (searchResult != null)
			{
				File.WriteAllLines(_settings.OutputFilePath, searchResult);
			}

			_timer.Start();
		}

		/// <summary>
		/// Запуск поиска файлов. Список найденных файлов записывается в текстовый файл.
		/// </summary>
		private List<string> StartSearch()
		{
			var output = new List<string>();
			output.Add("Начало поиска в " + DateTime.Now);

			foreach (var searchDirectory in _settings.SearchDirectory)
			{
				output.Add("Директория " + searchDirectory);
				output.Add("Шаблон поиска " + string.Join(";", _settings.SearchMask));

				var found = Search(searchDirectory, _settings.SearchMask);
				output.AddRange(found);
			}

			output.Add("Окончание поиска в " + DateTime.Now);
			return output;
		}

		/// <summary>
		/// ПРоизводит поиск файлов в соответсвии с указанным в настрйках методом поиска.
		/// </summary>
		/// <param name="searchDirectory">Директория поиска.</param>
		/// <param name="searchPatterns">Маски поиска.</param>
		/// <returns>Список найденных файлов.</returns>
		public List<string> Search(string searchDirectory, string[] searchPatterns)
		{
			var found = new ConcurrentBag<string>();

			switch (_settings.SearchMethod)
			{
				case SearchMethod.DIRECTORY_ENUMERATE_FILES:
					if (_settings.SearchParallel)
					{
						found.AddRange(searchPatterns.AsParallel()
							.SelectMany(searchPattern => GetFilesDotNet(searchDirectory, searchPattern)));
					}
					else
					{
						foreach (var searchPattern in searchPatterns)
						{
							found.AddRange(GetFilesDotNet(searchDirectory, searchPattern));
						}
					}
					break;
				case SearchMethod.FAST_DIRECTORY_ENUMERATOR:
					if (_settings.SearchParallel)
					{
						found.AddRange(searchPatterns.AsParallel()
							.SelectMany(searchPattern => GetFilesCodeProject(searchDirectory, searchPattern)));
					}
					else
					{
						foreach (var searchPattern in searchPatterns)
						{
							found.AddRange(GetFilesCodeProject(searchDirectory, searchPattern));
						}
					}
					break;
				case SearchMethod.FAST_FILE_INFO:
					if (_settings.SearchParallel)
					{
						found.AddRange(searchPatterns.AsParallel()
							.SelectMany(searchPattern => GetFilesFastInfo(searchDirectory, searchPattern)));
					}
					else
					{
						foreach (var searchPattern in searchPatterns)
						{
							found.AddRange(GetFilesFastInfo(searchDirectory, searchPattern));
						}
					}
					break;
				default:
					throw new ArgumentException("Недопустимый метод поиска.");
			}

			return found.ToList();
		}

		/// <summary>
		/// Поиск файлов стандартными средствами .Net
		/// </summary>
		/// <param name="path">Директория поиска.</param>
		/// <param name="pattern">Маска поиска.</param>
		/// <returns>Список найденных файлов.</returns>
		private List<string> GetFilesDotNet(string path, string pattern)
		{
			var files = new List<string>();

			try
			{
				files.AddRange(Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly)
					.Where(f => IsExcepted(new FileInfo(f).DirectoryName) == false));
				if (_settings.SearchOption == SearchOption.AllDirectories)
				{
					foreach (var directory in Directory.GetDirectories(path))
						files.AddRange(GetFilesDotNet(directory, pattern));
				}
			}
			catch (UnauthorizedAccessException) { }
			catch (Exception) { }

			return files;
		}

		/// <summary>
		/// Поиск файлов посредством класса FastDirectoryEnumerator, найденного на CodeProject.
		/// </summary>
		/// <param name="path">Директория поиска.</param>
		/// <param name="pattern">Маска поиска.</param>
		/// <returns>Список найденных файлов.</returns>
		private IEnumerable<string> GetFilesCodeProject(string path, string pattern)
		{
			var files = new List<string>();

			try
			{
				files.AddRange(FastDirectoryEnumerator.EnumerateFiles(path, pattern, _settings.SearchOption)
					.Where(f => IsExcepted(Path.GetDirectoryName(f.Path)) == false)
					.Select(f => f.Path));
			}
			catch (UnauthorizedAccessException) { }
			catch (Exception) { }

			return files;
		}

		/// <summary>
		/// Поиск файлов посредством класса FasFileInfo (новая версия FastDirectoryEnumerator).
		/// </summary>
		/// <param name="path">Директория поиска.</param>
		/// <param name="pattern">Маска поиска.</param>
		/// <returns>Список найденных файлов.</returns>
		private IEnumerable<string> GetFilesFastInfo(string path, string pattern)
		{
			var files = new List<string>();

			try
			{
				files.AddRange(FastFileInfo.EnumerateFiles(path, pattern, _settings.SearchOption)
					.Where(f => IsExcepted(f.DirectoryName) == false)
					.Select(f => f.FullName));
			}
			catch (UnauthorizedAccessException) { }
			catch (Exception) { }

			return files;
		}

		/// <summary>
		/// Проверка директории на наличие в списке исключенных директорий.
		/// </summary>
		/// <param name="directory">Директория поиска.</param>
		/// <returns></returns>
		private bool IsExcepted(string directory)
		{
			return _settings.ExceptDirectory.Any(dir => dir.Contains(directory.ToLower()));
		}

		private bool IsShouldStart()
		{
			var shedule = "";


			return true;
		}
	}
}