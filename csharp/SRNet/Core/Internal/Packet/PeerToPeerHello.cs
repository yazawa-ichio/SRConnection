namespace SRNet.Packet
{
	internal readonly struct PeerToPeerHello
	{
		public const PacketType Type = PacketType.PeerToPeerHello;
		public readonly int ConnectionId;
		public readonly byte CookieSize;
		public readonly byte[] Cookie;

		public PeerToPeerHello(int connectionId, byte[] cookie)
		{
			ConnectionId = connectionId;
			if (cookie != null)
			{
				CookieSize = (byte)cookie.Length;
				Cookie = cookie;
			}
			else
			{
				CookieSize = 0;
				Cookie = default;
			}
		}

		public int GetSize()
		{
			return sizeof(byte) + sizeof(int) + sizeof(byte) + CookieSize;
		}

		public byte[] Pack()
		{
			byte[] buf = new byte[GetSize()];
			Pack(buf);
			return buf;
		}

		public int Pack(byte[] buf)
		{
			int offset = 0;
			buf[offset++] = (byte)Type;
			BinaryUtil.Write(ConnectionId, buf, ref offset);
			buf[offset++] = CookieSize;
			if (CookieSize > 0)
			{
				BinaryUtil.Write(Cookie, buf, ref offset);
			}
			return offset;
		}

		public static bool TryUnpack(byte[] buf, int size, out PeerToPeerHello packet)
		{
			if (size < 5 || buf[0] != (byte)Type)
			{
				packet = default;
				return false;
			}
			int offset = 1;
			int id = BinaryUtil.ReadInt(buf, ref offset);
			byte cookieSize = buf[offset++];
			byte[] cookie = default;
			if (cookieSize > 0)
			{
				cookie = BinaryUtil.ReadBytes(buf, cookieSize, ref offset);
			}
			packet = new PeerToPeerHello(id, cookie);
			return true;
		}

	}
}