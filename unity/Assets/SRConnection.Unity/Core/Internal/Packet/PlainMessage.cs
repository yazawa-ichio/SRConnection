using System;

namespace SRConnection.Packet
{
	internal readonly struct PlainMessage
	{
		public const PacketType Type = PacketType.PlainMessage;
		public readonly int ConnectionId;
		public readonly short SendSequence;
		public readonly short ReceiveSequence;
		public readonly ArraySegment<byte> Payload;

		public PlainMessage(int connectionId, PeerEntry peer, ArraySegment<byte> payload)
		{
			ConnectionId = connectionId;
			SendSequence = peer.IncrementSendSequence();
			ReceiveSequence = peer.ReceiveSequence;
			Payload = payload;
		}

		public PlainMessage(int connectionId, short sendSequence, short receiveSequence, ArraySegment<byte> payload)
		{
			ConnectionId = connectionId;
			SendSequence = sendSequence;
			ReceiveSequence = receiveSequence;
			Payload = payload;
		}

		public int Pack(byte[] buf)
		{
			int offset = 0;
			buf[offset++] = (byte)Type;
			BinaryUtil.Write(ConnectionId, buf, ref offset);
			BinaryUtil.Write(SendSequence, buf, ref offset);
			BinaryUtil.Write(ReceiveSequence, buf, ref offset);
			BinaryUtil.Write(Payload, buf, ref offset);
			return offset;
		}

		public static bool TryUnpack(byte[] buf, int size, out PlainMessage packet)
		{
			if (sizeof(byte) + sizeof(int) + sizeof(short) + sizeof(short) > size)
			{
				packet = default;
				return false;
			}
			int offest = 1;
			var id = BinaryUtil.ReadInt(buf, ref offest);
			var sendSeq = BinaryUtil.ReadShort(buf, ref offest);
			var ackSeq = BinaryUtil.ReadShort(buf, ref offest);
			packet = new PlainMessage(id, sendSeq, ackSeq, new ArraySegment<byte>(buf, offest, size - offest));
			return true;
		}

	}

}