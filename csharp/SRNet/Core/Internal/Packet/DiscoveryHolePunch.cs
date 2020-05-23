namespace SRNet.Packet
{
	internal readonly struct DiscoveryHolePunch
	{
		public const PacketType Type = PacketType.DiscoveryHolePunch;
		public static readonly byte[] Data = System.Text.Encoding.UTF8.GetBytes("DiscoveryHolePunch");
		static readonly byte[] s_Packet;

		static DiscoveryHolePunch()
		{
			s_Packet = new byte[Data.Length + 1];
			s_Packet[0] = (byte)Type;
			System.Buffer.BlockCopy(Data, 0, s_Packet, 1, Data.Length);
		}

		public byte[] Pack()
		{
			return s_Packet;
		}

		public static bool TryUnpack(byte[] buf, int size, out DiscoveryHolePunch packet)
		{
			if (sizeof(byte) + Data.Length > size)
			{
				packet = default;
				return false;
			}
			int offset = 1;
			for (var i = 0; i < Data.Length; i++)
			{
				if (Data[i] != buf[offset + i])
				{
					packet = default;
					return false;
				}
			}
			packet = new DiscoveryHolePunch();
			return true;
		}

	}
}