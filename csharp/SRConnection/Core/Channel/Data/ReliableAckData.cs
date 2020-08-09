namespace SRConnection.Channel
{
	public readonly struct ReliableAckData
	{
		public readonly short Channel;
		public const DataType Type = DataType.ReliableAck;
		public readonly short ReceivedSequence;
		public readonly short NextReceivedSequence;
		public readonly short LastSequence;

		public ReliableAckData(short channel, short receivedSequence, short nextReceivedSequence, short lastSequence)
		{
			Channel = channel;
			ReceivedSequence = receivedSequence;
			NextReceivedSequence = nextReceivedSequence;
			LastSequence = lastSequence;
		}

		public void Pack(byte[] buf, out int size)
		{
			size = 0;
			BinaryUtil.Write(Channel, buf, ref size);
			buf[size++] = (byte)Type;
			BinaryUtil.Write(ReceivedSequence, buf, ref size);
			BinaryUtil.Write(NextReceivedSequence, buf, ref size);
			BinaryUtil.Write(LastSequence, buf, ref size);
		}

		public static bool TryUnpack(byte[] buf, ref int offset, out ReliableAckData data)
		{
			var channel = BinaryUtil.ReadShort(buf, ref offset);
			if ((byte)Type != buf[offset++])
			{
				data = default;
				return false;
			}
			var received = BinaryUtil.ReadShort(buf, ref offset);
			var nextReceived = BinaryUtil.ReadShort(buf, ref offset);
			var last = BinaryUtil.ReadShort(buf, ref offset);
			data = new ReliableAckData(channel, received, nextReceived, last);
			return true;
		}

	}
}