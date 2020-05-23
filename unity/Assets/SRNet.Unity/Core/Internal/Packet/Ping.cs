namespace SRNet.Packet
{
	internal readonly struct Ping : IEncryptPacket
	{
		public const PacketType Type = PacketType.Ping;
		public readonly int ConnectionId;
		public readonly short SendSequence;
		public readonly short ReceiveSequence;

		public Ping(int connectionId, short sendSequence, short receiveSequence)
		{
			ConnectionId = connectionId;
			SendSequence = sendSequence;
			ReceiveSequence = receiveSequence;
		}

		public Ping(int connectionId, PeerEntry peer)
		{
			ConnectionId = connectionId;
			SendSequence = peer.IncrementSendSequence();
			ReceiveSequence = peer.ReceiveSequence;
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
			BinaryUtil.Write(SendSequence, buf, ref offset);
			BinaryUtil.Write(ReceiveSequence, buf, ref offset);
			encryptor.Encrypt(buf, 5, ref offset);
			return offset;
		}

		public static bool TryUnpack(byte[] buf, int size, Encryptor encryptor, out Ping packet)
		{
			int offset = 0;
			if (sizeof(byte) + sizeof(int) + sizeof(short) + sizeof(short) > size || buf[offset++] != (byte)Type)
			{
				packet = default;
				return false;
			}
			if (!encryptor.TryDecrypt(buf, 5, ref size))
			{
				packet = default;
				return false;
			}
			var id = BinaryUtil.ReadInt(buf, ref offset);
			var sendSeq = BinaryUtil.ReadShort(buf, ref offset);
			var recvSeq = BinaryUtil.ReadShort(buf, ref offset);
			packet = new Ping(id, sendSeq, recvSeq);
			return true;
		}

	}

}