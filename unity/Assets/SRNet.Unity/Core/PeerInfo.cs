namespace SRNet.Packet
{
	public readonly struct PeerInfo
	{
		public readonly int ConnectionId;
		public readonly PeerEndPoint EndPont;
		public readonly PeerEndPoint LocalEndPont;
		readonly byte RandamSize;
		public readonly byte[] Randam;

		public PeerInfo(int connectionId, PeerEndPoint endPoint, byte[] randam) : this()
		{
			ConnectionId = connectionId;
			EndPont = endPoint;
			LocalEndPont = new PeerEndPoint(null, 0);
			RandamSize = (byte)randam.Length;
			Randam = randam;
		}

		public PeerInfo(int connectionId, PeerEndPoint endPoint, PeerEndPoint localEndPoint, byte[] randam)
		{
			ConnectionId = connectionId;
			EndPont = endPoint;
			LocalEndPont = localEndPoint;
			RandamSize = (byte)randam.Length;
			Randam = randam;
		}

		internal int GetSize()
		{
			return sizeof(int) + EndPont.GetSize() + LocalEndPont.GetSize() + sizeof(byte) + RandamSize;
		}

		internal void Pack(byte[] buf, ref int offset)
		{
			BinaryUtil.Write(ConnectionId, buf, ref offset);
			EndPont.Pack(buf, ref offset);
			LocalEndPont.Pack(buf, ref offset);
			buf[offset++] = RandamSize;
			BinaryUtil.Write(Randam, buf, ref offset);
		}

		internal static bool TryUnpack(byte[] buf, ref int offset, out PeerInfo info)
		{
			var connectionId = BinaryUtil.ReadInt(buf, ref offset);
			var endPoint = PeerEndPoint.Unpack(buf, ref offset);
			var localEndPoint = PeerEndPoint.Unpack(buf, ref offset);
			var randamSize = buf[offset++];
			var randam = BinaryUtil.ReadBytes(buf, randamSize, ref offset);
			info = new PeerInfo(connectionId, endPoint, localEndPoint, randam);
			return true;
		}

	}
}