using SRNet.Channel;
using SRNet.Stun;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SRNet.Unity
{
	public class P2PClient : MonoBehaviour
	{
		public event Action OnConnect;

		public event Action OnDisconnect;

		public event Action<Peer> OnAddPeer;

		public event Action<Peer> OnRemovePeer;

		public event Action<Peer, byte[]> OnMessage;

		public event Action<Message> OnRawMessage;

		public bool IsConnection => m_Connection != null && !m_Connection.Disposed;

		public int SelfId => m_Connection.SelfId;

		public ICollection<Peer> Peers => m_Connection.Peers;

		public ChannelMapAccessor Channel => m_Connection.Channel;

		public ChannelAccessor Reliable => m_Connection.Reliable;

		public ChannelAccessor Unreliable => m_Connection.Unreliable;

		public bool AutoDispatch { get; set; } = true;

		Connection m_Connection;
		CancellationTokenSource m_Cancellation = default;
		P2PHostConnection m_HostConnection;

		public async Task RemoteMatching(string url, CancellationToken token = default)
		{
			InitConnect(token);
			SetConnection(await Connection.P2PMatching(url, token: m_Cancellation.Token));
		}

		public async Task RemoteMatching(Func<StunResult, CancellationToken, Task<P2PSettings>> func, CancellationToken token = default)
		{
			InitConnect(token);
			SetConnection(await Connection.P2PMatching(func, token: m_Cancellation.Token));
		}

		public void StartHost(string roomName)
		{
			Disconnect();
			SetConnection(m_HostConnection = Connection.StartLocalHost(roomName));
		}

		public async Task ConnectHost(DiscoveryRoom room, CancellationToken token = default)
		{
			InitConnect(token);
			SetConnection(await Connection.Connect(room, token: m_Cancellation.Token));
		}

		public Task ConnectHost(string roomName) => ConnectHost(roomName, new CancellationTokenSource(10000).Token);

		public async Task ConnectHost(string roomName, CancellationToken token)
		{
			InitConnect(token);
			var room = await DiscoveryUtil.GetRoom(roomName, token);
			SetConnection(await Connection.Connect(room, token: m_Cancellation.Token));
		}

		protected void InitConnect(CancellationToken token)
		{
			Disconnect();
			m_Cancellation = new CancellationTokenSource();
			token.Register(m_Cancellation.Cancel);
		}

		protected void SetConnection(Connection connection)
		{
			m_Connection = connection;
			m_Connection.OnPeerEvent += OnPeerEvent;
			m_Cancellation = null;
			OnConnect?.Invoke();
			Update();
		}

		public void Disconnect()
		{
			if (m_Connection == null)
			{
				m_Cancellation?.Cancel();
				m_Cancellation = null;
			}
			else
			{
				if (!m_Connection.Disposed)
				{
					m_Connection.BroadcastDisconnect();
					m_Connection = null;
					m_HostConnection = null;
				}
				OnDisconnect?.Invoke();
			}
		}

		public void StopHostMatching()
		{
			m_HostConnection.StopMatching();
		}

		public Peer GetPeer(int id)
		{
			return m_Connection.GetPeer(id);
		}

		public bool TryGetPeer(int id, out Peer peer)
		{
			return m_Connection.TryGetPeer(id, out peer);
		}

		public void Broadcast(byte[] buf, bool reliable = true)
		{
			Broadcast(buf, 0, buf.Length, reliable);
		}

		public void Broadcast(byte[] buf, int offset, int size, bool reliable = true)
		{
			if (reliable)
			{
				Reliable.Broadcast(buf, offset, size);
			}
			else
			{
				Unreliable.Broadcast(buf, offset, size);
			}
		}

		public void Broadcast<T>(Action<Stream, T> write, in T obj, bool reliable = true)
		{
			if (reliable)
			{
				Reliable.Broadcast(write, in obj);
			}
			else
			{
				Unreliable.Broadcast(write, in obj);
			}
		}

		public void Dispatch()
		{
			if (m_Connection == null) return;

			if (m_Connection.Disposed)
			{
				Disconnect();
				return;
			}

			while (m_Connection.TryReceive(out var message))
			{
				OnRawMessage?.Invoke(message);

				OnMessage?.Invoke(message.Peer, message.ToArray());

				if (m_Connection == null)
				{
					return;
				}

				if (m_Connection.Disposed)
				{
					Disconnect();
					return;
				}

			}
		}

		void Update()
		{
			if (AutoDispatch)
			{
				Dispatch();
			}
		}

		void OnDestroy()
		{
			Disconnect();
		}

		void OnPeerEvent(PeerEvent e)
		{
			switch (e.EventType)
			{
				case PeerEvent.Type.Add:
					OnAddPeer?.Invoke(e.Peer);
					break;
				case PeerEvent.Type.Remove:
					OnRemovePeer?.Invoke(e.Peer);
					break;
			}
		}

	}

}