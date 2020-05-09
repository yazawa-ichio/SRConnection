namespace SRNet
{
	public class P2PHostConnection : Connection
	{
		public DiscoveryService DiscoveryService { get; private set; }

		internal P2PHostConnection(P2PConnectionImpl impl) : base(impl)
		{
			DiscoveryService = impl.DiscoveryService;
		}
	}

}