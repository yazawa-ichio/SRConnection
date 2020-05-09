using System.Net;

namespace SRNet.Stun
{
	public class StunResult
	{
		public NatType NatType { get; private set; }

		public IPEndPoint EndPoint { get; private set; }

		public IPEndPoint LocalEndPoint { get; private set; }

		public StunResult(NatType natType, IPEndPoint endPoint, IPEndPoint localEndPoint)
		{
			NatType = natType;
			EndPoint = endPoint;
			LocalEndPoint = localEndPoint;
		}

	}
}
