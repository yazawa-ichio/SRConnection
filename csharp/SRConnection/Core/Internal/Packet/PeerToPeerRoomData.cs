namespace SRConnection.Packet
{
	internal readonly struct PeerToPeerRoomData
	{
		public readonly int ConnectionId;
		public readonly byte CookieSize;
		public readonly byte[] Cookie;
		public readonly byte RandamSize;
		public readonly byte[] Randam;

		public PeerToPeerRoomData(int connectionId, byte[] cookie, byte[] randam)
		{
			ConnectionId = connectionId;
			CookieSize = (byte)cookie.Length;
			Cookie = cookie;
			RandamSize = (byte)randam.Length;
			Randam = randam;
		}

		public int GetSize()
		{
			return sizeof(int) + sizeof(byte) + CookieSize + sizeof(byte) + RandamSize;
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
			BinaryUtil.Write(ConnectionId, buf, ref offset);
			buf[offset++] = CookieSize;
			BinaryUtil.Write(Cookie, buf, ref offset);
			buf[offset++] = RandamSize;
			BinaryUtil.Write(Randam, buf, ref offset);
			return offset;
		}

		public static bool TryUnpack(byte[] buf, int size, out PeerToPeerRoomData data)
		{
			if (size < 6)
			{
				data = default;
				return false;
			}
			int offset = 0;
			int id = BinaryUtil.ReadInt(buf, ref offset);
			byte cookieSize = buf[offset++];
			byte[] cookie = BinaryUtil.ReadBytes(buf, cookieSize, ref offset);
			byte randamSize = buf[offset++];
			byte[] randam = BinaryUtil.ReadBytes(buf, randamSize, ref offset);
			data = new PeerToPeerRoomData(id, cookie, randam);
			return true;
		}

	}
}