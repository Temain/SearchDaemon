using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SearchDaemon.Extensions
{
	public static class ConcurrentBagExtensions
	{
		public static void AddRange<T>(this ConcurrentBag<T> bag, IEnumerable<T> toAdd)
		{
			foreach (var element in toAdd)
			{
				bag.Add(element);
			}
		}
	}
}
