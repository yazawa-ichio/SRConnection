using System;
using System.Collections.Generic;

namespace SRConnection.Channel
{
	public static class FragmentUtil
	{
		public static void GetFragments(byte[] buf, int offset, int size, short id, int fragmentSize, List<Fragment> list)
		{
			list.Clear();
			while (size > 0)
			{
				int count = size > fragmentSize ? fragmentSize : size;
				var fragment = FragmentPool.Get();
				fragment.Id = id;
				fragment.DataSize = (short)count;
				Buffer.BlockCopy(buf, offset, fragment.Data, 0, count);
				offset += count;
				size -= count;
				list.Add(fragment);
			}
			for (short i = 0; i < list.Count; i++)
			{
				list[i].Index = i;
				list[i].Length = (short)list.Count;
			}
		}

		public static byte[] ToBytes(this List<Fragment> self)
		{
			int size = 0;
			foreach (var fragment in self)
			{
				size += fragment.DataSize;
			}
			byte[] ret = new byte[size];
			int offset = 0;
			foreach (var fragment in self)
			{
				Buffer.BlockCopy(fragment.Data, 0, ret, offset, fragment.DataSize);
				offset += fragment.DataSize;
			}
			return ret;
		}

		public static void TryReturn(this List<Fragment> self)
		{
			foreach (var item in self)
			{
				item.TryReturn();
			}
			self.Clear();
		}

		public static void AddRef(this List<Fragment> self)
		{
			foreach (var item in self)
			{
				item.AddRef();
			}
		}

		public static void RemoveRef(this List<Fragment> self, bool clear)
		{
			foreach (var item in self)
			{
				item.RemoveRef();
			}
			if (clear) self.Clear();
		}

	}
}