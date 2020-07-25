using SRNet.Channel;
using System;
using System.IO;

namespace SRNet
{
	public class Peer : IEquatable<Peer>
	{
		public bool IsConnection => m_Entry.Disposed && !m_Connection.Disposed;

		public int ConnectionId => m_Entry.ConnectionId;

		PeerEntry m_Entry;
		Connection m_Connection;
		ConnectionImpl m_Impl;

		internal Peer(PeerEntry entry, Connection connection, ConnectionImpl impl)
		{
			m_Entry = entry;
			m_Connection = connection;
			m_Impl = impl;
		}

		public void Send(byte[] buf, bool reliable = true)
		{
			Send(buf, 0, buf.Length, reliable);
		}

		public void Send(byte[] buf, int offset, int size, bool reliable = true)
		{
			short channel = reliable ? DefaultChannel.Reliable : DefaultChannel.Unreliable;
			m_Connection.ChannelSend(channel, m_Entry.ConnectionId, buf, offset, size);
		}

		public void Send<T>(Action<Stream, T> write, in T obj, bool reliable = true)
		{
			short channel = reliable ? DefaultChannel.Reliable : DefaultChannel.Unreliable;
			m_Connection.ChannelSend(channel, m_Entry.ConnectionId, write, obj);
		}

		public PeerChannelAccessor Channel(short channel) => new PeerChannelAccessor(channel, m_Entry.ConnectionId, m_Connection);

		public bool Ping()
		{
			lock (m_Impl)
			{
				return m_Impl.SendPing(m_Entry.ConnectionId);
			}
		}

		public bool Disconnect()
		{
			lock (m_Impl)
			{
				return m_Impl.SendDisconnect(m_Entry.ConnectionId);
			}
		}

		public bool Equals(Peer other)
		{
			return other.ConnectionId == ConnectionId;
		}

		public override bool Equals(object obj)
		{
			if (obj is Peer peer)
			{
				return peer.ConnectionId == ConnectionId;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ConnectionId.GetHashCode();
		}

	}

}