using System;

namespace SRConnection.Channel
{

	public readonly struct ReliableData : IDisposable
	{
		public readonly short Channel;
		public const DataType Type = DataType.Reliable;
		public readonly short Sequence;
		public readonly short ReceiveSequence;
		public readonly Fragment Payload;

		public ReliableData(short channel, short sequence, Fragment payload)
		{
			Channel = channel;
			Sequence = sequence;
			payload.AddRef();
			Payload = payload;
			//ReceiveSequenceはパック時に追加するので0で問題ない
			ReceiveSequence = 0;
		}

		private ReliableData(short channel, short sequence, short receiveSequence, Fragment payload)
		{
			Channel = channel;
			Sequence = sequence;
			ReceiveSequence = receiveSequence;
			payload.AddRef();
			Payload = payload;
		}

		public void Pack(byte[] buf, short receiveSequence, out int size)
		{
			size = 0;
			BinaryUtil.Write(Channel, buf, ref size);
			buf[size++] = (byte)Type;
			BinaryUtil.Write(Sequence, buf, ref size);
			BinaryUtil.Write(receiveSequence, buf, ref size);
			Payload.Write(buf, ref size);
		}

		public void Dispose()
		{
			Payload.RemoveRef();
		}

		public static bool TryUnpack(byte[] buf, ref int offset, out ReliableData data)
		{
			var channel = BinaryUtil.ReadShort(buf, ref offset);
			if ((byte)Type != buf[offset++])
			{
				data = default;
				return false;
			}
			var sequence = BinaryUtil.ReadShort(buf, ref offset);
			var receiveSequence = BinaryUtil.ReadShort(buf, ref offset);
			var payload = FragmentPool.Get();
			payload.Read(buf, ref offset);
			data = new ReliableData(channel, sequence, receiveSequence, payload);
			return true;
		}

	}
}