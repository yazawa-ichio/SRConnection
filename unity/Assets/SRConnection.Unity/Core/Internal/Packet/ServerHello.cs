namespace SRConnection.Packet
{
	internal readonly struct ServerHello
	{
		public const byte Type = (byte)PacketType.ServerHello;
		public readonly byte MajorVersion;
		public readonly byte MinorVersion;
		public readonly byte[] Cookie;

		public ServerHello(byte majorVersion, byte minorVersion, byte[] cookie)
		{
			MajorVersion = majorVersion;
			MinorVersion = minorVersion;
			Cookie = cookie;
		}

		public int GetSize()
		{
			// Type + MajorVersion + MinorVersion + Cookie
			return sizeof(byte) + sizeof(byte) + sizeof(byte) + Cookie.Length;
		}

		public byte[] Pack()
		{
			var buf = new byte[GetSize()];
			int offset = 0;
			buf[offset++] = (byte)Type;
			buf[offset++] = MajorVersion;
			buf[offset++] = MinorVersion;
			BinaryUtil.Write(Cookie, buf, ref offset);
			return buf;
		}

		public static bool TryUnpack(byte[] buf, int size, out ServerHello packet)
		{
			if (size < 3 || buf[0] != (byte)ServerHello.Type)
			{
				packet = default;
				return false;
			}
			int offset = 3;
			var cookie = BinaryUtil.ReadBytes(buf, size - offset, ref offset);
			packet = new ServerHello(buf[1], buf[2], cookie);
			return true;
		}


	}
}