using System;
using System.IO;

namespace SRNet.Channel
{
	public readonly struct PeerChannelAccessor
	{
		public readonly short Channel;
		readonly int m_ConnectionId;
		readonly ChannelContext m_Context;

		public ChannelAccessor Parent => new ChannelAccessor(Channel, m_Context);

		internal PeerChannelAccessor(short channel, int connectionId, ChannelContext context)
		{
			Channel = channel;
			m_ConnectionId = connectionId;
			m_Context = context;
		}

		public void Send(byte[] buf, int offset, int size)
		{
			m_Context.Send(Channel, m_ConnectionId, buf, offset, size);
		}

		public void Send(byte[] buf)
		{
			m_Context.Send(Channel, m_ConnectionId, buf, 0, buf.Length);
		}

		public void Send<T>(Action<Stream, T> write, in T obj)
		{
			m_Context.Send(Channel, m_ConnectionId, write, obj);
		}

		public static implicit operator short(PeerChannelAccessor channel)
		{
			return channel.Channel;
		}

	}
}