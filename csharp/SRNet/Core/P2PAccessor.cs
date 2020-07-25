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

		internal P2PAccessor(ConnectionImpl impl)
		{
			m_Impl = impl;
		}

		public void Connect(PeerInfo[] list, bool init = true)
		{
			lock (m_Impl)
			{
				m_Impl.P2PTask.UpdateList(list, init);
			}
		}

		public void Connect(PeerInfo info)
		{
			lock (m_Impl)
			{
				m_Impl.P2PTask.Add(info);
			}
		}

		public Task WaitHandshake(CancellationToken token = default)
		{
			lock (m_Impl)
			{
				token.ThrowIfCancellationRequested();
				return m_Impl.P2PTask.WaitTaskComplete(token);
			}
		}

		public void Cancel(int connectionId)
		{
			lock (m_Impl)
			{
				m_Impl.P2PTask.Remove(connectionId);
			}
		}

		public void Cancel()
		{
			lock (m_Impl)
			{
				m_Impl.P2PTask.UpdateList(Array.Empty<PeerInfo>(), true);
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