using System.Collections.Generic;

namespace SRNet.Channel
{
	public static class SeqUtil
	{
		class SeqComparer : IComparer<short>
		{
			public int Compare(short x, short y) => SeqUtil.CompareImpl(x, y);
		}

		public static readonly IComparer<short> Comparer = new SeqComparer();

		const short HalfMax = short.MaxValue / 2;

		static int CompareImpl(short x, short y)
		{
			if (x > HalfMax && y < HalfMax)
			{
				return -1;
			}
			return x - y;
		}

		public static short Increment(short x)
		{
			return ++x;
		}

		public static bool IsGreater(short x, short y)
		{
			return CompareImpl(x, y) > 0;
		}

		public static bool IsGreaterEqual(short x, short y)
		{
			return CompareImpl(x, y) >= 0;
		}

	}

}