namespace SRConnection.Packet
{
	internal readonly struct ClientHello
	{
		public const PacketType Type = PacketType.ClientHello;
		public readonly byte MajorVersion;
		public readonly byte MinorVersion;

		public ClientHello(byte majorVersion, byte minorVersion)
		{
			MajorVersion = majorVersion;
			MinorVersion = minorVersion;
		}

		public int GetSize()
		{
			// Type + MajorVersion + MinorVersion
			return sizeof(byte) + sizeof(byte) + sizeof(byte);
		}

		public byte[] Pack()
		{
			byte[] buf = new byte[GetSize()];
			buf[0] = (byte)PacketType.ClientHello;
			buf[1] = MajorVersion;
			buf[2] = MinorVersion;
			return buf;
		}

		public static bool TryUnpack(byte[] buf, int offset, int size, out ClientHello packet)
		{
			if (size <= 2)
			{
				packet = default;
				return false;
			}
			packet = new ClientHello(buf[offset], buf[offset + 1]);
			return true;
		}


	}
}