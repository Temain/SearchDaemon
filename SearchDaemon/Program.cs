using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using Ninject;
using SearchDaemon.Core.Ninject;

namespace SearchDaemon
{
	static class Program
	{
		/// <summary>
		/// Главная точка входа для приложения.
		/// </summary>
		static void Main()
		{
			Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

			using (IKernel kernel = new StandardKernel(new SearchDaemonNinjectModule()))
			{
				var searchService = kernel.Get<SearchService>();
				if (Environment.UserInteractive)
				{
#if DEBUG
					searchService.Start();
					Thread.Sleep(Timeout.Infinite);
#else
					// MessageBox.Show(@"Приложение должно быть установлено в виде службы Windows 
					// и не может быть запущено интерактивно.");
#endif
				}
				else
				{
					ServiceBase.Run(searchService);
				}
			}
		}
	}
}
