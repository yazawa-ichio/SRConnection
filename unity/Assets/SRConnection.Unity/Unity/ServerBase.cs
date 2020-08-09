using SRConnection.Channel;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SRConnection.Unity
{

	public abstract class ServerBase : MonoBehaviour
	{

		public ICollection<Peer> Peers => m_Connection.Peers;

		public ChannelMapAccessor Channel => m_Connection.Channel;

		public ChannelAccessor Reliable => m_Connection.Reliable;

		public ChannelAccessor Unreliable => m_Connection.Unreliable;

		protected virtual bool AutoDispatch => true;

		protected virtual bool UseCopyMessage => false;

		Connection m_Connection;

		public void Setup(ServerConfig config)
		{
			Disconnect();
			m_Connection = Connection.StartServer(config);
			m_Connection.OnPeerEvent += OnPeerEvent;
		}

		public void Dispatch()
		{
			if (m_Connection == null) return;

			if (m_Connection.Disposed)
			{
				Disconnect();
				return;
			}

			OnPreDispatch();

			while (m_Connection.TryReadMessage(out var message))
			{
				if (UseCopyMessage)
				{
					OnMessage(message.Copy());
				}
				else
				{
					OnMessage(message);
				}

				if (m_Connection == null)
				{
					break;
				}

				if (m_Connection.Disposed)
				{
					break;
				}

			}

			OnPostDispatch();
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

		public void Disconnect()
		{
			if (m_Connection != null)
			{
				OnPreDisconnect();
				m_Connection.BroadcastDisconnect();
				m_Connection.Dispose();
				m_Connection = null;
				OnPostDisconnect();
			}
		}

		protected virtual void OnPreDispatch() { }

		protected virtual void OnPostDispatch() { }

		protected abstract void OnAddPeer(Peer peer);

		protected abstract void OnRemovePeer(Peer peer);

		protected abstract void OnMessage(Message message);

		protected virtual void OnPreDisconnect() { }

		protected virtual void OnPostDisconnect() { }


		void Update()
		{
			if (AutoDispatch)
			{
				Dispatch();
			}
		}

		void OnPeerEvent(PeerEvent e)
		{
			switch (e.EventType)
			{
				case PeerEvent.Type.Add:
					OnAddPeer(e.Peer);
					break;
				case PeerEvent.Type.Remove:
					OnRemovePeer(e.Peer);
					break;
			}
		}
	}

}