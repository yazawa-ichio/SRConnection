using SRNet.Channel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SRNet
{
	public static class DefaultChannel
	{
		public const short Reliable = 1;
		public const short Unreliable = 2;
	}

	public partial class Connection : IDisposable
	{


		ConnectionImpl m_Impl;
		Queue<PeerEvent> m_PeerEvent = new Queue<PeerEvent>();
		ConcurrentDictionary<int, Peer> m_Peers = new ConcurrentDictionary<int, Peer>();
		ChannelContext m_Channel;

		public int SelfId => m_Impl.SelfId;

		public bool Disposed => m_Impl.Disposed;

		public bool AutoDisposeOnDisconnectOwner
		{
			get => m_Impl.DisposeOnDisconnectOwner;
			set => m_Impl.DisposeOnDisconnectOwner = value;
		}

		public ConnectionChannelAccessor Reliable => Channel(DefaultChannel.Reliable);

		public ConnectionChannelAccessor Unreliable => Channel(DefaultChannel.Unreliable);

		public P2PAccessor P2P { get; private set; }

		internal Connection(ConnectionImpl impl)
		{
			m_Channel = new ChannelContext(impl, m_Peers);
			m_Channel.Bind(DefaultChannel.Reliable, new ReliableChannelConfig());
			m_Channel.Bind(DefaultChannel.Unreliable, new UnreliableChannelConfig());
			m_Impl = impl;
			m_Impl.OnAddPeer += OnAdd;
			m_Impl.OnRemotePeer += OnRemote;
			m_Impl.OnPostTimerUpdate += OnPostTimerUpdate;
			foreach (var peer in m_Impl.GetPeers())
			{
				OnAdd(peer);
			}
			if (impl.UseP2P)
			{
				P2P = new P2PAccessor(m_Impl, m_Channel);
			}
		}

		void OnPostTimerUpdate(DateTime now, TimeSpan delta)
		{
			m_Channel.Update(delta);
		}

		public void Dispose()
		{
			m_Channel.Dispose();
			m_Impl.Dispose();
		}

		bool m_HandlePeerEvent;

		public bool TryGetPeerEvent(out PeerEvent peerEvent)
		{
			lock (m_PeerEvent)
			{
				if (!m_HandlePeerEvent)
				{
					foreach (var peer in m_Peers)
					{
						m_PeerEvent.Enqueue(new PeerEvent(PeerEvent.Type.Add, peer.Value));
					}
					m_HandlePeerEvent = true;
				}
				if (m_PeerEvent.Count == 0)
				{
					peerEvent = default;
					return false;
				}
				peerEvent = m_PeerEvent.Dequeue();
				return true;
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

		public ICollection<Peer> GetPeers()
		{
			return m_Peers.Values;
		}

		public T BindChannel<T>(short id, Action<T> action = null) where T : IConfig, new()
		{
			T config = new T();
			action?.Invoke(config);
			BindChannel(id, config);
			return config;
		}

		public void BindChannel(short id, IConfig config)
		{
			if (id <= 100) throw new ArgumentException("user channel is greater than 100", nameof(id));
			m_Channel.Bind(id, config);
		}

		public void UnbindChannel(short id)
		{
			if (id <= 100) throw new ArgumentException("user channel is greater than 100", nameof(id));
			m_Channel.Unbind(id);
		}

		public ConnectionChannelAccessor Channel(short channel) => new ConnectionChannelAccessor(channel, m_Channel);

		public void Send(int connectionId, byte[] buf, bool reliable = true)
		{
			Send(connectionId, buf, 0, buf.Length, reliable);
		}

		public void Send(int connectionId, byte[] buf, int offset, int size, bool reliable = true)
		{
			var channel = reliable ? DefaultChannel.Unreliable : DefaultChannel.Unreliable;
			m_Channel.Send(channel, connectionId, buf, offset, size);
		}

		public void BroadcastDisconnect()
		{
			m_Impl.BroadcastDisconnect();
			Dispose();
		}

		public bool TryReceive(out Message message)
		{
			return m_Channel.TryReadMessage(out message);
		}

		public bool TryPollReceive(out Message message, TimeSpan time)
		{
			return TryPollReceive(out message, (int)(time.TotalMilliseconds * 1000));
		}

		public bool TryPollReceive(out Message message, int microSeconds)
		{
			if (TryReceive(out message))
			{
				return true;
			}
			return (m_Impl.Poll(microSeconds) && TryReceive(out message));
		}


		void OnAdd(PeerEntry entry)
		{
			lock (m_PeerEvent)
			{
				var peer = new Peer(entry, m_Impl, m_Channel);
				m_Peers.TryAdd(peer.ConnectionId, peer);
				if (m_HandlePeerEvent)
				{
					m_PeerEvent.Enqueue(new PeerEvent(PeerEvent.Type.Add, peer));
				}
			}
		}

		void OnRemote(PeerEntry entry)
		{
			lock (m_PeerEvent)
			{
				if (m_Peers.TryRemove(entry.ConnectionId, out var peer))
				{
					if (m_HandlePeerEvent)
					{
						m_PeerEvent.Enqueue(new PeerEvent(PeerEvent.Type.Remove, peer));
					}
				}
			}
		}
	}

}