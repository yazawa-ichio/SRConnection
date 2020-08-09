using System;
using System.Security.Cryptography;

namespace SRConnection
{
	internal static class Random
	{
		static RandomProvider s_Impl = new RandomProvider();

		public static int GenInt()
		{
			lock (s_Impl)
			{
				return s_Impl.GenInt();
			}
		}

		public static short GenShort()
		{
			lock (s_Impl)
			{
				return s_Impl.GenShort();
			}
		}

		public static byte[] GenBytes(int size)
		{
			lock (s_Impl)
			{
				byte[] buf = new byte[size];
				s_Impl.GenBytes(buf);
				return buf;
			}
		}

	}

	internal class RandomProvider : IDisposable
	{
		RNGCryptoServiceProvider m_Provider = new RNGCryptoServiceProvider();
		byte[] m_Buffer = new byte[8];

		public int GenInt()
		{
			m_Provider.GetBytes(m_Buffer, 0, 4);
			int offset = 0;
			return BinaryUtil.ReadInt(m_Buffer, ref offset);
		}

		public short GenShort()
		{
			m_Provider.GetBytes(m_Buffer, 0, 2);
			int offset = 0;
			return BinaryUtil.ReadShort(m_Buffer, ref offset);
		}

		public void GenBytes(byte[] buf)
		{
			GenBytes(buf, 0, buf.Length);
		}

		public void GenBytes(byte[] buf, int offst, int size)
		{
			m_Provider.GetBytes(buf, offst, size);
		}

		public void Dispose()
		{
			m_Provider.Dispose();
			m_Provider = null;
		}

	}
}