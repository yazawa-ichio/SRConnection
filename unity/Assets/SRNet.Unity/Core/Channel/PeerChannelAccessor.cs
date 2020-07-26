using System;
using System.IO;

namespace SRNet.Channel
{
	public readonly struct PeerChannelAccessor
	{
		public readonly short Channel;
		readonly int m_ConnectionId;
		readonly Connection m_Connection;

		public ChannelAccessor Parent => new ChannelAccessor(Channel, m_Connection);

		internal PeerChannelAccessor(short channel, int connectionId, Connection conn)
		{
			Channel = channel;
			m_ConnectionId = connectionId;
			m_Connection = conn;
		}

		public void Send(byte[] buf, int offset, int size)
		{
			m_Connection.ChannelSend(Channel, m_ConnectionId, buf, offset, size);
		}

		public void Send(byte[] buf)
		{
			m_Connection.ChannelSend(Channel, m_ConnectionId, buf, 0, buf.Length);
		}

		public void Send<T>(Action<Stream, T> write, in T obj)
		{
			m_Connection.ChannelSend(Channel, m_ConnectionId, write, obj);
		}

		public static implicit operator short(PeerChannelAccessor channel)
		{
			return channel.Channel;
		}

	}
}