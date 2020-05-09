namespace SRNet.Packet
{
	internal readonly struct HandshakeAccept
	{
		public const PacketType Type = PacketType.HandshakeAccept;
		public readonly int ConnectionId;
		public static readonly byte[] Data = System.Text.Encoding.UTF8.GetBytes("HandshakeAccept");

		public HandshakeAccept(int connectionId)
		{
			ConnectionId = connectionId;
		}

		public int Pack(byte[] buf, Encryptor encryptor)
		{
			int offset = 0;
			buf[offset++] = (byte)Type;
			BinaryUtil.Write(ConnectionId, buf, ref offset);
			BinaryUtil.Write(Data, buf, ref offset);
			encryptor.Encrypt(buf, 5, ref offset);
			return offset;
		}

		public static bool TryUnpack(byte[] buf, int size, Encryptor encryptor, out HandshakeAccept packet)
		{
			if (!encryptor.TryDecrypt(buf, 5, ref size))
			{
				packet = default;
				return false;
			}

			int offset = 0;
			if (size < sizeof(byte) + sizeof(int) + HandshakeAccept.Data.Length || buf[offset++] != (byte)HandshakeAccept.Type)
			{
				packet = default;
				return false;
			}
			int id = BinaryUtil.ReadInt(buf, ref offset);
			for (int i = 0; i < HandshakeAccept.Data.Length; i++)
			{
				if (buf[offset++] != HandshakeAccept.Data[i])
				{
					packet = default;
					return false;
				}
			}
			packet = new HandshakeAccept(id);
			return true;
		}

	}

}