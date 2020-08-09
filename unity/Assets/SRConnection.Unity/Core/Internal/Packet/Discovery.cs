using System;

namespace SRConnection.Packet
{

	internal readonly struct Discovery
	{
		public const PacketType Type = PacketType.Discovery;
		public readonly short QuerySize;
		public readonly ArraySegment<byte> Query;

		public Discovery(ArraySegment<byte> query)
		{
			QuerySize = (short)query.Count;
			Query = query;
		}

		public int GetSize()
		{
			return sizeof(byte) + sizeof(short) + QuerySize;
		}

		public byte[] Pack()
		{
			byte[] buf = new byte[GetSize()];
			int offset = 0;
			buf[offset++] = (byte)Type;
			BinaryUtil.Write(QuerySize, buf, ref offset);
			BinaryUtil.Write(Query, buf, ref offset);
			return buf;
		}

		public static bool TryUnpack(byte[] buf, int size, out Discovery packet)
		{
			if (size < 3 || buf[0] != (byte)Type)
			{
				packet = default;
				return false;
			}
			int offset = 1;
			var querySize = BinaryUtil.ReadShort(buf, ref offset);
			packet = new Discovery(BinaryUtil.ReadArraySegment(buf, querySize, ref offset));
			return true;
		}


	}

}