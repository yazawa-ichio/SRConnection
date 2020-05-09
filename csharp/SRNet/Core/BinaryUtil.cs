using System;

namespace SRNet
{
	public static class BinaryUtil
	{
		public static void Write(byte value, byte[] buf, ref int offset)
		{
			buf[offset++] = (byte)(value);
		}

		public static void Write(short value, byte[] buf, ref int offset)
		{
			buf[offset++] = (byte)(value);
			buf[offset++] = (byte)(value >> 8);
		}

		public static void Write(int value, byte[] buf, ref int offset)
		{
			buf[offset++] = (byte)(value);
			buf[offset++] = (byte)(value >> 8);
			buf[offset++] = (byte)(value >> 16);
			buf[offset++] = (byte)(value >> 24);
		}

		public static void Write(long value, byte[] buf, ref int offset)
		{
			buf[offset++] = (byte)(value);
			buf[offset++] = (byte)(value >> 8);
			buf[offset++] = (byte)(value >> 16);
			buf[offset++] = (byte)(value >> 24);
			buf[offset++] = (byte)(value >> 32);
			buf[offset++] = (byte)(value >> 40);
			buf[offset++] = (byte)(value >> 48);
			buf[offset++] = (byte)(value >> 56);
		}

		public static void Write(in ArraySegment<byte> value, byte[] buf, ref int offset)
		{
			Buffer.BlockCopy(value.Array, value.Offset, buf, offset, value.Count);
			offset += value.Count;
		}


		public static void Write(byte[] value, byte[] buf, ref int offset)
		{
			Buffer.BlockCopy(value, 0, buf, offset, value.Length);
			offset += value.Length;
		}

		public static void Write(string value, byte[] buf, ref int offset)
		{
			offset += System.Text.Encoding.UTF8.GetBytes(value, 0, value.Length, buf, offset);
		}

		public static short ReadShort(byte[] buf, ref int offset)
		{
			return (short)(buf[offset++] | (buf[offset++] << 8));
		}

		public static int ReadInt(byte[] buf, ref int offset)
		{
			return (buf[offset++] << 0) | (buf[offset++] << 8) | (buf[offset++] << 16) | (buf[offset++] << 24);
		}

		public static long ReadLong(byte[] buf, ref int offset)
		{
			int i1 = (buf[offset++] << 0) | (buf[offset++] << 8) | (buf[offset++] << 16) | (buf[offset++] << 24);
			int i2 = (buf[offset++] << 0) | (buf[offset++] << 8) | (buf[offset++] << 16) | (buf[offset++] << 24);
			return (uint)i1 | ((long)i2 << 32);
		}

		public static byte[] ReadBytes(byte[] buf, int size, ref int offset)
		{
			byte[] ret = new byte[size];
			Buffer.BlockCopy(buf, offset, ret, 0, size);
			offset += size;
			return ret;
		}

		public static ArraySegment<byte> ReadArraySegment(byte[] buf, int size, ref int offset)
		{
			var ret = new ArraySegment<byte>(buf, offset, size);
			offset += size;
			return ret;
		}

		public static string ReadString(byte[] buf, int size, ref int offset)
		{
			var ret = System.Text.Encoding.UTF8.GetString(buf, offset, size);
			offset += size;
			return ret;
		}


	}


	public static class NetBinaryUtil
	{
		public static void Write(byte value, byte[] buf, ref int offset)
		{
			buf[offset++] = (byte)(value);
		}

		public static void Write(ushort value, byte[] buf, ref int offset)
		{
			buf[offset++] = (byte)(value >> 8);
			buf[offset++] = (byte)(value);
		}

		public static void Write(uint value, byte[] buf, ref int offset)
		{
			buf[offset++] = (byte)(value >> 24);
			buf[offset++] = (byte)(value >> 16);
			buf[offset++] = (byte)(value >> 8);
			buf[offset++] = (byte)(value);
		}

		public static void Write(ulong value, byte[] buf, ref int offset)
		{
			buf[offset++] = (byte)(value >> 56);
			buf[offset++] = (byte)(value >> 48);
			buf[offset++] = (byte)(value >> 40);
			buf[offset++] = (byte)(value >> 32);
			buf[offset++] = (byte)(value >> 24);
			buf[offset++] = (byte)(value >> 16);
			buf[offset++] = (byte)(value >> 8);
			buf[offset++] = (byte)(value);
		}

		public static void Write(in ArraySegment<byte> value, byte[] buf, ref int offset)
		{
			Buffer.BlockCopy(value.Array, value.Offset, buf, offset, value.Count);
			offset += value.Count;
		}


		public static void Write(byte[] value, byte[] buf, ref int offset)
		{
			Buffer.BlockCopy(value, 0, buf, offset, value.Length);
			offset += value.Length;
		}

		public static void Write(byte[] value, int size, byte[] buf, ref int offset)
		{
			Buffer.BlockCopy(value, 0, buf, offset, size);
			offset += size;
		}

		public static void Write(string value, byte[] buf, ref int offset)
		{
			offset += System.Text.Encoding.UTF8.GetBytes(value, 0, value.Length, buf, offset);
		}

		public static ushort ReadUShort(byte[] buf, ref int offset)
		{
			return (ushort)(buf[offset++] << 8 | (buf[offset++]));
		}

		public static short ReadShort(byte[] buf, ref int offset)
		{
			return (short)(buf[offset++] << 8 | (buf[offset++]));
		}

		public static int ReadInt(byte[] buf, ref int offset)
		{
			return (buf[offset++] << 24) | (buf[offset++] << 16) | (buf[offset++] << 8) | (buf[offset++] << 0);
		}

		public static uint ReadUInt(byte[] buf, ref int offset)
		{
			return ((uint)buf[offset++] << 24) | ((uint)buf[offset++] << 16) | ((uint)buf[offset++] << 8) | (uint)buf[offset++];
		}

		public static long ReadLong(byte[] buf, ref int offset)
		{
			int i1 = (buf[offset++] << 24) | (buf[offset++] << 16) | (buf[offset++] << 8) | (buf[offset++] << 0);
			int i2 = (buf[offset++] << 24) | (buf[offset++] << 16) | (buf[offset++] << 8) | (buf[offset++] << 0);
			return (uint)i2 | ((long)i1 << 32);
		}

		public static byte[] ReadBytes(byte[] buf, int size, ref int offset)
		{
			byte[] ret = new byte[size];
			Buffer.BlockCopy(buf, offset, ret, 0, size);
			offset += size;
			return ret;
		}

		public static ArraySegment<byte> ReadArraySegment(byte[] buf, int size, ref int offset)
		{
			var ret = new ArraySegment<byte>(buf, offset, size);
			offset += size;
			return ret;
		}

		public static string ReadString(byte[] buf, int size, ref int offset)
		{
			var ret = System.Text.Encoding.UTF8.GetString(buf, offset, size);
			offset += size;
			return ret;
		}


	}
}