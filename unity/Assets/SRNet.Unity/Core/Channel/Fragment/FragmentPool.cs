using System.Collections.Concurrent;

namespace SRNet.Channel
{
	public static class FragmentPool
	{
		public static int MaxCount = 64;

		static ConcurrentQueue<Fragment> s_Pool = new ConcurrentQueue<Fragment>();

		public static void Clear()
		{
			while (!s_Pool.IsEmpty) s_Pool.TryDequeue(out _);
		}

		internal static Fragment Get()
		{
			if (s_Pool.TryDequeue(out var ret))
			{
				return ret;
			}
			return new Fragment();
		}

		internal static void Return(Fragment item)
		{
			if (s_Pool.Count < MaxCount)
			{
				item.OnReturn();
				s_Pool.Enqueue(item);
			}
		}
	}
}