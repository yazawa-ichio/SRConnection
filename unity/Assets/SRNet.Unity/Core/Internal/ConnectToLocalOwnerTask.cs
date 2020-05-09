using SRNet.Packet;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SRNet
{
	internal class ConnectToLocalOwnerTask
	{
		class HandshakeResult
		{
			public Encryptor Encryptor;

			public HandshakeResult(Encryptor encryptor)
			{
				Encryptor = encryptor;
			}
		}

		UdpSocket m_Socket;
		IPEndPoint m_RemoteEP;
		PeerToPeerRoomData m_Data;
		int m_ConnectionId;
		EncryptorGenerator m_EncryptorGenerator = new EncryptorGenerator();
		int m_HolePunchRequestPort;

		public ConnectToLocalOwnerTask(IPEndPoint remoteEP, PeerToPeerRoomData data, int holePunchRequestPort)
		{
			m_RemoteEP = remoteEP;
			m_Data = data;
			m_HolePunchRequestPort = holePunchRequestPort;
		}

		public async Task<P2PConnectionImpl> Run()
		{
			try
			{
				m_Socket = new UdpSocket();
				m_Socket.Bind();
				m_ConnectionId = Random.GenInt();
				var buf = new PeerToPeerRequest(m_ConnectionId, m_Data.Cookie).Pack();
				var res = await new TimeoutRetryableRequester<HandshakeResult>(WaitHandshakeAccept(), () => Send(buf)).Run();
				var peer = new PeerEntry(m_Data.ConnectionId, 0, res.Encryptor, m_RemoteEP);
				return new P2PConnectionImpl(m_ConnectionId, m_Socket, peer, m_EncryptorGenerator);
			}
			catch (Exception)
			{
				m_Socket.Dispose();
				throw;
			}
		}

		void Send(byte[] buf)
		{
			{
				var packet = new DiscoveryHolePunch().Pack();
				m_Socket.Broadcast(packet, 0, packet.Length, m_HolePunchRequestPort);
			}
			{
				m_Socket.Send(buf, 0, buf.Length, m_RemoteEP);
			}
		}

		async Task<HandshakeResult> WaitHandshakeAccept()
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
				int offset = 5;
				int nonce = BinaryUtil.ReadInt(buf, ref offset);
				EncryptorKey key = default;
				key.Cookie = m_Data.Cookie;
				key.MajorVersion = Protocol.MajorVersion;
				key.MinorVersion = Protocol.MinorVersion;
				key.Nonce = nonce;
				key.ConnectionId = m_ConnectionId;
				key.Random = new ArraySegment<byte>(m_Data.Randam);

				var encryptor = m_EncryptorGenerator.Generate(in key);

				if (!PeerToPeerAccept.TryUnpack(buf, size, encryptor, out var _))
				{
					throw new Exception("fail unpack HandshakeAccept");
				}

				return new HandshakeResult(encryptor);
			}
		}

	}
}