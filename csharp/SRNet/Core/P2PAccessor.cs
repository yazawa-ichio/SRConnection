using SRNet.Channel;
using SRNet.Packet;
using SRNet.Stun;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SRNet
{
	public class P2PAccessor
	{
		ConnectionImpl m_Impl;
		ChannelContext m_Channel;

		internal P2PAccessor(ConnectionImpl impl, ChannelContext channel)
		{
			m_Impl = impl;
			m_Channel = channel;
		}

		public void Connect(PeerInfo[] list, bool init = true)
		{
			lock (m_Impl)
			{
				m_Impl.UpdateConnectPeerList(list, init);
			}
		}

		public void Connect(PeerInfo info)
		{
			lock (m_Impl)
			{
				m_Impl.AddConnectPeer(info);
			}
		}

		public Task WaitHandshake(CancellationToken token = default)
		{
			lock (m_Impl)
			{
				token.ThrowIfCancellationRequested();
				return m_Impl.WaitHandshake(token);
			}
		}

		public void Cancel(int connectionId)
		{
			lock (m_Impl)
			{
				m_Impl.CancelP2PHandshake(connectionId);
			}
		}

		public void Cancel()
		{
			lock (m_Impl)
			{
				m_Impl.UpdateConnectPeerList(Array.Empty<PeerInfo>(), true);
			}
		}

		public Task<StunResult> StunQuery(string host, int port, TimeSpan timeout)
		{
			lock (m_Impl)
			{
				return m_Impl.StunQuery(host, port, timeout);
			}
		}

	}

}