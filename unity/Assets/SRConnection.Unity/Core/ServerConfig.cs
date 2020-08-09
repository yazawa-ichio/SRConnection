using System.Net;
using System.Security.Cryptography;

namespace SRConnection
{
	public class ServerConfig
	{
		public IPEndPoint LocalEP { get; set; }

		public RSA RSA { get; set; }

		public bool ConnectionIdAbsOnly { get; set; }

		public static ServerConfig Create(int port)
		{
			return new ServerConfig
			{
				LocalEP = new IPEndPoint(IPAddress.Any, port)
			};
		}

		public static ServerConfig FromXML(string xml, int port)
		{
			var config = new ServerConfig();
			config.LocalEP = new IPEndPoint(IPAddress.Any, port);
			config.RSA = RSA.Create();
			config.RSA.FromXmlString(xml);
			return config;
		}

	}
}