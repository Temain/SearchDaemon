using System.Diagnostics;
using SearchDaemon.Core.Models;
using SearchDaemon.Core.Services.Interfaces;

namespace SearchDaemon.Core.Ninject.Factory
{
	public interface ISearchFactory
	{
		ISearchHandler CreateHandler(ISearchEngine searchEngine, Settings settings, EventLog eventLog);

		ISearchEngine CreateEngine(Settings settings);
	}
}
