using System.Net;

namespace SRConnection.Packet
{
	public readonly struct PeerEndPoint
	{
		public readonly byte AddressSize;
		public readonly string Address;
		public readonly int Port;

		public PeerEndPoint(string address, int port)
		{
			if (address == null)
			{
				AddressSize = 0;
				Address = "";
			}
			else
			{
				AddressSize = (byte)System.Text.Encoding.UTF8.GetByteCount(address);
				Address = address;
			}
			Port = port;
		}

		public PeerEndPoint(IPEndPoint endPoint)
		{
			if (endPoint == null)
			{
				AddressSize = 0;
				Address = "";
				Port = 0;
			}
			else
			{
				AddressSize = (byte)System.Text.Encoding.UTF8.GetByteCount(endPoint.Address.ToString());
				Address = endPoint.Address.ToString();
				Port = endPoint.Port;
			}
		}

		public PeerEndPoint(string endPoint)
		{
			if (string.IsNullOrEmpty(endPoint))
			{
				AddressSize = 0;
				Address = "";
				Port = 0;
			}
			else
			{
				var index = endPoint.LastIndexOf(":");
				var address = endPoint.Substring(0, index);
				var port = int.Parse(endPoint.Substring(index + 1));
				AddressSize = (byte)System.Text.Encoding.UTF8.GetByteCount(address);
				Address = address;
				Port = port;
			}
		}

		public int GetSize()
		{
			return sizeof(byte) + AddressSize + sizeof(int);
		}

		public void Pack(byte[] buf, ref int offset)
		{
			buf[offset++] = AddressSize;
			BinaryUtil.Write(Address, buf, ref offset);
			BinaryUtil.Write(Port, buf, ref offset);
		}

		public IPEndPoint To()
		{
			if (AddressSize == 0) return null;
			return new IPEndPoint(IPAddress.Parse(Address), Port);
		}

		public override string ToString()
		{
			return Address + ":" + Port;
		}

		public static PeerEndPoint Unpack(byte[] buf, ref int offset)
		{
			var addressSize = buf[offset++];
			var address = BinaryUtil.ReadString(buf, addressSize, ref offset);
			var port = BinaryUtil.ReadInt(buf, ref offset);
			return new PeerEndPoint(address, port);
		}

	}
}