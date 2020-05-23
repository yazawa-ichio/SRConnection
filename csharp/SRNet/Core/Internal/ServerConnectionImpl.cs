using SRNet.Packet;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;

namespace SRNet
{

	internal class ServerConnectionImpl : ConnectionImpl
	{
		static readonly int ClientIdCapacity = 128;

		protected override bool IsHost => true;

		byte[] m_ServerHello;
		DateTime m_CookieCreateTime;
		Queue<int> m_ClientIdBuffer = new Queue<int>(ClientIdCapacity);
		Dictionary<int, int> m_ClientIdPeerMap = new Dictionary<int, int>();
		RSA m_RSA;
		IdGenerator m_IdGenerator = new IdGenerator();

		public ServerConnectionImpl(ServerConfig config) : base(new UdpSocket(), new EncryptorGenerator())
		{
			m_Socket.Bind(config.LocalEP, true);
			m_ServerHello = m_CookieProvider.CreatePacket();
			m_CookieCreateTime = DateTime.UtcNow;
			m_RSA = config.RSA;
		}

		protected override void Dispose(bool disposing)
		{
			m_RSA?.Dispose();
			m_RSA = null;
			m_IdGenerator.Dispose();
			m_IdGenerator = null;
			base.Dispose(disposing);
		}

		protected override void TimerUpdate(TimeSpan delta)
		{
			PeerUpdate(delta);
			if (DateTime.UtcNow - m_CookieCreateTime > TimeSpan.FromMinutes(1))
			{
				Log.Debug("update cookie ");
				m_ServerHello = m_CookieProvider.CreatePacket();
				m_CookieCreateTime = DateTime.UtcNow;
			}
		}

		protected override int GetSendId(int peerId)
		{
			return peerId;
		}

		protected internal override void OnRemove(PeerEntry peer)
		{
			m_IdGenerator?.Remove(peer.ConnectionId);
			base.OnRemove(peer);
		}

		protected override void OnClientHello(byte[] buf, int size, IPEndPoint remoteEP)
		{
			if (ClientHello.TryUnpack(buf, 0, size, out _))
			{
				lock (m_Socket)
				{
					m_Socket.Send(m_ServerHello, 0, m_ServerHello.Length, remoteEP);
				}
			}
		}

		protected override void OnHandshakeRequest(byte[] buf, int size, IPEndPoint remoteEP)
		{
			if (HandshakeRequest.TryUnpack(m_CookieProvider, m_RSA, buf, size, out var packet))
			{
				int offest = 0;
				int clientId = BinaryUtil.ReadInt(packet.Payload, ref offest);
				PeerEntry peer = null;
				if (m_ClientIdPeerMap.TryGetValue(clientId, out int connectionId))
				{
					m_PeerManager.TryGetValue(connectionId, out peer);
				}
				else if (HandshakeRequestPayload.TryUnpack(packet.Payload, out var payload))
				{
					if (m_ClientIdBuffer.Count == ClientIdCapacity)
					{
						m_ClientIdPeerMap.Remove(m_ClientIdBuffer.Dequeue());
					}
					m_ClientIdBuffer.Enqueue(clientId);
					var id = m_IdGenerator.Gen();
					m_ClientIdPeerMap[clientId] = id;

					var key = new EncryptorKey(packet, payload, id);
					var encryptor = m_EncryptorGenerator.Generate(in key);
					peer = new PeerEntry(id, clientId, encryptor, remoteEP);

					m_PeerManager.Add(peer);
				}
				if (peer != null)
				{
					lock (m_Socket)
					{
						size = new HandshakeAccept(peer.ConnectionId).Pack(m_SendBuffer, peer.Encryptor);
						m_Socket.Send(m_SendBuffer, 0, size, peer.EndPoint);
					}
				}
			}
		}

	}

}