using Ninject.Extensions.Factory;
using Ninject.Modules;
using SearchDaemon.Core.Ninject.Factory;
using SearchDaemon.Core.Services;
using SearchDaemon.Core.Services.Interfaces;

namespace SearchDaemon.Core.Ninject
{
	public class SearchDaemonNinjectModule : NinjectModule
	{
		public override void Load()
		{
			Bind<ISearchHandler>().To<SearchHandler>();
			Bind<ISearchEngine>().To<SearchEngine>();

			Bind<ISearchFactory>().ToFactory();
		}
	}
}
