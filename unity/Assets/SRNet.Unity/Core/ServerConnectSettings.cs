using System.Net;
using System.Security.Cryptography;

namespace SRNet
{
	public class ServerConnectSettings
	{
		public IPEndPoint EndPoint { get; set; }
		public RSA RSA { get; set; }
		public byte[] Cookie { get; internal set; }

		public static ServerConnectSettings FromXML(string xml, IPEndPoint endpoint)
		{
			var config = new ServerConnectSettings();
			config.EndPoint = endpoint;
			config.RSA = RSA.Create();
			config.RSA.FromXmlString(xml);
			return config;
		}

	}
}