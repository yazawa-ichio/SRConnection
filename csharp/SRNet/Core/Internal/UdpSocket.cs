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
		UdpClient m_UdpClient;
		EndPoint m_RemoteEP;

		public IPEndPoint LocalEP => m_Socket.LocalEndPoint as IPEndPoint;

		~UdpSocket()
		{
			Dispose();
		}

		public void Bind()
		{
			Bind(new IPEndPoint(IPAddress.Any, 0), false);
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
			if (m_Socket.Available == 0) return false;
			size = m_Socket.ReceiveFrom(buffer, ref m_RemoteEP);
			remoteEP = m_RemoteEP as IPEndPoint;
			return true;
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
			m_Socket.SendTo(buf, offest, size, SocketFlags.None, remoteEP);
		}

		public bool Broadcast(byte[] buf, int offest, int size, int port)
		{
			return m_Socket.SendTo(buf, offest, size, SocketFlags.None, new IPEndPoint(IPAddress.Broadcast, port)) > 0;
		}

		public Task<StunResult> StunQuery(string host, int port)
		{
			return StunClient.Run(m_UdpClient, host, port);
		}

		public Task<StunResult> StunQuery(string host, int port, TimeSpan timeout)
		{
			return new StunQuery(m_Socket, host, port, timeout).Run();
		}

		public void Dispose()
		{
			m_Socket?.Dispose();
			GC.SuppressFinalize(this);
		}

	}
}