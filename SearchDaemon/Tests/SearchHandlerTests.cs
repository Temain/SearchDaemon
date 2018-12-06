using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SearchDaemon.Handlers;
using SearchDaemon.Models;

namespace SearchDaemon.Tests
{
	public class SearchHandlerTests
	{
		private readonly Settings _settings;
		private readonly SearchHandler _searchHandler;

		public SearchHandlerTests(SearchHandler searchHandler, Settings settings)
		{
			_settings = settings;
			_searchHandler = searchHandler;
		}

		public void RunAllMethods()
		{
			var iterations = 100;
			var output = new List<string>();
			var methods = new List<SearchMethod>
			{
				SearchMethod.DIRECTORY_ENUMERATE_FILES,
				SearchMethod.FAST_DIRECTORY_ENUMERATOR,
				SearchMethod.FAST_FILE_INFO
			};

			var stopwatch = new Stopwatch();
			foreach (var method in methods)
			{
				output.Add("Метод: " + method);
				_settings.SearchMethod = method;

				long total = 0;
				for (var i = 0; i < iterations; i++)
				{
					foreach (var searchDirectory in _settings.SearchDirectory)
					{
						stopwatch.Restart();
						_searchHandler.Search(searchDirectory, _settings.SearchMask);

						var time = stopwatch.ElapsedMilliseconds;
						output.Add("Итерация: " + (i + 1) + ", Время поиска: " + time);
						total += time;
					}
				}
				output.Add("Всего: " + total);
				output.Add("");
			}

			File.WriteAllLines(_settings.OutputFilePath, output);
		}
	}
}
