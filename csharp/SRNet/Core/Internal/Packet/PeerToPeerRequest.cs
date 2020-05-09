namespace SRNet.Packet
{
	internal readonly struct PeerToPeerRequest
	{
		public const PacketType Type = PacketType.PeerToPeerRequest;
		public readonly int ConnectionId;
		public readonly byte MajorVersion;
		public readonly byte MinorVersion;
		public readonly byte[] Cookie;

		public PeerToPeerRequest(int connectionId, byte[] cookie)
		{
			ConnectionId = connectionId;
			MajorVersion = Protocol.MajorVersion;
			MinorVersion = Protocol.MinorVersion;
			Cookie = cookie;
		}

		public PeerToPeerRequest(int connectionId, byte majorVersion, byte minorVersion, byte[] cookie)
		{
			ConnectionId = connectionId;
			MajorVersion = majorVersion;
			MinorVersion = minorVersion;
			Cookie = cookie;
		}

		public int GetSize()
		{
			return sizeof(byte) + sizeof(int) + sizeof(byte) + sizeof(byte) + Cookie.Length;
		}

		public byte[] Pack()
		{
			byte[] buf = new byte[GetSize()];
			int offset = 0;
			buf[offset++] = (byte)Type;
			BinaryUtil.Write(ConnectionId, buf, ref offset);
			buf[offset++] = MajorVersion;
			buf[offset++] = MinorVersion;
			BinaryUtil.Write(Cookie, buf, ref offset);
			return buf;
		}

		public static bool TryUnpack(CookieProvider cookieProvider, byte[] buf, int size, out PeerToPeerRequest packet)
		{
			if (size < CookieProvider.CookieSize + 7 || !cookieProvider.Check(buf, 7, out var cookie))
			{
				packet = default;
				return false;
			}

			int offest = 1;
			var id = BinaryUtil.ReadInt(buf, ref offest);
			byte majorVersion = buf[offest++];
			byte minorVersion = buf[offest++];

			packet = new PeerToPeerRequest(id, majorVersion, minorVersion, cookie);
			return true;
		}
	}

}