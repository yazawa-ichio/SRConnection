using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SRNet
{
	using Stun;

	internal class UdpSocket : IDisposable
	{
		const int SioUdpConnreset = -1744830452;
		Socket m_Socket;
		internal UdpClient m_UdpClient;
		EndPoint m_RemoteEP;
		StunQuery m_StunQuery;

		public IPEndPoint LocalEP => m_Socket.LocalEndPoint as IPEndPoint;

		public StunResult StunResult => m_StunQuery?.Result ?? null;

		~UdpSocket()
		{
			Dispose();
		}


		public void Bind()
		{
			Bind(AddressFamily.InterNetwork);
		}

		public void Bind(AddressFamily addressFamily)
		{
			if (addressFamily == AddressFamily.InterNetworkV6)
			{
				Bind(new IPEndPoint(IPAddress.IPv6Any, 0), false);
			}
			else
			{
				Bind(new IPEndPoint(IPAddress.Any, 0), false);
			}
		}

		public void Bind(IPEndPoint localEP, bool reuse)
		{
			m_UdpClient = new UdpClient(localEP);
			m_Socket = m_UdpClient.Client;
			m_Socket.EnableBroadcast = true;
			try
			{
				m_Socket.IOControl(SioUdpConnreset, new byte[] { 0 }, null);
			}
			catch { }
			m_Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, reuse);
			m_Socket.Blocking = false;
			m_RemoteEP = new IPEndPoint(m_Socket.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0);
		}

		public bool TryReceiveFrom(byte[] buffer, ref int size, ref IPEndPoint remoteEP)
		{
			while (true)
			{
				if (m_Socket.Available == 0) return false;
				size = m_Socket.ReceiveFrom(buffer, ref m_RemoteEP);
				remoteEP = m_RemoteEP as IPEndPoint;
				if (CheckStunQuery(buffer, size))
				{
					continue;
				}
				return true;
			}
		}

		bool CheckStunQuery(byte[] buffer, int size)
		{
			if (m_StunQuery == null || m_StunQuery.IsCompleted) return false;

			if (size >= 20 && m_StunQuery.TryReceive(buffer))
			{
				return true;
			}
			return false;
		}

		public bool Poll(int microSeconds, SelectMode mode)
		{
			if (m_Socket.Available > 0) return true;
			return m_Socket.Poll(microSeconds, mode);
		}

		public Task<UdpReceiveResult> ReceiveAsync()
		{
			return m_UdpClient.ReceiveAsync();
		}

		public Task<int> SendAsync(byte[] buf, int size, IPEndPoint endPoint)
		{
			return m_UdpClient.SendAsync(buf, size, endPoint);
		}

		public void Send(byte[] buf, int offest, int size, EndPoint remoteEP)
		{
			lock (m_Socket)
			{
				m_Socket.SendTo(buf, offest, size, SocketFlags.None, remoteEP);
			}
		}

		public bool Broadcast(byte[] buf, int offest, int size, int port)
		{
			lock (m_Socket)
			{
				return m_Socket.SendTo(buf, offest, size, SocketFlags.None, new IPEndPoint(IPAddress.Broadcast, port)) > 0;
			}
		}


		public Task<StunResult> StunQuery(string host, int port, TimeSpan timeout)
		{
			if (m_StunQuery != null && !m_StunQuery.IsCompleted)
			{
				return m_StunQuery.Run();
			}
			m_StunQuery = new StunQuery(m_Socket, host, port, timeout);
			return m_StunQuery.Run();
		}

		public void Dispose()
		{
			m_Socket?.Dispose();
			GC.SuppressFinalize(this);
		}

	}
}