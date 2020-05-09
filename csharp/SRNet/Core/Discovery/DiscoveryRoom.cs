using System.Net;

namespace SRNet
{
	public class DiscoveryRoom
	{
		public readonly IPAddress Address;
		public readonly int Port;
		public readonly string Name;
		public readonly byte[] Data;
		public readonly int DiscoveryPort;

		public DiscoveryRoom(IPAddress address, int port, string name, byte[] data, int discoveryPort)
		{
			Address = address;
			Port = port;
			Name = name;
			Data = data;
			DiscoveryPort = discoveryPort;
		}
	}

}
