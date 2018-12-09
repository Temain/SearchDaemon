using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using CronNET;
using SearchDaemon.Core.Extensions;
using SearchDaemon.Core.Models;
using SearchDaemon.Core.Services.Interfaces;

namespace SearchDaemon.Core.Services
{
	public class SearchHandler : ISearchHandler
	{
		private readonly ISearchEngine _searchEngine;
		private readonly EventLog _eventLog;

		private System.Timers.Timer _timer;
		private CronDaemon _cronDaemon;

		#region Constructors

		public SearchHandler(ISearchEngine searchEngine, EventLog eventLog)
		{
			_searchEngine = searchEngine;
			_eventLog = eventLog;
		}

		#endregion

		/// <summary>
		/// Запуск поиска.
		/// </summary>
		public void Start()
		{
			if (SearchSettings.SearchStartType == SearchStartType.Timer)
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
			if (SearchSettings.SearchStartType == SearchStartType.Timer)
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
		private void StartTimer()
		{
			_timer = new System.Timers.Timer(SearchSettings.TimerInterval);
			_timer.Elapsed += OnSearch;
			_timer.AutoReset = false;

			// Принудительно выполнить проверку, не дожидаясь таймера
			ThreadPool.QueueUserWorkItem((_) => OnSearch(null, null));
		}

		/// <summary>
		/// Запуск крона.
		/// </summary>
		private void StartCron()
		{
			_cronDaemon = new CronDaemon();
			_cronDaemon.AddJob(SearchSettings.Crontab, OnSearch);
			_cronDaemon.Start();
		}

		/// <summary>
		/// Остановка таймера.
		/// </summary>
		private void StopTimer()
		{
			_timer.Stop();
			_timer.Dispose();
		}

		/// <summary>
		/// Остановка крона.
		/// </summary>
		private void StopCron()
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
				File.WriteAllLines(SearchSettings.OutputFilePath, searchResult);
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
			var found = new ConcurrentBag<string>();
			output.Add("Начало поиска: " + DateTime.Now);
			output.Add("Шаблон поиска: " + string.Join(";", SearchSettings.SearchMask));
			output.Add("Директории: " + string.Join(";", SearchSettings.SearchDirectory));

			if (SearchSettings.SearchParallel)
			{
				found.AddRange(
					SearchSettings.SearchDirectory.AsParallel()
						.SelectMany(directory => _searchEngine.Search(directory, SearchSettings.SearchMask)));
			}
			else
			{
				foreach (var searchDirectory in SearchSettings.SearchDirectory)
				{
					found.AddRange(
						_searchEngine.Search(searchDirectory, SearchSettings.SearchMask));
				}
			}

			if (SearchSettings.DeleteFiles) DeletedFiles(found);

			output.AddRange(found);
			output.Add("Найдено: " + found.Count() + " файлов");
			output.Add("Окончание поиска: " + DateTime.Now);
			return output.ToList();
		}

		/// <summary>
		/// Удаление файлов, если это возможно.
		/// </summary>
		/// <param name="files">Список файлов.</param>
		private void DeletedFiles(IEnumerable<string> files)
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