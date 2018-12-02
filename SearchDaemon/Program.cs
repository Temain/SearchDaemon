using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SearchDaemon
{
	static class Program
	{
		/// <summary>
		/// Главная точка входа для приложения.
		/// </summary>
		static void Main()
		{
			if (Environment.UserInteractive)
			{
#if DEBUG
				(new SearchService()).Start();
				Thread.Sleep(Timeout.Infinite);
#else
				// MessageBox.Show("Приложение должно быть установлено в виде службы Windows и не может быть запущено интерактивно.");
#endif
			}
			else
			{
				ServiceBase.Run(new SearchService());
			}
		}
	}
}
