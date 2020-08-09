using System;
using System.Net;

namespace SRConnection.Packet
{
	internal readonly struct DiscoveryResponse
	{
		public const PacketType Type = PacketType.DiscoveryResponse;
		public readonly int Port;
		public readonly short NameSize;
		public readonly ArraySegment<byte> Name;
		public readonly short DataSize;
		public readonly ArraySegment<byte> Data;

		public DiscoveryResponse(int port, ArraySegment<byte> name, ArraySegment<byte> payload)
		{
			Port = port;
			NameSize = (short)name.Count;
			Name = name;
			DataSize = (short)payload.Count;
			Data = payload;
		}

		public DiscoveryRoom CreateRoom(IPAddress address, int discoveryPort)
		{
			var name = System.Text.Encoding.UTF8.GetString(Name.Array, Name.Offset, Name.Count);
			var data = new byte[DataSize];
			Buffer.BlockCopy(Data.Array, Data.Offset, data, 0, Data.Count);
			var room = new DiscoveryRoom(address, Port, name, data, discoveryPort);
			return room;
		}

		public int GetSize()
		{
			return sizeof(byte) + sizeof(int) + sizeof(short) + NameSize + sizeof(short) + DataSize;
		}

		public byte[] Pack()
		{
			byte[] buf = new byte[GetSize()];
			int offset = 0;
			buf[offset++] = (byte)Type;
			BinaryUtil.Write(Port, buf, ref offset);
			BinaryUtil.Write(NameSize, buf, ref offset);
			BinaryUtil.Write(Name, buf, ref offset);
			BinaryUtil.Write(DataSize, buf, ref offset);
			BinaryUtil.Write(Data, buf, ref offset);
			return buf;
		}

		public static bool TryUnpack(byte[] buf, int size, out DiscoveryResponse packet)
		{
			if (buf[0] != (byte)Type)
			{
				packet = default;
				return false;
			}
			int offset = 1;
			int port = BinaryUtil.ReadInt(buf, ref offset);
			var nameSize = BinaryUtil.ReadShort(buf, ref offset);
			var name = BinaryUtil.ReadArraySegment(buf, nameSize, ref offset);
			var dataSize = BinaryUtil.ReadShort(buf, ref offset);
			var data = BinaryUtil.ReadArraySegment(buf, dataSize, ref offset);
			packet = new DiscoveryResponse(port, name, data);
			return true;
		}


	}

}