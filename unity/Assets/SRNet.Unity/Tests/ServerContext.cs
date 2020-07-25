using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SRNet.Unity.Tests
{
	public class ServerContext<T> : IDisposable where T : ServerBase
	{
		public static readonly int DefaultPort = 8701;

		public T Server { get; private set; }

		GameObject m_Owner;
		ServerConfig m_Config;

		public ServerContext(bool ipv4, bool rsa)
		{
			m_Owner = new GameObject($"Server:{typeof(T).Name}");
			Server = m_Owner.AddComponent<T>();
			m_Config = new ServerConfig();
			m_Config.LocalEP = new IPEndPoint(ipv4 ? IPAddress.Any : IPAddress.IPv6Any, DefaultPort);
			if (rsa)
			{
				m_Config.RSA = RSA.Create();
			}
			Server.Setup(m_Config);
		}

		public void Dispose()
		{
			Server.Disconnect();
			GameObject.Destroy(m_Owner);
		}


		public void Broadcast(string message, bool reliable = true)
		{
			var buf = Encoding.UTF8.GetBytes(message);
			Server.Broadcast(buf, reliable);
		}

		public ServerConnectSettings GetConnectSettings()
		{
			bool ipv4 = m_Config.LocalEP.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
			IPEndPoint ep;
			if (ipv4)
			{
				ep = new IPEndPoint(IPAddress.Loopback, m_Config.LocalEP.Port);
			}
			else
			{
				ep = new IPEndPoint(IPAddress.IPv6Loopback, m_Config.LocalEP.Port);
			}
			RSA rsa = null;
			if (m_Config.RSA != null)
			{
				rsa = RSA.Create();
				rsa.ImportParameters(m_Config.RSA.ExportParameters(false));
			}
			return new ServerConnectSettings()
			{
				EndPoint = ep,
				RSA = rsa
			};

		}

	}
}