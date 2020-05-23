using SRNet.Packet;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SRNet
{
	public class DiscoveryService : IDisposable
	{
		public const int Port = 54731;

		UdpSocket m_Socket = new UdpSocket();
		bool m_Bind;
		IPEndPoint m_LocalEP;
		bool m_Run;
		string m_Name;
		int m_ServicePort;
		byte[] m_Response;

		public event Action<IPEndPoint> OnHolePunchRequest;

		public DiscoveryService(string name, IPEndPoint localEP, byte[] data, int servicePort = Port)
		{
			m_Name = name;
			m_ServicePort = servicePort;
			var nameBuf = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(name));
			m_LocalEP = localEP;
			m_Response = new DiscoveryResponse(localEP.Port, nameBuf, new ArraySegment<byte>(data)).Pack();
		}

		~DiscoveryService()
		{
			Dispose();
		}

		public void Start()
		{
			Start((x) => true);
		}

		public void Start(bool nameMatch)
		{
			Start((x) => !nameMatch || m_Name == x);
		}

		public void Start(Func<string, bool> match)
		{
			if (m_Run) throw new InvalidOperationException("すでに実行済みです");
			m_Run = true;
			if (!m_Bind)
			{
				m_Bind = true;
				m_Socket.Bind(new IPEndPoint(m_LocalEP.Address, m_ServicePort), true);
			}
			HolePunch();
			Receive(match);
		}

		async void HolePunch()
		{
			//送信したポートにしか受け取れない場合があるので、とりあえず送ってポートを開ける
			while (m_Run)
			{
				try
				{
					lock (m_Socket)
					{
						m_Socket.Broadcast(Array.Empty<byte>(), 0, 0, m_ServicePort);
					}
					await Task.Delay(500);
				}
				catch { }
			}
		}

		async void Receive(Func<string, bool> match)
		{
			while (m_Run)
			{
				try
				{
					var res = await m_Socket.ReceiveAsync();
					if (Discovery.TryUnpack(res.Buffer, res.Buffer.Length, out var packet))
					{
						var query = System.Text.Encoding.UTF8.GetString(packet.Query.Array, packet.Query.Offset, packet.Query.Count);
						if (match(query))
						{
							lock (m_Socket)
							{
								m_Socket.Send(m_Response, 0, m_Response.Length, res.RemoteEndPoint);
							}
						}
					}
					else if (DiscoveryHolePunch.TryUnpack(res.Buffer, res.Buffer.Length, out _))
					{
						OnHolePunchRequest?.Invoke(res.RemoteEndPoint);
					}
				}
				catch { }
			}
		}

		public void Dispose()
		{
			m_Run = false;
			m_Socket?.Dispose();
			GC.SuppressFinalize(this);
		}
	}

}