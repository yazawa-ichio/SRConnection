using SRNet.Packet;
using System;
using System.Net;

namespace SRNet
{
	internal class ConnectToPeerTask : IDisposable
	{
		public readonly int ConnectionId;
		PeerInfo m_Info;
		ConnectionImpl m_Connection;
		PeerToPeerRequest m_Request;
		byte[] m_HandshakePacket;
		IPEndPoint m_RemoteEP;
		IPEndPoint m_LocalEP;
		TimeSpan m_Interval;
		bool m_RequestFlag;
		bool m_Disposed;

		public ConnectToPeerTask(ConnectionImpl connection, PeerInfo info, bool requestFlag)
		{
			m_Connection = connection;
			ConnectionId = info.ConnectionId;
			m_RequestFlag = requestFlag;
			m_Info = info;
			m_RemoteEP = info.EndPont.To();
			m_LocalEP = info.LocalEndPont.To();
			SendHello();
		}

		public void UpdateInfo(PeerInfo info)
		{
			m_Info = info;
			m_RemoteEP = info.EndPont.To();
			Send();
		}

		void Send()
		{
			m_Interval = TimeSpan.FromMilliseconds(500);
			if (!m_RequestFlag)
			{
				SendHolePunch();
			}
			else
			{
				if (m_HandshakePacket == null)
				{
					SendHello();
				}
				else
				{
					SendHandshake();
				}
			}
		}

		void SendHolePunch()
		{
			lock (m_Connection.m_Socket)
			{
				m_Connection.m_Socket.Send(Array.Empty<byte>(), 0, 0, m_RemoteEP);
				if (m_LocalEP != null)
				{
					m_Connection.m_Socket.Send(Array.Empty<byte>(), 0, 0, m_LocalEP);
				}
			}
		}

		void SendHello()
		{
			lock (m_Connection.m_Socket)
			{
				var size = new PeerToPeerHello(m_Connection.SelfId, null).Pack(m_Connection.m_SendBuffer);
				m_Connection.m_Socket.Send(m_Connection.m_SendBuffer, 0, size, m_RemoteEP);
				if (m_LocalEP != null)
				{
					m_Connection.m_Socket.Send(m_Connection.m_SendBuffer, 0, size, m_LocalEP);
				}
			}
		}

		void SendHandshake()
		{
			lock (m_Connection.m_Socket)
			{
				m_Connection.m_Socket.Send(m_HandshakePacket, 0, m_HandshakePacket.Length, m_RemoteEP);
				if (m_LocalEP != null)
				{
					m_Connection.m_Socket.Send(m_HandshakePacket, 0, m_HandshakePacket.Length, m_LocalEP);
				}
			}
		}

		public void OnPeerToPeerHello(PeerToPeerHello packet, IPEndPoint remoteEP)
		{
			lock (m_Connection.m_Socket)
			{
				if (m_LocalEP != null && remoteEP.Address.Equals(m_LocalEP.Address) && remoteEP.Port == m_LocalEP.Port)
				{
					m_LocalEP = null;
				}
			}
			m_RemoteEP = remoteEP;
			m_Request = new PeerToPeerRequest(m_Connection.SelfId, packet.Cookie);
			m_HandshakePacket = m_Request.Pack();
			Send();
		}

		public bool OnHandshakeAccept(byte[] buf, int size, out PeerEntry peer, IPEndPoint remoteEP)
		{
			if (size < 9)
			{
				peer = null;
				return false;
			}
			int offset = 5;
			int nonce = BinaryUtil.ReadInt(buf, ref offset);
			var key = new EncryptorKey(m_Request, m_Info.Randam, nonce);
			var encryptor = m_Connection.m_EncryptorGenerator.Generate(key);

			if (!PeerToPeerAccept.TryUnpack(buf, size, encryptor, out var packet))
			{
				peer = null;
				return false;
			}

			peer = new PeerEntry(packet.ConnectionId, nonce, encryptor, remoteEP);
			return true;
		}

		public void Update(TimeSpan delta)
		{
			if (m_Disposed) return;
			m_Interval -= delta;
			if (m_Interval < TimeSpan.Zero)
			{
				Send();
			}
		}

		public void Dispose()
		{
			m_Disposed = true;
		}

	}

}