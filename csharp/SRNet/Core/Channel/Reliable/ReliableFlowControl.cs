using System;
using System.Collections.Generic;

namespace SRNet.Channel
{

	public class ReliableFlowControl : IDisposable
	{
		short m_ChannelId;
		int m_ConnectionId;
		IChannelContext m_Context;

		List<ReliableData> m_AckWaitList = new List<ReliableData>();
		Queue<ReliableData> m_SendWaitList = new Queue<ReliableData>();
		ReliableFragmentQueue m_ReceiveQueue;
		DateTime m_SendAckAt;
		short m_SendSequence;
		ReliableChannelConfig m_Config;
		bool m_SendAckFlag;

		public ReliableFlowControl(short channelId, int connectionId, IChannelContext ctx, ReliableChannelConfig config = null)
		{
			m_ChannelId = channelId;
			m_ConnectionId = connectionId;
			m_Context = ctx;
			m_Config = config ?? new ReliableChannelConfig();
			m_ReceiveQueue = new ReliableFragmentQueue();
		}

		public void Send(List<Fragment> input)
		{
			foreach (var fragment in input)
			{
				lock (m_AckWaitList)
				{
					var data = new ReliableData(m_ChannelId, ++m_SendSequence, fragment);
					if (m_AckWaitList.Count < m_Config.MaxWindowSize)
					{
						m_AckWaitList.Add(data);
						Send(data);
					}
					else
					{
						m_SendWaitList.Enqueue(data);
					}
				}
			}
		}

		void Send(in ReliableData data)
		{
			data.Pack(m_Context.SharedSendBuffer, m_ReceiveQueue.ReceivedSequence, out var size);
			m_Context.Send(m_ConnectionId, m_Context.SharedSendBuffer, 0, size, m_Config.Encrypt);
		}

		public void SendAck()
		{
			m_SendAckFlag = false;
			m_SendAckAt = DateTime.UtcNow;
			var cur = m_ReceiveQueue.ReceivedSequence;
			var next = m_ReceiveQueue.NextReceivedSequence;
			var last = m_ReceiveQueue.LastReceivedSequence;
			new ReliableAckData(m_ChannelId, cur, next, last).Pack(m_Context.SharedSendBuffer, out var size);
			m_Context.Send(m_ConnectionId, m_Context.SharedSendBuffer, 0, size, m_Config.Encrypt);
		}

		public bool TryDequeue(List<Fragment> output)
		{
			return m_ReceiveQueue.TryDequeue(output);
		}

		public bool Enqueue(in ReliableData packet)
		{
			bool ret = m_ReceiveQueue.Enqueue(in packet);
			m_SendAckFlag = true;
			if (!ret || DateTime.UtcNow - m_SendAckAt > TimeSpan.FromMilliseconds(100))
			{
				SendAck();
			}
			return ret;
		}

		public void ReceiveAck(short receivedSequence)
		{
			lock (m_AckWaitList)
			{
				while (m_AckWaitList.Count > 0)
				{
					var packet = m_AckWaitList[0];
					if (SeqUtil.IsGreater(packet.Sequence, receivedSequence))
					{
						break;
					}
					packet.Dispose();
					m_AckWaitList.RemoveAt(0);
				}
				while (m_SendWaitList.Count > 0 && m_AckWaitList.Count < m_Config.MaxWindowSize)
				{
					var data = m_SendWaitList.Dequeue();
					m_AckWaitList.Add(data);
					Send(data);
				}
			}
		}

		public void ReceiveAck(in ReliableAckData data)
		{
			lock (m_AckWaitList)
			{
				ReceiveAck(data.ReceivedSequence);
				for (int i = 0; i < m_Config.MaxWindowSize && i < m_AckWaitList.Count; i++)
				{
					var packet = m_AckWaitList[i];
					if (SeqUtil.IsGreaterEqual(packet.Sequence, data.NextReceivedSequence))
					{
						break;
					}
					Send(packet);
				}
			}
		}

		public void Update(in TimeSpan delta)
		{
			//TODO:最適化。時間を見る。
			Resend((int)(m_Config.MaxWindowSize / 4d));
			if (m_SendAckFlag)
			{
				SendAck();
			}
		}

		void Resend(int max)
		{
			lock (m_AckWaitList)
			{
				for (int i = 0; i < m_AckWaitList.Count && i < max; i++)
				{
					Send(m_AckWaitList[i]);
				}
			}
		}

		public void Dispose()
		{
			lock (m_AckWaitList)
			{
				foreach (var packet in m_AckWaitList)
				{
					packet.Dispose();
				}
				m_AckWaitList.Clear();
			}
		}

	}

}

