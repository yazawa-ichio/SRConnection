using SRNet.Channel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SRNet
{

	public partial class Connection : IDisposable
	{
		public int SelfId => m_Impl.SelfId;

		public bool Disposed => m_Impl.Disposed;

		public bool AutoDisposeOnDisconnectOwner
		{
			get => m_Impl.DisposeOnDisconnectOwner;
			set => m_Impl.DisposeOnDisconnectOwner = value;
		}

		public ICollection<Peer> Peers => m_Peers.Values;

		public ChannelMapAccessor Channel { get; private set; }

		public readonly ChannelAccessor Reliable;

		public readonly ChannelAccessor Unreliable;

		public P2PAccessor P2P { get; private set; }

		public event Action<PeerEvent> OnPeerEvent;

		ConnectionImpl m_Impl;
		Queue<PeerEvent> m_PeerEvent = new Queue<PeerEvent>();
		ConcurrentDictionary<int, Peer> m_Peers = new ConcurrentDictionary<int, Peer>();
		ChannelContext m_ChannelContext;

		internal Connection(ConnectionImpl impl)
		{
			m_ChannelContext = new ChannelContext(impl, m_Peers);
			Channel = new ChannelMapAccessor(m_ChannelContext);
			Reliable = Channel.Reliable;
			Unreliable = Channel.Unreliable;
			m_Impl = impl;
			m_Impl.OnAddPeer += OnAdd;
			m_Impl.OnRemotePeer += OnRemove;
			m_Impl.OnPostTimerUpdate += OnPostTimerUpdate;
			foreach (var peer in m_Impl.GetPeers())
			{
				OnAdd(peer);
			}
			if (impl.UseP2P)
			{
				P2P = new P2PAccessor(m_Impl, m_ChannelContext);
			}
		}

		void OnPostTimerUpdate(DateTime now, TimeSpan delta)
		{
			m_ChannelContext.Update(delta);
		}

		public void Dispose()
		{
			m_ChannelContext.Dispose();
			m_Impl.Dispose();
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
			var channel = reliable ? DefaultChannel.Unreliable : DefaultChannel.Unreliable;
			m_ChannelContext.Send(channel, connectionId, buf, offset, size);
		}

		public void BroadcastDisconnect()
		{
			m_Impl.BroadcastDisconnect();
			Dispose();
		}

		bool m_HandlePeerEvent;
		public bool TryReceive(out Message message)
		{
			lock (m_PeerEvent)
			{
				try
				{
					while (m_PeerEvent.Count > 0)
					{
						var e = m_PeerEvent.Dequeue();
						OnPeerEvent?.Invoke(e);
					}
					m_HandlePeerEvent = true;
					return m_ChannelContext.TryReadMessage(out message);
				}
				finally
				{
					m_HandlePeerEvent = false;
				}
			}
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
				var peer = new Peer(entry, m_Impl, m_ChannelContext);
				m_Peers.TryAdd(peer.ConnectionId, peer);
				m_ChannelContext.AddPeer(entry.ConnectionId);
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
		}

		void OnRemove(PeerEntry entry)
		{
			lock (m_PeerEvent)
			{
				m_Peers.TryRemove(entry.ConnectionId, out var peer);
				m_ChannelContext.RemovePeer(entry.ConnectionId);
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
		}

	}

}