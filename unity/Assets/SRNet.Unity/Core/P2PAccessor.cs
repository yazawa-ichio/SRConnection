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
			m_Impl.UpdateConnectPeerList(list, init);
		}

		public void Connect(PeerInfo info)
		{
			m_Impl.AddConnectPeer(info);
		}

		public async Task WaitHandshake(CancellationToken token = default)
		{
			var task = m_Impl.WaitHandshake();
			while (!task.IsCompleted)
			{
				m_Channel.PreReadMessage();
				await Task.WhenAny(task, Task.Delay(200, token));
			}
		}

		public void Cancel(int connectionId)
		{
			m_Impl.CancelP2PHandshake(connectionId);
		}

		public void Cancel()
		{
			m_Impl.UpdateConnectPeerList(Array.Empty<PeerInfo>(), true);
		}

		public Task<StunResult> StunQuery(string host, int port, TimeSpan timeout)
		{
			return m_Impl.StunQuery(host, port, timeout);
		}

	}

}