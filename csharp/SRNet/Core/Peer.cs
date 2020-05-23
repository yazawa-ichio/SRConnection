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
		ConnectionImpl m_Connection;
		ChannelContext m_Channel;

		internal Peer(PeerEntry entry, ConnectionImpl connection, ChannelContext channel)
		{
			m_Entry = entry;
			m_Connection = connection;
			m_Channel = channel;
		}

		public void Send(byte[] buf, int offset, int size, bool reliable = true)
		{
			short channel = reliable ? DefaultChannel.Reliable : DefaultChannel.Unreliable;
			m_Channel.Send(channel, m_Entry.ConnectionId, buf, offset, size);
		}

		public void Send(byte[] buf, bool reliable = true)
		{
			short channel = reliable ? DefaultChannel.Reliable : DefaultChannel.Unreliable;
			m_Channel.Send(channel, m_Entry.ConnectionId, buf, 0, buf.Length);
		}

		public void Send<T>(Action<Stream, T> write, in T obj, bool reliable = true)
		{
			short channel = reliable ? DefaultChannel.Reliable : DefaultChannel.Unreliable;
			m_Channel.Send(channel, m_Entry.ConnectionId, write, obj);
		}

		public void Send(short channel, byte[] buf, int offset, int size)
		{
			m_Channel.Send(channel, m_Entry.ConnectionId, buf, offset, size);
		}

		public void Send(short channel, byte[] buf)
		{
			m_Channel.Send(channel, m_Entry.ConnectionId, buf, 0, buf.Length);
		}

		public void Send<T>(short channel, Action<Stream, T> write, in T obj)
		{
			m_Channel.Send(channel, m_Entry.ConnectionId, write, obj);
		}

		public bool SendPing()
		{
			return m_Connection.SendPing(m_Entry.ConnectionId);
		}

		public bool SendDisconnect()
		{
			return m_Connection.SendDisconnect(m_Entry.ConnectionId);
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