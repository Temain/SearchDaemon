using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Opulos.Core.IO;
using SearchDaemon.Core.Models;
using SearchDaemon.Core.Services.Interfaces;

namespace SearchDaemon.Core.Services
{
	public class SearchEngine : ISearchEngine
	{
		#region Contructors

		public SearchEngine() { }

		#endregion // Contructors

		/// <summary>
		/// Производит поиск файлов в соответсвии с указанным в настрйках методом поиска.
		/// </summary>
		/// <param name="directory">Директория поиска.</param>
		/// <param name="patterns">Маски поиска.</param>
		/// <returns>Список найденных файлов.</returns>
		public IEnumerable<string> Search(string directory, string[] patterns)
		{
			var found = new List<string>();
			var searchMask = (string.Join("$|", patterns) + "$")
				.Replace(".", "[.]").Replace("*", ".*").Replace("?", ".");
			var searchRegex = new Regex(searchMask, RegexOptions.IgnoreCase /*| RegexOptions.Compiled*/);

			switch (SearchSettings.SearchMethod)
			{
				case SearchMethod.DIRECTORY_ENUMERATE_FILES:
					found.AddRange(GetFilesDotNet(directory, searchRegex));
					break;
				case SearchMethod.FAST_FILE_INFO:
					found.AddRange(GetFilesFastInfo(directory, searchRegex));
					break;
				case SearchMethod.FAST_FILE_INFO_WITH_EXCLUDE:
					found.AddRange(GetFilesFastInfoWithExclude(directory, searchRegex));
					break;
				default:
					throw new ArgumentException("Недопустимый метод поиска.");
			}

			return found;
		}

		/// <summary>
		/// Поиск файлов стандартными средствами .Net
		/// </summary>
		/// <param name="path">Директория поиска.</param>
		/// <param name="regex">Регулярное выражение.</param>
		/// <returns>Список найденных файлов.</returns>
		private IEnumerable<string> GetFilesDotNet(string path, Regex regex)
		{
			var files = Enumerable.Empty<string>();
			try
			{
				if (IsExcluded(path) == false)
				{
					var directoryInfo = new DirectoryInfo(path);
					files = files.Concat(
						directoryInfo.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly)
							.Where(f => regex.IsMatch(f.Name) == true /*&& IsExcluded(f.DirectoryName) == false*/)
							.Select(f => f.FullName));
					if (SearchSettings.SearchOption == SearchOption.AllDirectories)
					{
						foreach (var dir in Directory.EnumerateDirectories(path))
							files = files.Concat(GetFilesDotNet(dir, regex));
					}
				}
			}
			catch (UnauthorizedAccessException) { }
			catch (Exception) { }

			return files;
		}

		/// <summary>
		/// Поиск файлов посредством класса FasFileInfo.
		/// </summary>
		/// <param name="path">Директория поиска.</param>
		/// <param name="regex">Маска поиска.</param>
		/// <returns>Список найденных файлов.</returns>
		private IEnumerable<string> GetFilesFastInfo(string path, Regex regex)
		{
			return FastFileInfo.EnumerateFiles(path, "*.*", SearchSettings.SearchOption)
				.Where(f => regex.IsMatch(f.Name) == true && IsExcluded(f.DirectoryName) == false)
				.Select(f => f.FullName);
		}

		/// <summary>
		/// Поиск файлов посредством класса FasFileInfo с получением директорий.
		/// </summary>
		/// <param name="path">Директория поиска.</param>
		/// <param name="regex">Маска поиска.</param>
		/// <returns>Список найденных файлов.</returns>
		private IEnumerable<string> GetFilesFastInfoWithExclude(string path, Regex regex)
		{
			var files = Enumerable.Empty<string>();
			try
			{
				if (IsExcluded(path) == false)
				{
					var directoryInfo = new DirectoryInfo(path);
					files = files.Concat(
						FastFileInfo.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly)
							.Where(f => regex.IsMatch(f.Name) == true /*&& IsExcluded(f.DirectoryName) == false*/)
							.Select(f => f.FullName));
					if (SearchSettings.SearchOption == SearchOption.AllDirectories)
					{
						foreach (var dir in Directory.EnumerateDirectories(path))
							files = files.Concat(GetFilesDotNet(dir, regex));
					}
				}
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
		private bool IsExcluded(string directory)
		{
			return SearchSettings.ExcludeDirectory.Any(excluded => directory.StartsWith(excluded, StringComparison.InvariantCultureIgnoreCase));
		}
	}
}
