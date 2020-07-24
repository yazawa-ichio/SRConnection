using SRNet.Packet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SRNet
{
	internal class PeerManager
	{
		ConcurrentDictionary<int, PeerEntry> m_PeerMap = new ConcurrentDictionary<int, PeerEntry>();
		List<PeerEntry> m_PeerList = new List<PeerEntry>();
		Queue<PeerEntry> m_RemoveQueue = new Queue<PeerEntry>();

		ConnectionImpl m_Connection;

		public PeerManager(ConnectionImpl connection)
		{
			m_Connection = connection;
		}

		public ICollection<PeerEntry> GetPeers()
		{
			return m_PeerMap.Values;
		}

		public void Update(TimeSpan delta)
		{
			lock (m_PeerList)
			{
				m_RemoveQueue.Clear();
				foreach (var peer in m_PeerList)
				{
					if (peer.CheckTimeout(delta))
					{
						m_RemoveQueue.Enqueue(peer);
					}
				}
				while (m_RemoveQueue.Count > 0)
				{
					Remove(m_RemoveQueue.Dequeue().ConnectionId);
				}
			}
		}

		public void Add(PeerEntry peer)
		{
			lock (m_PeerList)
			{
				if (m_PeerMap.TryAdd(peer.ConnectionId, peer))
				{
					m_PeerList.Add(peer);
					m_Connection.OnAdd(peer);
				}
			}
		}

		public void Remove(int connectionId)
		{
			lock (m_PeerList)
			{
				if (m_PeerMap.TryRemove(connectionId, out PeerEntry peer))
				{
					m_PeerList.Remove(peer);
					m_Connection.OnRemove(peer);
					peer.Dispose();
				}
			}
		}

		public PeerInfo[] CreatePeerInfoList(byte[] randam)
		{
			lock (m_PeerList)
			{
				PeerInfo[] ret = new PeerInfo[m_PeerList.Count];
				for (int i = 0; i < ret.Length; i++)
				{
					var peer = m_PeerList[i];
					ret[i] = new PeerInfo(peer.ConnectionId, new PeerEndPoint(peer.EndPoint), randam);
				}
				return ret;
			}
		}

		public bool TryGetValue(int id, out PeerEntry peer)
		{
			return m_PeerMap.TryGetValue(id, out peer);
		}

		public void ForEach<T>(Action<PeerEntry, T> action, T obj)
		{
			lock (m_PeerList)
			{
				foreach (var peer in m_PeerList)
				{
					try
					{
						action?.Invoke(peer, obj);
					}
					catch (Exception ex)
					{
						Log.Warning("foreach error {0} : {1}", peer.ConnectionId, ex);
					}
				}
			}
		}

		public void ForEach(Action<PeerEntry> action)
		{
			lock (m_PeerList)
			{
				foreach (var peer in m_PeerList)
				{
					try
					{
						action?.Invoke(peer);
					}
					catch (Exception ex)
					{
						Log.Warning("foreach error {0} : {1}", peer.ConnectionId, ex);
					}
				}
			}
		}

	}

}