namespace SRNet
{
	public class ClientConnection : Connection
	{
		ClientConnectionImpl m_ClientImpl;

		public Peer Server { get; private set; }

		public bool AllowP2P => m_ClientImpl.AllowP2P;

		internal ClientConnection(ClientConnectionImpl impl) : base(impl)
		{
			m_ClientImpl = impl;
			Server = GetPeer(SelfId);
		}

		public void SendDisconnect()
		{
			Server.SendDisconnect();
			Dispose();
		}

		public void SendPing()
		{
			Server.SendPing();
		}

	}

}