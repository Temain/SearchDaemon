using System.Diagnostics;
using Ninject;
using Ninject.Parameters;
using SearchDaemon.Core.Ninject.Factory.Interfaces;
using SearchDaemon.Core.Services.Interfaces;

namespace SearchDaemon.Core.Ninject.Factory
{
	public class SearchFactory : ISearchFactory
	{
		private readonly IKernel _kernel;

		public SearchFactory(IKernel kernel)
		{
			_kernel = kernel;
		}

		public ISearchEngine CreateEngine()
		{
			return _kernel.Get<ISearchEngine>();
		}

		public ISearchHandler CreateHandler(ISearchEngine searchEngine, EventLog eventLog)
		{
			return _kernel.Get<ISearchHandler>(
				new ConstructorArgument("searchEngine", searchEngine),
				new ConstructorArgument("eventLog", eventLog));
		}
	}
}
