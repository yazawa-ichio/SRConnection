using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace SRConnection
{
	public class ServerConnectSettings
	{
		public IPEndPoint EndPoint { get; set; }
		public bool UseP2P { get; set; }
		public RSA RSA { get; set; }
		public byte[] Cookie { get; set; }

		public static ServerConnectSettings Create(string host, int port)
		{
			var addresses = Dns.GetHostAddresses(host);
			if (addresses == null)
			{
				throw new Exception("not found address" + host);
			}
			var address = addresses.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
			if (address == null)
			{
				address = addresses[0];
			}
			return new ServerConnectSettings
			{
				EndPoint = new IPEndPoint(address, port)
			};
		}

		public static ServerConnectSettings FromXML(string xml, IPEndPoint endpoint)
		{
			var config = new ServerConnectSettings();
			config.EndPoint = endpoint;
			config.RSA = RSA.Create();
			config.RSA.FromXmlString(xml);
			return config;
		}

		public static ServerConnectSettings FromXML(string xml, string host, int port)
		{
			var config = Create(host, port);
			config.RSA = RSA.Create();
			config.RSA.FromXmlString(xml);
			return config;
		}


	}
}