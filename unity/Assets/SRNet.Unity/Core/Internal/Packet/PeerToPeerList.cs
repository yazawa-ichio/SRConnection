namespace SRNet.Packet
{

	internal readonly struct PeerToPeerList : IEncryptPacket
	{
		public const PacketType Type = PacketType.PeerToPeerList;
		public readonly int ConnectionId;
		public readonly byte Revision;
		public readonly byte Size;
		public readonly PeerInfo[] Peers;

		public PeerToPeerList(int connectionId, byte revision, PeerInfo[] peers)
		{
			ConnectionId = connectionId;
			Revision = revision;
			Size = (byte)peers.Length;
			Peers = peers;
		}

		public int Pack(byte[] buf, Encryptor encryptor)
		{
			return Pack(ConnectionId, buf, encryptor);
		}

		public int Pack(int id, byte[] buf, Encryptor encryptor)
		{
			int offset = 0;
			buf[offset++] = (byte)Type;
			BinaryUtil.Write(id, buf, ref offset);
			buf[offset++] = Revision;
			buf[offset++] = Size;
			for (int i = 0; i < Peers.Length; i++)
			{
				Peers[i].Pack(buf, ref offset);
			}
			encryptor.Encrypt(buf, 6, ref offset);
			return offset;
		}

		public static bool TryUnpack(byte[] buf, int size, Encryptor encryptor, out PeerToPeerList packet)
		{
			int offset = 1;
			var connectionId = BinaryUtil.ReadInt(buf, ref offset);
			var revision = buf[offset++];

			if (!encryptor.TryDecrypt(buf, 6, ref size))
			{
				packet = default;
				return false;
			}

			var peerSize = buf[offset++];
			var peers = new PeerInfo[peerSize];
			for (int i = 0; i < peers.Length; i++)
			{
				if (!PeerInfo.TryUnpack(buf, ref offset, out var info))
				{
					packet = default;
					return false;
				}
				peers[i] = info;
			}
			packet = new PeerToPeerList(connectionId, revision, peers);
			return true;
		}

	}
}