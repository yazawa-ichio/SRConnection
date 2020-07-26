using System;
using System.IO;

namespace SRNet.Channel
{
	public readonly struct ChannelAccessor
	{
		public readonly short Id;
		readonly Connection m_Conn;

		internal ChannelAccessor(short channel, Connection conn)
		{
			Id = channel;
			m_Conn = conn;
		}

		public PeerChannelAccessor Target(int connectionId)
		{
			return new PeerChannelAccessor(Id, connectionId, m_Conn);
		}

		public PeerChannelAccessor Target(Peer peer)
		{
			return new PeerChannelAccessor(Id, peer.ConnectionId, m_Conn);
		}

		public void Broadcast(byte[] buf)
		{
			m_Conn.ChannelBroadcast(Id, buf, 0, buf.Length);
		}

		public void Broadcast(byte[] buf, int offset, int size)
		{
			m_Conn.ChannelBroadcast(Id, buf, offset, size);
		}

		public void Broadcast<T>(Action<Stream, T> write, in T obj)
		{
			m_Conn.ChannelBroadcast(Id, write, obj);
		}

		public static implicit operator short(ChannelAccessor channel)
		{
			return channel.Id;
		}


	}
}