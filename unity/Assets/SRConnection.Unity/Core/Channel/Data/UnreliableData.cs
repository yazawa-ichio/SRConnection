using System;

namespace SRConnection.Channel
{
	public readonly struct UnreliableData : IDisposable
	{
		public readonly short Channel;
		public const DataType Type = DataType.Unreliable;
		public readonly short Sequence;
		public readonly Fragment Payload;

		public UnreliableData(short channel, short sequence, Fragment payload)
		{
			Channel = channel;
			Sequence = sequence;
			payload.AddRef();
			Payload = payload;
		}

		public void Pack(byte[] buf, out int size)
		{
			size = 0;
			BinaryUtil.Write(Channel, buf, ref size);
			buf[size++] = (byte)Type;
			BinaryUtil.Write(Sequence, buf, ref size);
			Payload.Write(buf, ref size);
		}

		public void Dispose()
		{
			Payload.RemoveRef();
		}

		public static bool TryUnpack(byte[] buf, ref int offset, out UnreliableData data)
		{
			var channel = BinaryUtil.ReadShort(buf, ref offset);
			var type = buf[offset++];
			var sequence = BinaryUtil.ReadShort(buf, ref offset);
			var payload = FragmentPool.Get();
			payload.Read(buf, ref offset);
			data = new UnreliableData(channel, sequence, payload);
			return true;
		}

	}
}