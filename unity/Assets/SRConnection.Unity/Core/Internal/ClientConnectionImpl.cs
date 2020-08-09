namespace SRConnection
{

	internal class ClientConnectionImpl : ConnectionImpl
	{
		bool m_UseP2P;
		public override bool UseP2P => m_UseP2P;

		internal ClientConnectionImpl(int id, UdpSocket socket, ServerConnectSettings settings, Encryptor encryptor, EncryptorGenerator encryptorGenerator) : base(socket, encryptorGenerator)
		{
			SelfId = id;
			m_UseP2P = settings.UseP2P;
			m_PeerManager.Add(new PeerEntry(id, 0, encryptor, settings.EndPoint));
		}

		protected override void Dispose(bool disposing)
		{
			BroadcastDisconnect();
			base.Dispose(disposing);
		}

		internal protected override void OnRemove(PeerEntry peer)
		{
			base.OnRemove(peer);
			if (DisposeOnDisconnectOwner && peer.ConnectionId == SelfId)
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