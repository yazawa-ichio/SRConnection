namespace SRNet.Packet
{
	internal readonly struct PeerToPeerAccept
	{
		public const PacketType Type = PacketType.PeerToPeerAccept;
		public readonly int ConnectionId;
		public readonly int Nonce;
		public readonly byte Revision;
		public readonly byte Size;
		public readonly PeerInfo[] Peers;

		public PeerToPeerAccept(int connectionId, int nonce)
		{
			ConnectionId = connectionId;
			Nonce = nonce;
			Revision = 0;
			Size = 0;
			Peers = System.Array.Empty<PeerInfo>();
		}

		public PeerToPeerAccept(int connectionId, int nonce, PeerToPeerList list)
		{
			ConnectionId = connectionId;
			Nonce = nonce;
			Revision = list.Revision;
			Size = list.Size;
			Peers = list.Peers;
		}

		public PeerToPeerList GetPeerToPeerList()
		{
			return new PeerToPeerList(ConnectionId, Revision, Peers);
		}

		public int Pack(byte[] buf, Encryptor encryptor)
		{
			int offset = 0;
			buf[offset++] = (byte)Type;
			BinaryUtil.Write(ConnectionId, buf, ref offset);
			BinaryUtil.Write(Nonce, buf, ref offset);
			buf[offset++] = Revision;
			buf[offset++] = Size;
			for (int i = 0; i < Peers.Length; i++)
			{
				Peers[i].Pack(buf, ref offset);
			}
			encryptor.Encrypt(buf, 9, ref offset);
			return offset;
		}

		public static bool TryUnpack(byte[] buf, int size, Encryptor encryptor, out PeerToPeerAccept packet)
		{
			if (!encryptor.TryDecrypt(buf, 9, ref size))
			{
				packet = default;
				return false;
			}

			int offset = 0;
			if (buf[offset++] != (byte)PeerToPeerAccept.Type)
			{
				packet = default;
				return false;
			}
			int id = BinaryUtil.ReadInt(buf, ref offset);
			int nonce = BinaryUtil.ReadInt(buf, ref offset);
			var revision = buf[offset++];
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
			packet = new PeerToPeerAccept(id, nonce, new PeerToPeerList(id, revision, peers));
			return true;
		}

	}

}