using SRConnection.Channel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace SRConnection
{

	public partial class Connection : IDisposable
	{
		public int SelfId => m_Impl.SelfId;

		public bool Disposed => m_Impl.Disposed;

		public ICollection<Peer> Peers => m_Peers.Values;

		public ChannelMapAccessor Channel { get; private set; }

		public readonly ChannelAccessor Reliable;

		public readonly ChannelAccessor Unreliable;

		public P2PAccessor P2P { get; private set; }

		public readonly RuntimeConfig Config;

		public event Action<PeerEvent> OnPeerEvent;

		ConnectionImpl m_Impl;
		Queue<PeerEvent> m_PeerEvent = new Queue<PeerEvent>();
		ConcurrentDictionary<int, Peer> m_Peers = new ConcurrentDictionary<int, Peer>();
		ChannelManager m_Channel;
		ConnectionStatusUpdater m_StatusUpdater;
		bool m_InitRead;
		byte[] m_ReceiveBuffer = new byte[Fragment.Size + 100];
		Queue<Message> m_BufferingMessage = new Queue<Message>();

		internal Connection(ConnectionImpl impl)
		{
			m_Channel = new ChannelManager(impl);
			m_StatusUpdater = new ConnectionStatusUpdater(this);
			Channel = new ChannelMapAccessor(this, m_Channel);

			Reliable = Channel.Reliable;
			Unreliable = Channel.Unreliable;
			m_Impl = impl;
			m_Impl.OnAddPeer += OnAdd;
			m_Impl.OnRemotePeer += OnRemove;
			if (impl.UseP2P)
			{
				P2P = new P2PAccessor(m_Impl);
			}

			Config = new RuntimeConfig(this, m_Impl, m_StatusUpdater);

			foreach (var peer in m_Impl.GetPeers())
			{
				OnAdd(peer);
			}

		}

		public void Dispose()
		{
			lock (m_Impl)
			{
				m_Channel.Dispose();
				m_Impl.Dispose();
				m_StatusUpdater.Dispose();
			}
		}

		public Peer GetPeer(int id)
		{
			m_Peers.TryGetValue(id, out var peer);
			return peer;
		}

		public bool TryGetPeer(int id, out Peer peer)
		{
			return m_Peers.TryGetValue(id, out peer);
		}

		public void Send(int connectionId, byte[] buf, bool reliable = true)
		{
			Send(connectionId, buf, 0, buf.Length, reliable);
		}

		public void Send(int connectionId, byte[] buf, int offset, int size, bool reliable = true)
		{
			lock (m_Impl)
			{
				var channel = reliable ? DefaultChannel.Reliable : DefaultChannel.Unreliable;
				m_Channel.Send(channel, connectionId, buf, offset, size);
			}
		}

		public void ChannelSend(short channel, int connectionId, byte[] buf, int offset, int size)
		{
			lock (m_Impl)
			{
				m_Channel.Send(channel, connectionId, buf, offset, size);
			}
		}

		public void ChannelSend<T>(short channel, int connectionId, Action<Stream, T> write, in T obj)
		{
			lock (m_Impl)
			{
				m_Channel.Send(channel, connectionId, write, in obj);
			}
		}

		public void ChannelBroadcast(short channel, byte[] buf, int offset, int size)
		{
			lock (m_Impl)
			{
				m_Channel.Broadcast(channel, buf, offset, size);
			}
		}

		public void ChannelBroadcast<T>(short channel, Action<Stream, T> write, in T obj)
		{
			lock (m_Impl)
			{
				m_Channel.Broadcast(channel, write, in obj);
			}
		}

		public void BroadcastDisconnect()
		{
			lock (m_Impl)
			{
				m_Impl.BroadcastDisconnect();
				Dispose();
			}
		}

		public void ManualTimeUpdate()
		{
			lock (m_Impl)
			{
				var tmp = m_HandlePeerEvent;
				try
				{
					m_HandlePeerEvent = true;
					UpdateStatus();
				}
				finally
				{
					m_HandlePeerEvent = tmp;
				}
			}
		}

		bool m_HandlePeerEvent;
		public bool TryReadMessage(out Message message)
		{
			lock (m_Impl)
			{
				try
				{
					m_InitRead = true;
					m_StatusUpdater.OnPreRead();
					while (m_PeerEvent.Count > 0)
					{
						var e = m_PeerEvent.Dequeue();
						OnPeerEvent?.Invoke(e);
					}
					m_HandlePeerEvent = true;
					var ret = TryReadChannelMessage(out message, false);
					m_StatusUpdater.TryUpdate(!ret);
					return ret;
				}
				finally
				{
					m_HandlePeerEvent = false;
				}
			}
		}

		public bool PollTryReadMessage(out Message message, TimeSpan time)
		{
			return PollTryReadMessage(out message, (int)(time.TotalMilliseconds * 1000));
		}

		public bool PollTryReadMessage(out Message message, int microSeconds)
		{
			if (TryReadMessage(out message))
			{
				return true;
			}
			if (!m_Impl.Poll(microSeconds))
			{
				return false;
			}
			return TryReadMessage(out message);
		}

		void OnAdd(PeerEntry entry)
		{
			var peer = new Peer(entry, this, m_Impl);
			m_Peers.TryAdd(peer.ConnectionId, peer);
			m_Channel.AddPeer(entry.ConnectionId);
			var e = new PeerEvent(PeerEvent.Type.Add, peer);
			if (m_HandlePeerEvent)
			{
				OnPeerEvent?.Invoke(e);
			}
			else
			{
				m_PeerEvent.Enqueue(e);
			}
		}

		void OnRemove(PeerEntry entry)
		{
			m_Peers.TryRemove(entry.ConnectionId, out var peer);
			m_Channel.RemovePeer(entry.ConnectionId);
			var e = new PeerEvent(PeerEvent.Type.Remove, peer);
			if (m_HandlePeerEvent)
			{
				OnPeerEvent?.Invoke(e);
			}
			else
			{
				m_PeerEvent.Enqueue(e);
			}
		}


		void PreReadMessage()
		{
			TryReadChannelMessage(out _, true);
		}

		bool TryReadChannelMessage(out Message message, bool buffering)
		{
			while (!buffering && m_BufferingMessage.Count > 0)
			{
				message = m_BufferingMessage.Dequeue();
				//切断済みのPeerのメッセージは飛ばさない
				if (!m_Peers.ContainsKey(message.Peer.ConnectionId))
				{
					continue;
				}
				return true;
			}
			int size = 0;
			int id = 0;
			while (m_Impl.TryReceiveFrom(m_ReceiveBuffer, 0, ref size, ref id))
			{
				if (!TryGetPeer(id, out var peer))
				{
					continue;
				}
				if (m_Channel.TryRead(peer, m_ReceiveBuffer, size, out message))
				{
					if (buffering)
					{
						m_BufferingMessage.Enqueue(message.Copy());
						continue;
					}
					return true;
				}
			}
			message = default;
			return false;
		}

		DateTime m_PrevTime = DateTime.UtcNow;
		internal void UpdateStatus()
		{
			lock (m_Impl)
			{
				m_StatusUpdater.OnUpdate();
				var now = DateTime.UtcNow;
				var delta = now - m_PrevTime;
				if (delta < TimeSpan.Zero) delta = TimeSpan.Zero;
				if (delta > TimeSpan.FromMilliseconds(1000)) delta = TimeSpan.FromMilliseconds(1000);
				m_PrevTime = now;
				m_Impl.OnUpdateStatus(delta);
				m_Channel.OnUpdateStatus(delta);
				if (!m_InitRead)
				{
					PreReadMessage();
				}
			}
		}

	}

}