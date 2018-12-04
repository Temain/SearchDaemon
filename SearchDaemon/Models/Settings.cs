using System.IO;

namespace SearchDaemon.Models
{
	public class Settings
	{
		public int TimerInterval { get; set; }

		public string SearchDirectory { get; set; }

		public string ExceptDirectory { get; set; }

		public SearchOption SearchOption { get; set; }

		public string SearchMask { get; set; }

		public string OutputFilePath { get; set; }

		public SearchMethod SearchMethod { get; set; }

		public bool SearchParallel { get; set; }

		public bool Loaded { get; set; }
	}
}
