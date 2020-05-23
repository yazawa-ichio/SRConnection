using System;
using System.Collections.Generic;

namespace SRNet.Channel
{

	public class ReliableChannel : IChannel
	{
		short m_ChannelId;
		IChannelContext m_Context;
		Dictionary<int, ReliableFlowControl> m_FlowControls = new Dictionary<int, ReliableFlowControl>();

		ReliableChannelConfig m_Config;

		public ReliableChannel(ReliableChannelConfig config)
		{
			m_Config = config;
		}

		public void Init(short channelId, IChannelContext ctx)
		{
			m_ChannelId = channelId;
			m_Context = ctx;
		}

		ReliableFlowControl GetOrAdd(int id)
		{
			if (!m_FlowControls.TryGetValue(id, out var value))
			{
				m_FlowControls[id] = value = new ReliableFlowControl(m_ChannelId, id, m_Context, m_Config);
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
			var tmpOffset = offset;
			if (ReliableAckData.TryUnpack(buf, ref offset, out var ack))
			{
				GetOrAdd(id).ReceiveAck(ack);

			}
			offset = tmpOffset;
			if (ReliableData.TryUnpack(buf, ref offset, out var data))
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

		public void Update(in TimeSpan delta)
		{
			foreach (var val in m_FlowControls.Values)
			{
				val.Update(delta);
			}
		}

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