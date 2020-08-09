namespace SRConnection.Packet
{
	internal readonly struct Disconnect
	{
		public const PacketType Type = PacketType.Disconnect;
		public readonly int ConnectionId;
		public readonly short Sequence;
		public static readonly byte[] Data = System.Text.Encoding.UTF8.GetBytes("Disconnect");

		public Disconnect(int id, short sequence)
		{
			ConnectionId = id;
			Sequence = sequence;
		}

		public int Pack(byte[] buf, Encryptor encryptor)
		{
			int offset = 0;
			buf[offset++] = (byte)Type;
			BinaryUtil.Write(ConnectionId, buf, ref offset);
			BinaryUtil.Write(Sequence, buf, ref offset);
			BinaryUtil.Write(Data, buf, ref offset);
			encryptor.Encrypt(buf, 5, ref offset);
			return offset;
		}

		public static bool TryUnpack(byte[] buf, int size, Encryptor encryptor, out Disconnect packet)
		{
			if (sizeof(byte) + sizeof(int) + sizeof(short) + Data.Length > size)
			{
				packet = default;
				return false;
			}
			if (!encryptor.TryDecrypt(buf, 5, ref size))
			{
				packet = default;
				return false;
			}
			int offset = 1;
			var id = BinaryUtil.ReadInt(buf, ref offset);
			var seq = BinaryUtil.ReadShort(buf, ref offset);
			for (var i = 0; i < Data.Length; i++)
			{
				if (Data[i] != buf[offset + i])
				{
					packet = default;
					return false;
				}
			}
			packet = new Disconnect(id, seq);
			return true;
		}

	}

}