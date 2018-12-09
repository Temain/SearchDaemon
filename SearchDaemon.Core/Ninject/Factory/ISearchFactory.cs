using System.Diagnostics;
using SearchDaemon.Core.Services.Interfaces;

namespace SearchDaemon.Core.Ninject.Factory
{
	public interface ISearchFactory
	{
		ISearchHandler CreateHandler(ISearchEngine searchEngine, EventLog eventLog);

		ISearchEngine CreateEngine();
	}
}
