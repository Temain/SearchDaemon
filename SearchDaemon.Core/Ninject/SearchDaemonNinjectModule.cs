using Ninject.Modules;
using SearchDaemon.Core.Ninject.Factory;
using SearchDaemon.Core.Ninject.Factory.Interfaces;
using SearchDaemon.Core.Services;
using SearchDaemon.Core.Services.Interfaces;

namespace SearchDaemon.Core.Ninject
{
	public class SearchDaemonNinjectModule : NinjectModule
	{
		public override void Load()
		{
			Bind<ISearchFactory>().To<SearchFactory>();
			Bind<ISearchHandler>().To<SearchHandler>();
			Bind<ISearchEngine>().To<SearchEngine>();
		}
	}
}
