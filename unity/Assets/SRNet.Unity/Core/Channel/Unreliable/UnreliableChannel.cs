using System;
using System.Collections.Generic;

namespace SRNet.Channel
{
	public class UnreliableChannel : IChannel
	{
		short m_ChannelId;
		IChannelContext m_Context;
		Dictionary<int, UnreliableFlowControl> m_FlowControls = new Dictionary<int, UnreliableFlowControl>();
		UnreliableChannelConfig m_Config;

		public UnreliableChannel(UnreliableChannelConfig config)
		{
			m_Config = config;
		}

		public void Init(short channelId, IChannelContext ctx)
		{
			m_ChannelId = channelId;
			m_Context = ctx;
		}

		UnreliableFlowControl GetOrAdd(int id)
		{
			if (!m_FlowControls.TryGetValue(id, out var value))
			{
				m_FlowControls[id] = value = new UnreliableFlowControl(m_ChannelId, id, m_Context, m_Config);
			}
			return value;
		}

		public void Send(int id, List<Fragment> input)
		{
			GetOrAdd(id).Send(input);
		}

		public bool TryRead(int id, List<Fragment> output)
		{
			if (m_FlowControls.TryGetValue(id, out var value))
			{
				return value.TryDequeue(output);
			}
			return false;
		}

		public void OnReceive(int id, byte[] buf, int offset, int size)
		{
			if (UnreliableData.TryUnpack(buf, ref offset, out var data))
			{
				GetOrAdd(id).Enqueue(data);
			}
		}

		public void RemovePeer(int id)
		{
			if (m_FlowControls.TryGetValue(id, out var value))
			{
				m_FlowControls.Remove(id);
				value.Dispose();
			}
		}

		public void Update(in TimeSpan delta) { }

		public void Dispose()
		{
			foreach (var val in m_FlowControls.Values)
			{
				val.Dispose();
			}
			m_FlowControls = null;
		}

	}
}