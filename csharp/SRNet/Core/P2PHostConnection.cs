namespace SRNet
{

	public class P2PHostConnection : Connection
	{

		P2PConnectionImpl m_Impl;
		internal P2PHostConnection(P2PConnectionImpl impl) : base(impl)
		{
			m_Impl = impl;
		}

		public void StopMatching()
		{
			m_Impl.StopMatching();
		}

	}

}