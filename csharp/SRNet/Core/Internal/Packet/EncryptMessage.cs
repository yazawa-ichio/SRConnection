using System;

namespace SRNet.Packet
{

	internal readonly struct EncryptMessage : IEncryptPacket
	{
		public const PacketType Type = PacketType.EncryptMessage;
		public readonly int ConnectionId;
		public readonly short SendSequence;
		public readonly short ReceiveSequence;
		public readonly ArraySegment<byte> Payload;

		public EncryptMessage(int connectionId, PeerEntry peer, ArraySegment<byte> payload)
		{
			ConnectionId = connectionId;
			SendSequence = peer.IncrementSendSequence();
			ReceiveSequence = peer.ReceiveSequence;
			Payload = payload;
		}

		public EncryptMessage(int connectionId, short sendSequence, short receiveSequence, ArraySegment<byte> payload)
		{
			ConnectionId = connectionId;
			SendSequence = sendSequence;
			ReceiveSequence = receiveSequence;
			Payload = payload;
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
			BinaryUtil.Write(Payload, buf, ref offset);
			encryptor.Encrypt(buf, 5, ref offset);
			return offset;
		}

		public static bool TryUnpack(byte[] buf, int size, Encryptor encryptor, out EncryptMessage packet)
		{
			if (sizeof(byte) + sizeof(int) + sizeof(short) + sizeof(short) > size)
			{
				packet = default;
				return false;
			}
			if (!encryptor.TryDecrypt(buf, 5, ref size))
			{
				packet = default;
				return false;
			}
			int offest = 1;
			var id = BinaryUtil.ReadInt(buf, ref offest);
			var sendSeq = BinaryUtil.ReadShort(buf, ref offest);
			var ackSeq = BinaryUtil.ReadShort(buf, ref offest);
			packet = new EncryptMessage(id, sendSeq, ackSeq, new ArraySegment<byte>(buf, offest, size - offest));
			return true;
		}

	}

}