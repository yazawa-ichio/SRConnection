namespace SRNet.Crypto
{
	public static class Pack
	{
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

		public static uint ReadUInt(byte[] buf, ref int offset)
		{
			return ((uint)buf[offset++] << 24) | ((uint)buf[offset++] << 16) | ((uint)buf[offset++] << 8) | (uint)buf[offset++];
		}

		public static ulong ReadULong(byte[] buf, ref int offset)
		{
			uint i1 = ((uint)buf[offset++] << 24) | ((uint)buf[offset++] << 16) | ((uint)buf[offset++] << 8) | (uint)buf[offset++];
			uint i2 = ((uint)buf[offset++] << 24) | ((uint)buf[offset++] << 16) | ((uint)buf[offset++] << 8) | (uint)buf[offset++];
			return (ulong)i2 | ((ulong)i1 << 32);
		}

	}
}