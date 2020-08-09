using System;
using System.Collections.Generic;

namespace SRConnection
{
	internal class IdGenerator : IDisposable
	{
		HashSet<int> m_HashSet = new HashSet<int>();
		RandomProvider m_Random = new RandomProvider();

		public bool AbsOnly;

		public int Gen()
		{
			while (true)
			{
				var id = m_Random.GenInt();
				if (AbsOnly && id < 0)
				{
					id = -id;
				}
				if (m_HashSet.Add(id))
				{
					return id;
				}
			}
		}

		public void Remove(int id)
		{
			m_HashSet.Remove(id);
		}

		public void Dispose()
		{
			m_Random.Dispose();
		}

	}

}