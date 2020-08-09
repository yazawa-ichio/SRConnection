using System;
using System.Collections.Generic;

namespace SRConnection.Channel
{
	public class UnreliableFlowControl : IDisposable
	{
		short m_ChannelId;
		int m_ConnectionId;
		IChannelContext m_Context;
		UnreliableChannelConfig m_Config;

		UnreliableFragmentQueue m_ReceiveQueue;
		short m_SendSequence;

		public UnreliableFlowControl(short channelId, int connectionId, IChannelContext ctx, UnreliableChannelConfig config = null)
		{
			m_ChannelId = channelId;
			m_ConnectionId = connectionId;
			m_Context = ctx;
			m_Config = config ?? new UnreliableChannelConfig();
			m_ReceiveQueue = new UnreliableFragmentQueue(m_Config);
		}

		public void Send(List<Fragment> input)
		{
			foreach (var fragment in input)
			{
				using (var data = new UnreliableData(m_ChannelId, ++m_SendSequence, fragment))
				{
					data.Pack(m_Context.SharedSendBuffer, out var size);
					m_Context.Send(m_ConnectionId, m_Context.SharedSendBuffer, 0, size, m_Config.Encrypt);
				}
			}
		}

		public bool TryDequeue(List<Fragment> output)
		{
			return m_ReceiveQueue.TryDequeue(output);
		}

		public void Enqueue(in UnreliableData packet)
		{
			m_ReceiveQueue.Enqueue(in packet);
		}

		public void Dispose()
		{
			m_ReceiveQueue.Dispose();
		}

	}
}