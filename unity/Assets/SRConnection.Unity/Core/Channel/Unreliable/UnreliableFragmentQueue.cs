﻿using System;
using System.Collections.Generic;

namespace SRConnection.Channel
{
	public class UnreliableFragmentQueue : IDisposable
	{
		SortedList<short, Fragment> m_Buffer = new SortedList<short, Fragment>();
		Dictionary<short, int> m_Count = new Dictionary<short, int>();
		HashSet<short> m_DequeueList = new HashSet<short>();
		Queue<short> m_RemoveBuffer = new Queue<short>();
		short m_LastDequeueSequence = -1;
		bool m_Disposed;
		UnreliableChannelConfig m_Config;

		public UnreliableFragmentQueue(UnreliableChannelConfig config)
		{
			m_Config = config;
		}

		public void Enqueue(in UnreliableData data)
		{
			if (m_Disposed)
			{
				data.Dispose();
				return;
			}
			if (m_Config.Ordered && SeqUtil.IsGreaterEqual(m_LastDequeueSequence, data.Sequence))
			{
				data.Dispose();
				return;
			}
			if (m_Buffer.ContainsKey(data.Sequence))
			{
				data.Dispose();
				return;
			}
			var payload = data.Payload;
			m_Buffer.Add(data.Sequence, payload);
			UpdateCount(payload);
			while (m_Config.MaxBufferSize <= m_Buffer.Count && m_Count.Count > 1)
			{
				if (!TryRemoveBuffer())
				{
					break;
				}
			}

		}

		bool TryRemoveBuffer()
		{
			// 次に取り出されるのでバッファーの縮小は無視する
			var fragment = m_Buffer.Values[0];
			if (m_DequeueList.Contains(fragment.Id))
			{
				return false;
			}
			// 同一IDの物は削除
			foreach (var kvp in m_Buffer)
			{
				if (fragment.Id != kvp.Value.Id)
				{
					break;
				}
				kvp.Value.RemoveRef();
				m_Count.Remove(fragment.Id);
				m_RemoveBuffer.Enqueue(kvp.Key);
			}
			while (m_RemoveBuffer.Count > 0)
			{
				m_Buffer.Remove(m_RemoveBuffer.Dequeue());
			}
			return true;
		}

		void UpdateCount(Fragment fragment)
		{
			if (fragment.Length == 1)
			{
				m_DequeueList.Add(fragment.Id);
				return;
			}
			if (m_Count.TryGetValue(fragment.Id, out var count))
			{
				count++;
				if (count < fragment.Length)
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
				TryRemoveLessThanLastDequeueSequence();
			}

			if (m_DequeueList.Count == 0) return false;

			short start = 0;
			int len = 0;
			foreach (var kvp in m_Buffer)
			{
				if (m_DequeueList.Contains(kvp.Value.Id))
				{
					start = kvp.Key;
					len = kvp.Value.Length;
					m_DequeueList.Remove(kvp.Value.Id);
					break;
				}
			}
			if (m_Config.Ordered)
			{
				m_LastDequeueSequence = start;
			}
			for (int i = 0; i < len; i++)
			{
				m_Buffer.TryGetValue(start, out var fragment);
				m_Buffer.Remove(start++);
				output.Add(fragment);
			}
			return true;

		}

		void TryRemoveLessThanLastDequeueSequence()
		{
			foreach (var kvp in m_Buffer)
			{
				if (!SeqUtil.IsGreaterEqual(m_LastDequeueSequence, kvp.Key))
				{
					break;
				}
				kvp.Value.RemoveRef();
				m_Count.Remove(kvp.Value.Id);
				m_RemoveBuffer.Enqueue(kvp.Key);
			}
			while (m_RemoveBuffer.Count > 0)
			{
				m_Buffer.Remove(m_RemoveBuffer.Dequeue());
			}
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