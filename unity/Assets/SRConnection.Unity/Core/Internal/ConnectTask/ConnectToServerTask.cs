using System;
using System.Threading.Tasks;

namespace SRConnection
{
	using Packet;
	using System.Security.Cryptography;
	using System.Threading;

	internal class ConnectToServerTask
	{

		class HandshakeResult
		{
			public int ConnectionId;
			public Encryptor Encryptor;

			public HandshakeResult(int connectionId, Encryptor encryptor)
			{
				ConnectionId = connectionId;
				Encryptor = encryptor;
			}
		}

		UdpSocket m_Socket;
		ServerConnectSettings m_Settings;
		EncryptorGenerator m_EncryptorGenerator = new EncryptorGenerator();
		byte[] m_Randam;
		EncryptorKey m_EncryptorKey;
		CancellationToken m_Token;

		public ConnectToServerTask(ServerConnectSettings settings, CancellationToken token)
		{
			m_Settings = settings;
			m_Randam = Random.GenBytes(EncryptorKey.RandamKey);
			m_Token = token;
		}

		public async Task<ClientConnectionImpl> Run()
		{
			try
			{
				m_Socket = new UdpSocket();
				m_Socket.Bind(m_Settings.EndPoint.AddressFamily);
				if (!m_Settings.UseP2P)
				{
					m_Socket.Connect(m_Settings.EndPoint);
				}
				byte[] cookie;
				if (m_Settings.Cookie == null)
				{
					var buf = new ClientHello(Protocol.MajorVersion, Protocol.MinorVersion).Pack();
					var req = new TimeoutRetryableRequester<ServerHello>(WaitServerHello(), () => Send(buf), m_Token);
					var res = await req.Run();
					cookie = res.Cookie;
				}
				else
				{
					cookie = m_Settings.Cookie;
				}
				{
					m_Token.ThrowIfCancellationRequested();

					var payload = new HandshakeRequestPayload(Random.GenInt(), m_Randam);
					byte[] payloadBuf = payload.Pack();
					if (m_Settings.RSA != null)
					{
						payloadBuf = m_Settings.RSA.Encrypt(payloadBuf, RSAEncryptionPadding.Pkcs1);
					}
					var request = new HandshakeRequest(cookie, payloadBuf);

					m_EncryptorKey = new EncryptorKey(request, payload, 0);

					var res = await (new TimeoutRetryableRequester<HandshakeResult>(WaitHandshakeAccept(), () => Send(request.Pack()), m_Token)).Run();

					m_Token.ThrowIfCancellationRequested();

					return new ClientConnectionImpl(res.ConnectionId, m_Socket, m_Settings, res.Encryptor, m_EncryptorGenerator);

				}
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				m_Socket.Dispose();
				m_EncryptorGenerator.Dispose();
				throw;
			}
		}

		void Send(byte[] buf)
		{
			m_Socket.Send(buf, 0, buf.Length, m_Settings.EndPoint);
		}

		async Task<ServerHello> WaitServerHello()
		{
			var receive = m_Socket.ReceiveAsync();
			var res = await receive;
			if (!ServerHello.TryUnpack(res.Buffer, res.Buffer.Length, out var packet))
			{
				throw new Exception("fail unpack ServerHello");
			}
			return packet;
		}

		async Task<HandshakeResult> WaitHandshakeAccept()
		{
			var receive = await m_Socket.ReceiveAsync();
			var buf = receive.Buffer;
			int size = buf.Length;

			int offset = 1;
			m_EncryptorKey.ConnectionId = BinaryUtil.ReadInt(buf, ref offset);
			var encryptor = m_EncryptorGenerator.Generate(in m_EncryptorKey);

			if (!HandshakeAccept.TryUnpack(buf, size, encryptor, out var packet))
			{
				throw new Exception("fail unpack HandshakeAccept");
			}
			return new HandshakeResult(packet.ConnectionId, encryptor);
		}

	}

}