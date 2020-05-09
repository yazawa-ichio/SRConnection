using System.Net;

namespace SRNet
{

	internal class ClientConnectionImpl : ConnectionImpl
	{
		bool m_AllowP2P;

		public bool AllowP2P { set => m_AllowP2P = true; get => m_AllowP2P; }

		protected override bool UseP2P => m_AllowP2P;

		internal ClientConnectionImpl(int id, UdpSocket socket, IPEndPoint remoteEP, Encryptor encryptor, EncryptorGenerator encryptorGenerator) : base(socket, encryptorGenerator)
		{
			SelfId = id;
			m_PeerManager.Add(new PeerEntry(id, 0, encryptor, remoteEP));
		}


		protected override void Dispose(bool disposing)
		{
			BroadcastDisconnect();
			base.Dispose(disposing);
		}

		internal protected override void OnRemove(PeerEntry peer)
		{
			base.OnRemove(peer);
			if (peer.ConnectionId == SelfId)
			{
				Dispose();
			}
		}

		public void SendDisconnect()
		{
			Dispose();
		}

		public void Send(byte[] buf, bool encrypt = true) => Send(SelfId, buf, 0, buf.Length, encrypt);

		public void Send(byte[] buf, int offset, int size, bool encrypt = true) => Send(SelfId, buf, offset, size, encrypt);



	}

}
