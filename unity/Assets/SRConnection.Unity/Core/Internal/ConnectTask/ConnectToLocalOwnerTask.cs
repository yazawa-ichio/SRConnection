﻿using SRConnection.Packet;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SRConnection
{
	internal class ConnectToLocalOwnerTask
	{
		class HandshakeResult
		{
			public Encryptor Encryptor;
			public PeerToPeerList List;

			public HandshakeResult(Encryptor encryptor, PeerToPeerList list)
			{
				Encryptor = encryptor;
				List = list;
			}
		}

		UdpSocket m_Socket;
		IPEndPoint m_RemoteEP;
		IPEndPoint m_RemoteLocalEP;
		int m_SelfId;
		EncryptorGenerator m_EncryptorGenerator = new EncryptorGenerator();
		int m_HolePunchRequestPort;

		int m_OwnerId;
		byte[] m_Cookie;
		byte[] m_Randam;
		CancellationToken m_Token;

		public ConnectToLocalOwnerTask(IPEndPoint remoteEP, PeerToPeerRoomData data, int holePunchRequestPort, CancellationToken token)
		{
			m_RemoteEP = remoteEP;
			m_HolePunchRequestPort = holePunchRequestPort;
			m_OwnerId = data.ConnectionId;
			m_Cookie = data.Cookie;
			m_Randam = data.Randam;
			m_Token = token;
		}

		public async Task<P2PConnectionImpl> Run()
		{
			try
			{
				m_Socket = new UdpSocket();
				m_Socket.Bind(m_RemoteEP.AddressFamily);
				m_SelfId = Random.GenInt();
				if (m_Cookie == null)
				{
					var buf = new PeerToPeerHello(m_SelfId, null).Pack();
					var res = await new TimeoutRetryableRequester<PeerToPeerHello>(WaitHello(), () => Send(buf), m_Token).Run();
					m_OwnerId = res.ConnectionId;
					m_Cookie = res.Cookie;
				}
				{
					var buf = new PeerToPeerRequest(m_SelfId, m_Cookie).Pack();
					var res = await new TimeoutRetryableRequester<HandshakeResult>(WaitHandshakeAccept(), () => Send(buf), m_Token).Run();
					var peer = new PeerEntry(m_OwnerId, 0, res.Encryptor, m_RemoteEP);

					m_Token.ThrowIfCancellationRequested();
					var impl = new P2PConnectionImpl(m_SelfId, m_Socket, peer, m_EncryptorGenerator);
					impl.P2PTask.UpdateList(res.List.Peers, true);
					return impl;
				}
			}
			catch (Exception)
			{
				m_Socket.Dispose();
				throw;
			}
		}

		void Send(byte[] buf)
		{
			if (m_HolePunchRequestPort != 0)
			{
				var packet = new DiscoveryHolePunch().Pack();
				m_Socket.Broadcast(packet, 0, packet.Length, m_HolePunchRequestPort);
			}
			{
				m_Socket.Send(buf, 0, buf.Length, m_RemoteEP);
			}
			if (m_RemoteLocalEP != null)
			{
				m_Socket.Send(buf, 0, buf.Length, m_RemoteLocalEP);
			}
		}

		async Task<PeerToPeerHello> WaitHello()
		{
			while (true)
			{
				var receive = await m_Socket.ReceiveAsync();
				var buf = receive.Buffer;
				int size = buf.Length;
				if (DiscoveryHolePunch.TryUnpack(buf, size, out var _))
				{
					continue;
				}
				if (PeerToPeerHello.TryUnpack(buf, size, out var packet))
				{
					m_RemoteEP = receive.RemoteEndPoint;
					m_RemoteLocalEP = null;
					return packet;
				}
				throw new Exception("fail PeerToPeerHello");
			}
		}

		async Task<HandshakeResult> WaitHandshakeAccept()
		{
			while (true)
			{
				var receive = await m_Socket.ReceiveAsync();
				var buf = receive.Buffer;
				int size = buf.Length;
				if (size == 0 || buf[0] != (byte)PeerToPeerAccept.Type)
				{
					continue;
				}
				int offset = 5;
				int nonce = BinaryUtil.ReadInt(buf, ref offset);
				EncryptorKey key = default;
				key.Cookie = m_Cookie;
				key.MajorVersion = Protocol.MajorVersion;
				key.MinorVersion = Protocol.MinorVersion;
				key.Nonce = nonce;
				key.ConnectionId = m_SelfId;
				key.Random = new ArraySegment<byte>(m_Randam);

				var encryptor = m_EncryptorGenerator.Generate(in key);

				if (!PeerToPeerAccept.TryUnpack(buf, size, encryptor, out var packet))
				{
					throw new Exception("fail unpack HandshakeAccept");
				}
				m_RemoteEP = receive.RemoteEndPoint;
				return new HandshakeResult(encryptor, packet.GetPeerToPeerList());
			}
		}

	}
}