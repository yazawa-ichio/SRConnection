namespace SRNet
{
	public class ClientConnection : Connection
	{
		public Peer Server { get; private set; }

		public bool CanP2P => P2P != null;

		internal ClientConnection(ClientConnectionImpl impl) : base(impl)
		{
			Server = GetPeer(SelfId);
		}

		public void SendDisconnect()
		{
			Server.Disconnect();
			Dispose();
		}

		public void SendPing()
		{
			Server.Ping();
		}

	}

}