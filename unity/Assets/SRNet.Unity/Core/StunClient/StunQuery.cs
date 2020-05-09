using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SRNet.Stun
{
	public class StunQuery
	{
		readonly Socket m_Socket;
		readonly string m_Host;
		readonly int m_Port;
		StunMessage m_Request;
		TaskCompletionSource<StunMessage> m_Future;
		TimeSpan m_Timeout;
		IPEndPoint m_RemoteEP;

		public StunQuery(Socket socket, string host, int port) : this(socket, host, port, TimeSpan.FromMilliseconds(1600)) { }

		public StunQuery(Socket socket, string host, int port, TimeSpan timeout)
		{
			m_Socket = socket;
			m_Host = host;
			m_Port = port;
			m_Timeout = timeout;
		}

		async Task<IPEndPoint> GetLocalEndPoint()
		{
			try
			{
				string hostname = Dns.GetHostName();
				foreach (var address in (await Dns.GetHostEntryAsync(hostname)).AddressList)
				{
					if (m_RemoteEP.AddressFamily == address.AddressFamily)
					{
						return new IPEndPoint(address, (m_Socket.LocalEndPoint as IPEndPoint).Port);
					}
				}
			}
			catch { }
			return null;
		}

		public async Task<StunResult> Run()
		{
			if (m_Future != null) throw new Exception("already run");
			if (m_RemoteEP == null)
			{
				m_RemoteEP = await GetEndPoint();
			}

			var test1 = await DoTransaction(m_RemoteEP);
			if (test1 == null)
			{
				return new StunResult(NatType.Unspecified, null, await GetLocalEndPoint());
			}

			var test2 = await DoTransaction(true, true, m_RemoteEP);
			if (m_Socket.LocalEndPoint.Equals(test1.MappedAddress))
			{
				if (test2 != null)
				{
					return new StunResult(NatType.OpenInternet, test1.MappedAddress.EndPoint, await GetLocalEndPoint());
				}
				else
				{
					return new StunResult(NatType.SymmetricUDPFirewall, test1.MappedAddress.EndPoint, await GetLocalEndPoint());
				}
			}
			else if (test2 != null)
			{
				return new StunResult(NatType.FullCone, test1.MappedAddress.EndPoint, await GetLocalEndPoint());
			}

			var test12 = await DoTransaction(test1.ChangedAddress.EndPoint);
			if (test12 == null)
			{
				throw new Exception("STUN Test I(II) fail.");
			}
			else if (!test12.MappedAddress.Equals(test1.MappedAddress))
			{
				return new StunResult(NatType.Symmetric, test1.MappedAddress.EndPoint, await GetLocalEndPoint());
			}

			var test3 = await DoTransaction(false, true, test1.ChangedAddress.EndPoint);

			if (test3 != null)
			{
				return new StunResult(NatType.Restricted, test1.MappedAddress.EndPoint, await GetLocalEndPoint());
			}
			else
			{
				return new StunResult(NatType.PortRestricted, test1.MappedAddress.EndPoint, await GetLocalEndPoint());
			}

		}

		async Task<IPEndPoint> GetEndPoint()
		{
			var address = await Dns.GetHostAddressesAsync(m_Host);
			IPEndPoint remoteEP = null;
			for (int i = 0; i < address.Length; i++)
			{
				remoteEP = new IPEndPoint(address[i], m_Port);
			}
			return remoteEP;
		}

		Task<StunMessage> DoTransaction(IPEndPoint remoteEP)
		{
			return DoTransaction(new StunMessage(MessageType.BindingRequest), remoteEP);
		}

		Task<StunMessage> DoTransaction(bool changeIP, bool changePort, IPEndPoint remoteEP)
		{
			var msg = new StunMessage(MessageType.BindingRequest);
			msg.Attributes.Add(new ChangeRequestAttribute
			{
				ChangeIP = changeIP,
				ChangePort = changePort,
			});
			return DoTransaction(msg, remoteEP);
		}


		async Task<StunMessage> DoTransaction(StunMessage request, IPEndPoint remoteEP)
		{
			if (m_Future != null) throw new Exception("already run");
			m_Request = request;
			byte[] buf = new byte[20];
			var size = m_Request.Write(ref buf);
			m_Future = new TaskCompletionSource<StunMessage>();
			var timeoutAt = DateTime.UtcNow.Add(m_Timeout);
			while (timeoutAt > DateTime.UtcNow)
			{
				m_Socket.SendTo(buf, size, SocketFlags.None, remoteEP);
				await Task.WhenAny(m_Future.Task, Task.Delay(200));
				if (m_Future.Task.IsCompleted)
				{
					var ret = await m_Future.Task;
					m_Future = null;
					return ret;
				}
			}
			m_Future = null;
			return null;
		}

		public void Receive(byte[] buf)
		{
			StunMessage response = new StunMessage(MessageType.BindingResponse);
			if (response.TryParse(buf))
			{
				for (int i = 0; i < m_Request.TransactionId.Length; i++)
				{
					if (m_Request.TransactionId[i] != response.TransactionId[i])
					{
						return;
					}
				}
				m_Future?.SetResult(response);
			}
		}

	}

}
