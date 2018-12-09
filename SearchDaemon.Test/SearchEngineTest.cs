using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SearchDaemon.Core.Models;
using SearchDaemon.Core.Services;

namespace SearchDaemon.Test
{
	[TestClass]
	public class SearchEngineTest
	{
		[TestInitialize]
		public void SearchEngineTestInitialize()
		{
			// var kernel = new StandardKernel(new SearchDaemonNinjectModule());
		}

		[TestMethod]
		public void RunAllMethods()
		{
			var iterations = 4;
			var output = new List<string>();
			var methods = new List<SearchMethod>
			{
				SearchMethod.DIRECTORY_ENUMERATE_FILES,
				SearchMethod.FAST_FILE_INFO,
				SearchMethod.FAST_FILE_INFO_WITH_EXCLUDE
			};

			SearchSettings.SearchParallel = false;

			var stopwatch = new Stopwatch();
			foreach (var method in methods)
			{
				output.Add("Метод: " + method);
				SearchSettings.SearchMethod = method;

				long total = 0;
				for (var i = 0; i < iterations; i++)
				{
					if (i % 2 != 0)
					{
						SearchSettings.SearchParallel = true;
					}
					else
					{
						SearchSettings.SearchParallel = false;
					}

					stopwatch.Restart();

					if (SearchSettings.SearchParallel)
					{
						SearchSettings.SearchDirectory.AsParallel()
							.SelectMany(directory => new SearchEngine().Search(directory, SearchSettings.SearchMask))
							.ToList();
					}
					else
					{
						foreach (var searchDirectory in SearchSettings.SearchDirectory)
						{
							new SearchEngine().Search(searchDirectory, SearchSettings.SearchMask);
						}
					}

					var time = stopwatch.ElapsedMilliseconds;
					output.Add("Итерация: " + (i + 1) + ", Время поиска: " + time);
					total += time;
				}
				output.Add("Всего: " + total);
				output.Add("");
			}

			File.WriteAllLines(SearchSettings.OutputFilePath, output);
		}
	}
}
