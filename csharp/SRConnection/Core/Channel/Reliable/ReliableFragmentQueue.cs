using System;
using System.Collections.Generic;

namespace SRConnection.Channel
{
	public class ReliableFragmentQueue : IDisposable
	{
		ReliableChannelConfig m_Config;
		SortedList<short, Fragment> m_Buffer = new SortedList<short, Fragment>(SeqUtil.Comparer);
		Dictionary<short, int> m_Count = new Dictionary<short, int>();
		HashSet<short> m_DequeueList = new HashSet<short>();
		Queue<short> m_RemoveBuffer = new Queue<short>();
		short m_OrderedNextSequence = 1;

		public short ReceivedSequence = 0;
		public short NextReceivedSequence = 0;
		public short LastReceivedSequence;

		bool m_Disposed;

		public ReliableFragmentQueue(ReliableChannelConfig config)
		{
			m_Config = config;
		}

		public bool Enqueue(in ReliableData data)
		{
			if (m_Disposed)
			{
				data.Dispose();
				return false;
			}
			if (!SeqUtil.IsGreater(data.Sequence, ReceivedSequence))
			{
				data.Dispose();
				return false;
			}
			if (m_Buffer.ContainsKey(data.Sequence))
			{
				data.Dispose();
				return false;
			}
			var payload = data.Payload;
			m_Buffer.Add(data.Sequence, payload);
			UpdateReceiveSequence(data.Sequence);
			UpdateCount(payload);
			return true;
		}

		void UpdateReceiveSequence(short seq)
		{
			if (SeqUtil.IsGreater(seq, LastReceivedSequence))
			{
				LastReceivedSequence = seq;
			}
			while (m_Buffer.ContainsKey(SeqUtil.Increment(ReceivedSequence)))
			{
				ReceivedSequence++;
			}
			if (LastReceivedSequence == ReceivedSequence)
			{
				NextReceivedSequence = -1;
			}
			else
			{
				var nextSeq = SeqUtil.Increment(ReceivedSequence);
				while (nextSeq < LastReceivedSequence && !m_Buffer.ContainsKey(SeqUtil.Increment(nextSeq)))
				{
					nextSeq++;
				}
				NextReceivedSequence = nextSeq;
			}
		}

		void UpdateCount(Fragment fragment)
		{
			if (fragment.Length == 1)
			{
				if (!m_Config.Ordered)
				{
					m_DequeueList.Add(fragment.Id);
				}
				return;
			}
			if (m_Count.TryGetValue(fragment.Id, out var count))
			{
				count++;
				if (m_Config.Ordered || count < fragment.Length)
				{
					m_Count[fragment.Id] = count;
				}
				else
				{
					m_Count.Remove(fragment.Id);
					m_DequeueList.Add(fragment.Id);
				}
			}
			else
			{
				m_Count[fragment.Id] = 1;
			}
		}

		public bool TryDequeue(List<Fragment> output)
		{
			if (m_Disposed) return false;

			if (m_Config.Ordered)
			{
				return TryOrderDequeue(output);
			}
			else
			{
				return TryFastDequeue(output);
			}
		}

		bool TryOrderDequeue(List<Fragment> output)
		{
			if (!m_Buffer.TryGetValue(m_OrderedNextSequence, out var fragment))
			{
				return false;
			}
			if (fragment.Length == 1)
			{
				m_Buffer.Remove(m_OrderedNextSequence++);
				output.Add(fragment);
				return true;
			}
			if (m_Count[fragment.Id] != fragment.Length)
			{
				return false;
			}
			m_Count.Remove(fragment.Id);
			for (int i = 0; i < fragment.Length; i++)
			{
				m_Buffer.TryGetValue(m_OrderedNextSequence, out fragment);
				m_Buffer.Remove(m_OrderedNextSequence++);
				output.Add(fragment);
			}
			return true;
		}

		bool TryFastDequeue(List<Fragment> output)
		{
			if (m_DequeueList.Count == 0) return false;

			short start = 0;
			int len = 0;
			foreach (var kvp in m_Buffer)
			{
				if (kvp.Value == null)
				{
					continue;
				}
				if (m_DequeueList.Contains(kvp.Value.Id))
				{
					start = kvp.Key;
					len = kvp.Value.Length;
					m_DequeueList.Remove(kvp.Value.Id);
					break;
				}
			}

			for (int i = 0; i < len; i++)
			{
				m_Buffer.TryGetValue(start, out var fragment);
				m_Buffer.Remove(start);
				m_Buffer.Add(start++, null);
				output.Add(fragment);
			}

			foreach (var kvp in m_Buffer)
			{
				if (kvp.Value != null)
				{
					break;
				}
				m_RemoveBuffer.Enqueue(kvp.Key);
			}
			while (m_RemoveBuffer.Count > 0)
			{
				m_Buffer.Remove(m_RemoveBuffer.Dequeue());
			}

			return true;
		}

		public void Dispose()
		{
			m_Disposed = true;
			foreach (var fragment in m_Buffer.Values)
			{
				fragment.RemoveRef();
			}
			m_Buffer.Clear();
		}
	}
}