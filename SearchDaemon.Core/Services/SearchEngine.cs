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
		private readonly Settings _settings;

		#region Constructors

		public SearchEngine(Settings settings)
		{
			_settings = settings;
		}

		#endregion

		/// <summary>
		/// Производит поиск файлов в соответсвии с указанным в настрйках методом поиска.
		/// </summary>
		/// <param name="searchDirectory">Директория поиска.</param>
		/// <param name="searchPatterns">Маски поиска.</param>
		/// <returns>Список найденных файлов.</returns>
		public IEnumerable<string> Search(string searchDirectory, string[] searchPatterns)
		{
			var found = new List<string>();
			var searchMask = (string.Join("$|", searchPatterns) + "$")
				.Replace(".", "[.]").Replace("*", ".*").Replace("?", ".");
			var searchRegex = new Regex(searchMask, RegexOptions.IgnoreCase /*| RegexOptions.Compiled*/);

			switch (_settings.SearchMethod)
			{
				case SearchMethod.DIRECTORY_ENUMERATE_FILES:
					found.AddRange(GetFilesDotNet(searchDirectory, searchRegex));
					break;
				case SearchMethod.FAST_FILE_INFO:
					found.AddRange(GetFilesFastInfo(searchDirectory, searchRegex));
					break;
				case SearchMethod.FAST_FILE_INFO_WITH_EXCLUDE:
					found.AddRange(GetFilesFastInfoWithExclude(searchDirectory, searchRegex));
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
					if (_settings.SearchOption == SearchOption.AllDirectories)
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
			return FastFileInfo.EnumerateFiles(path, "*.*", _settings.SearchOption)
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
					if (_settings.SearchOption == SearchOption.AllDirectories)
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
			return _settings.ExcludeDirectory.Any(excluded => directory.StartsWith(excluded, StringComparison.InvariantCultureIgnoreCase));
		}
	}
}
