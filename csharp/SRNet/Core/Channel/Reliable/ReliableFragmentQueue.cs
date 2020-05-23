using System;
using System.Collections.Generic;

namespace SRNet.Channel
{
	public class ReliableFragmentQueue : IDisposable
	{
		SortedList<short, Fragment> m_Buffer = new SortedList<short, Fragment>(SeqUtil.Comparer);
		Dictionary<short, int> m_Count = new Dictionary<short, int>();
		short m_NextSequence = 1;

		public short ReceivedSequence = 0;
		public short NextReceivedSequence = 0;
		public short LastReceivedSequence;

		bool m_Disposed;

		public bool Enqueue(in ReliableData data)
		{
			lock (m_Buffer)
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
				return;
			}
			if (m_Count.TryGetValue(fragment.Id, out var count))
			{
				m_Count[fragment.Id] = count + 1;
			}
			else
			{
				m_Count[fragment.Id] = 1;
			}
		}

		public bool TryDequeue(List<Fragment> output)
		{
			lock (m_Buffer)
			{
				if (m_Disposed) return false;
				if (!m_Buffer.TryGetValue(m_NextSequence, out var fragment))
				{
					return false;
				}
				if (fragment.Length == 1)
				{
					m_Buffer.Remove(m_NextSequence++);
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
					m_Buffer.TryGetValue(m_NextSequence, out fragment);
					m_Buffer.Remove(m_NextSequence++);
					output.Add(fragment);
				}
				return true;
			}
		}

		public void Dispose()
		{
			lock (m_Buffer)
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
}