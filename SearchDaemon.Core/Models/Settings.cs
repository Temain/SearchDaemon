using System.IO;

namespace SearchDaemon.Core.Models
{
	public class Settings
	{
		/// <summary>
		/// Тип запуска поиска:
		/// - по таймеру
		/// - по крону
		/// </summary>
		public SearchStartType SearchStartType { get; set; }

		/// <summary>
		/// Интервал запуска поиска при выборе типа запуска по таймеру. 
		/// </summary>
		public int TimerInterval { get; set; }

		/// <summary>
		/// Настройки запуска поиска по крону.
		/// 
		/// *    *    *    *    *  
		/// ┬    ┬    ┬    ┬    ┬
		/// │    │    │    │    │
		/// │    │    │    │    │
		/// │    │    │    │    └───── день недели (0 - 6) (воскресенье=0)
		/// │    │    │    └────────── мксяц (1 - 12)
		/// │    │    └─────────────── день месяца (1 - 31)
		/// │    └──────────────────── часы (0 - 23)
		/// └───────────────────────── минуты (0 - 59)
		///  * * * * *        Каджую минуту.
		///  0 * * * *        Каждый час.
		///  0,1,2 * * * *    0, 1 и 2 минута каждого часа.
		///  */2 * * * *     Каждые 2 минуты.
		///  1-55 * * * *     Каждую минуту через 55-ю минуту.
		///  * 1,10,20 * * *  Каждый 1, 10 и 20 час.
		/// </summary>
		public string Crontab { get; set; }

		/// <summary>
		/// Директории поиска.
		/// </summary>
		public string[] SearchDirectory { get; set; }

		/// <summary>
		/// Сканировать директории поиска параллельно.
		/// </summary>
		public bool SearchParallel { get; set; }

		/// <summary>
		/// Исключеные из поиска директории.
		/// </summary>
		public string[] ExcludeDirectory { get; set; }

		/// <summary>
		/// Опции поиска. Поиск только в выбранной директории или и в поддиректориях.
		/// </summary>
		public SearchOption SearchOption { get; set; }

		/// <summary>
		/// Маски поиска.
		/// </summary>
		public string[] SearchMask { get; set; }

		/// <summary>
		/// Директория файла с результатами поиска.
		/// </summary>
		public string OutputFilePath { get; set; }

		/// <summary>
		/// Метод поиска.
		/// Варианты:
		///	1. Стандартными средствами
		///	2. Обертка над методами WinAPI(Версия #1)
		///	3. Обертка над методами WinAPI (Версия #2)
		/// По умолчанию: 1
		/// </summary>
		public SearchMethod SearchMethod { get; set; }

		/// <summary>
		/// Удалять найденные файлы.
		/// Варианты: 0 или 1
		/// По умолчанию: 0
		/// </summary>
		public bool DeleteFiles { get; set; }

		/// <summary>
		/// Режим тестирования, выполнение поиска разными методами N итераций.
		/// Варианты: 0 или 1
		/// По умолчанию: 0
		/// </summary>
		public bool TestMode { get; set; }

		/// <summary>
		/// Флаг загрузки настроек.
		/// </summary>
		public bool Loaded { get; set; }
	}
}
