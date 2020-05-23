namespace SRNet.Packet
{
	internal readonly struct Pong : IEncryptPacket
	{
		public const PacketType Type = PacketType.Pong;
		public readonly int ConnectionId;
		public readonly short SendSequence;
		public readonly short ReceiveSequence;

		public Pong(int connectionId, short sendSequence, short receiveSequence)
		{
			ConnectionId = connectionId;
			SendSequence = sendSequence;
			ReceiveSequence = receiveSequence;
		}

		public Pong(int connectionId, PeerEntry peer)
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
			int size = 0;
			buf[size++] = (byte)Type;
			BinaryUtil.Write(id, buf, ref size);
			BinaryUtil.Write(SendSequence, buf, ref size);
			BinaryUtil.Write(ReceiveSequence, buf, ref size);
			encryptor.Encrypt(buf, 5, ref size);
			return size;
		}



		public static bool TryUnpack(byte[] buf, int size, Encryptor encryptor, out Pong packet)
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
			packet = new Pong(id, sendSeq, recvSeq);
			return true;
		}

	}

}