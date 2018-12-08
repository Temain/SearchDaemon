using System.Collections.Generic;

namespace SearchDaemon.Core.Services.Interfaces
{
	public interface ISearchEngine
	{
		IEnumerable<string> Search(string searchDirectory, string[] searchPatterns);
	}
}
