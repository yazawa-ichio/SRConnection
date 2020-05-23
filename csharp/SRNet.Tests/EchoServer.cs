using System;
using System.Net;
using System.Security.Cryptography;
using System.Threading;

namespace SRNet.Tests
{
	public class EchoServer : IDisposable
	{
		public static readonly IPEndPoint DefaultEndPoint = new IPEndPoint(IPAddress.Any, 8701);

		Connection m_Connection;
		Thread m_Thread;
		bool m_Disposed;

		public IPEndPoint LocalEP { get; private set; }

		public RSA RSA { get; private set; }

		public Connection Conn => m_Connection;

		public EchoServer() : this(DefaultEndPoint, RSA.Create()) { }

		public EchoServer(IPEndPoint localEP, RSA rsa)
		{
			LocalEP = localEP;
			RSA = rsa;
			ServerConfig config = new ServerConfig();
			config.LocalEP = localEP;
			config.RSA = rsa;
			m_Connection = Connection.StartServer(config);
			Start();
		}

		public EchoServer(Connection connection)
		{
			m_Connection = connection;
			Start();
		}

		public ServerConnectSettings GetConnectSettings()
		{
			bool ipv4 = LocalEP.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
			IPEndPoint ep;
			if (ipv4)
			{
				ep = new IPEndPoint(IPAddress.Loopback, LocalEP.Port);
			}
			else
			{
				ep = new IPEndPoint(IPAddress.IPv6Loopback, LocalEP.Port);
			}
			return new ServerConnectSettings()
			{
				EndPoint = ep,
				RSA = RSA.Create(RSA.ExportParameters(false))
			};
		}

		void Start()
		{
			if (m_Thread != null) throw new InvalidOperationException("already started");
			m_Thread = new Thread(ReceiveLoop);
			m_Thread.Start();
		}

		void ReceiveLoop(object state)
		{
			while (!m_Disposed)
			{
				try
				{
					if (m_Connection.TryPollReceive(out var message, TimeSpan.FromSeconds(1)))
					{
						Console.WriteLine(System.Text.Encoding.UTF8.GetString(message));
						message.Peer.Send(message.Channel, message);
					}
				}
				catch (Exception ex)
				{
					if (m_Disposed) return;
					Console.WriteLine(ex);
					throw;
				}
			}
		}

		public void Dispose()
		{
			if (m_Disposed) return;
			m_Disposed = true;
			m_Connection?.Dispose();
			if (m_Thread != null)
			{
				m_Thread.Join();
				m_Thread = null;
			}
		}
	}
}