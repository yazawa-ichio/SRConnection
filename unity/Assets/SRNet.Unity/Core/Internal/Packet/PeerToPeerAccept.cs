namespace SRNet.Packet
{
	internal readonly struct PeerToPeerAccept
	{
		public const PacketType Type = PacketType.PeerToPeerAccept;
		public readonly int ConnectionId;
		public readonly int Nonce;
		public static readonly byte[] Data = System.Text.Encoding.UTF8.GetBytes("PeerToPeerAccept");

		public PeerToPeerAccept(int connectionId, int nonce)
		{
			ConnectionId = connectionId;
			Nonce = nonce;
		}

		public int Pack(byte[] buf, Encryptor encryptor)
		{
			int offset = 0;
			buf[offset++] = (byte)Type;
			BinaryUtil.Write(ConnectionId, buf, ref offset);
			BinaryUtil.Write(Nonce, buf, ref offset);
			BinaryUtil.Write(Data, buf, ref offset);
			encryptor.Encrypt(buf, 9, ref offset);
			return offset;
		}

		public static bool TryUnpack(byte[] buf, int size, Encryptor encryptor, out PeerToPeerAccept packet)
		{
			if (!encryptor.TryDecrypt(buf, 9, ref size))
			{
				packet = default;
				return false;
			}

			int offset = 0;
			if (size < sizeof(byte) + sizeof(int) + PeerToPeerAccept.Data.Length || buf[offset++] != (byte)PeerToPeerAccept.Type)
			{
				packet = default;
				return false;
			}
			int id = BinaryUtil.ReadInt(buf, ref offset);
			int nonce = BinaryUtil.ReadInt(buf, ref offset);
			for (int i = 0; i < PeerToPeerAccept.Data.Length; i++)
			{
				if (buf[offset++] != PeerToPeerAccept.Data[i])
				{
					packet = default;
					return false;
				}
			}
			packet = new PeerToPeerAccept(id, nonce);
			return true;
		}

	}

}