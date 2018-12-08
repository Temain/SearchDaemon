using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SearchDaemon.Core.Models;
using SearchDaemon.Core.Services.Interfaces;

namespace SearchDaemon.Core.Tests
{
	public class SearchHandlerTests
	{
		private readonly Settings _settings;
		private readonly ISearchEngine _searchEngine;

		public SearchHandlerTests(ISearchEngine searchEngine, Settings settings)
		{
			_settings = settings;
			_searchEngine = searchEngine;
		}

		public void RunAllMethods()
		{
			var iterations = 1;
			var output = new List<string>();
			var methods = new List<SearchMethod>
			{
				SearchMethod.DIRECTORY_ENUMERATE_FILES,
				SearchMethod.FAST_FILE_INFO,
				SearchMethod.FAST_FILE_INFO_WITH_EXCLUDE
			};

			var stopwatch = new Stopwatch();
			foreach (var method in methods)
			{
				output.Add("Метод: " + method);
				_settings.SearchMethod = method;

				long total = 0;
				for (var i = 0; i < iterations; i++)
				{
					stopwatch.Restart();

					if (_settings.SearchParallel)
					{
						_settings.SearchDirectory.AsParallel()
							.SelectMany(directory => _searchEngine.Search(directory, _settings.SearchMask));
					}
					else
					{
						foreach (var searchDirectory in _settings.SearchDirectory)
						{
							_searchEngine.Search(searchDirectory, _settings.SearchMask);
						}
					}

					var time = stopwatch.ElapsedMilliseconds;
					output.Add("Итерация: " + (i + 1) + ", Время поиска: " + time);
					total += time;
				}
				output.Add("Всего: " + total);
				output.Add("");
			}

			File.WriteAllLines(_settings.OutputFilePath, output);
		}
	}
}
