using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using CodeProject;
using CronNET;
using Opulos.Core.IO;
using SearchDaemon.Extensions;
using SearchDaemon.Models;

namespace SearchDaemon.Handlers
{
	public class SearchHandler
	{
		private readonly EventLog _eventLog;
		private readonly Settings _settings;

		private System.Timers.Timer _timer;
		private CronDaemon _cronDaemon;

		public SearchHandler(Settings settings, EventLog eventLog)
		{
			_eventLog = eventLog;
			_settings = settings;
		}

		/// <summary>
		/// Запуск поиска.
		/// </summary>
		public void Start()
		{
			if (_settings.SearchStartType == SearchStartType.Timer)
			{
				StartTimer();
			}
			else
			{
				StartCron();
			}
		}

		/// <summary>
		/// Остановка поиска.
		/// </summary>
		public void Stop()
		{
			if (_settings.SearchStartType == SearchStartType.Timer)
			{
				StopTimer();
			}
			else
			{
				StopCron();
			}
		}

		/// <summary>
		/// Запуск таймера. Первая итерация запускается принудительно, 
		/// иначе она была бы проведена только по истечении заданного в настройках интервала времени.
		/// </summary>
		public void StartTimer()
		{
			var interval = _settings.TimerInterval;
			_timer = new System.Timers.Timer(interval);
			_timer.Elapsed += OnSearch;
			_timer.AutoReset = false;

			// Принудительно выполнить проверку, не дожидаясь таймера
			ThreadPool.QueueUserWorkItem((_) => OnSearch(null, null));
		}

		/// <summary>
		/// Запуск крона.
		/// </summary>
		public void StartCron()
		{
			_cronDaemon = new CronDaemon();
			_cronDaemon.AddJob(_settings.Crontab, OnSearch);
			_cronDaemon.Start();
		}

		/// <summary>
		/// Остановка таймера.
		/// </summary>
		public void StopTimer()
		{
			_timer.Stop();
			_timer.Dispose();
		}

		/// <summary>
		/// Остановка крона.
		/// </summary>
		public void StopCron()
		{
			_cronDaemon.Stop();
		}

		/// <summary>
		/// Производит поиск файлов с заданным промежутком времени, 
		/// следующая итерация не начинается пока не завершена предыдущая.
		/// </summary>
		private void OnSearch(object sender, ElapsedEventArgs e)
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

			if (_timer != null) _timer.Start();
		}

		private void OnSearch()
		{
			OnSearch(null, null);
		}

		/// <summary>
		/// Запуск поиска файлов. Список найденных файлов записывается в текстовый файл.
		/// </summary>
		private List<string> StartSearch()
		{
			var output = new List<string>();

			output.Add("Начало поиска в " + DateTime.Now);

			var foundCount = 0;
			foreach (var searchDirectory in _settings.SearchDirectory)
			{
				output.Add("Директория " + searchDirectory);
				output.Add("Шаблон поиска " + string.Join(";", _settings.SearchMask));

				var found = Search(searchDirectory, _settings.SearchMask);
				foundCount += found.Count();
				output.AddRange(found);

				if (_settings.DeleteFiles)
				{
					DeletedFiles(found);
				}
			}

			output.Add("Найдено " + foundCount + " файлов");
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
			var searchMask = string.Join("$|", searchPatterns) + "$";
			searchMask = searchMask.Replace(".", "[.]").Replace("*", ".*").Replace("?", ".");

			switch (_settings.SearchMethod)
			{
				case SearchMethod.DIRECTORY_ENUMERATE_FILES:
					found.AddRange(GetFilesDotNet(searchDirectory, searchMask));
					break;
				case SearchMethod.FAST_DIRECTORY_ENUMERATOR:
					found.AddRange(GetFilesCodeProject(searchDirectory, searchMask));
					break;
				case SearchMethod.FAST_FILE_INFO:
					found.AddRange(GetFilesFastInfo(searchDirectory, searchMask));
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
		/// <param name="mask">Маска поиска.</param>
		/// <returns>Список найденных файлов.</returns>
		private List<string> GetFilesDotNet(string path, string mask)
		{
			var files = new List<string>();
			try
			{
				files.AddRange(Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly)
					.Where(f => Regex.IsMatch(f, mask, RegexOptions.IgnoreCase) == true 
						&& IsExcepted(f) == false));
				if (_settings.SearchOption == SearchOption.AllDirectories)
				{
					foreach (var directory in Directory.GetDirectories(path))
						files.AddRange(GetFilesDotNet(directory, mask));
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
		/// <param name="mask">Маска поиска.</param>
		/// <returns>Список найденных файлов.</returns>
		private IEnumerable<string> GetFilesCodeProject(string path, string mask)
		{
			return FastDirectoryEnumerator.EnumerateFiles(path, "*.*", _settings.SearchOption)
				.Where(f => Regex.IsMatch(f.Name, mask, RegexOptions.IgnoreCase) == true 
					&& IsExcepted(f.Path) == false)
				.Select(f => f.Path);
		}

		/// <summary>
		/// Поиск файлов посредством класса FasFileInfo (новая версия FastDirectoryEnumerator).
		/// </summary>
		/// <param name="path">Директория поиска.</param>
		/// <param name="mask">Маска поиска.</param>
		/// <returns>Список найденных файлов.</returns>
		private IEnumerable<string> GetFilesFastInfo(string path, string mask)
		{
			return FastFileInfo.EnumerateFiles(path, "*.*", _settings.SearchOption)
				.Where(f => Regex.IsMatch(f.Name, mask, RegexOptions.IgnoreCase) == true 
					&& IsExcepted(f.DirectoryName) == false)
				.Select(f => f.FullName);
		}

		/// <summary>
		/// Проверка директории на наличие в списке исключенных директорий.
		/// </summary>
		/// <param name="directory">Директория поиска.</param>
		/// <returns></returns>
		private bool IsExcepted(string directory)
		{
			return _settings.ExcludeDirectory.Any(dir => dir.ToLower().StartsWith(directory.ToLower()));
		}

		/// <summary>
		/// Удаление файлов, если это возможно.
		/// </summary>
		/// <param name="files">Список файлов.</param>
		private void DeletedFiles(List<string> files)
		{
			foreach (var file in files)
			{
				try
				{
					File.Delete(file);
				}
				catch { }
			}
		}
	}
}