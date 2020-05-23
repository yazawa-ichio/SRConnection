using System.Net;

namespace SRNet
{
	public class LocalHostConfig
	{
		public string RoomName;
		public IPAddress Address = IPAddress.Any;
		public System.Func<string, bool> DiscoveryQueryMatch = (x) => true;
		public int DiscoveryServicePort = DiscoveryService.Port;
	}

}