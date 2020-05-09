using System;
using System.Threading;

namespace SRNet.Channel
{

	public class Fragment
	{
		public const int Size = 1024;
		public short Id;
		public short Length;
		public short Index;
		public short DataSize;
		public byte[] Data = new byte[Size];


		internal void OnReturn()
		{
			Index = -1;
			Length = -1;
			DataSize = 0;
		}

		int m_RefCount = 0;
		public void AddRef()
		{
			Interlocked.Increment(ref m_RefCount);
		}

		public void RemoveRef()
		{
			if (Interlocked.Decrement(ref m_RefCount) == 0)
			{
				FragmentPool.Return(this);
			}
		}

		public bool TryReturn()
		{
			if (m_RefCount == 0)
			{
				FragmentPool.Return(this);
				return true;
			}
			return false;
		}

		internal void Write(byte[] buf, ref int offset)
		{
			BinaryUtil.Write(Id, buf, ref offset);
			BinaryUtil.Write(Length, buf, ref offset);
			BinaryUtil.Write(Index, buf, ref offset);
			BinaryUtil.Write(DataSize, buf, ref offset);
			Buffer.BlockCopy(Data, 0, buf, offset, DataSize);
			offset += DataSize;
		}

		internal void Read(byte[] buf, ref int offset)
		{
			Id = BinaryUtil.ReadShort(buf, ref offset);
			Length = BinaryUtil.ReadShort(buf, ref offset);
			Index = BinaryUtil.ReadShort(buf, ref offset);
			DataSize = BinaryUtil.ReadShort(buf, ref offset);
			Buffer.BlockCopy(buf, offset, Data, 0, DataSize);
			offset += DataSize;
		}

	}
}