using System;
using System.IO;

namespace SRNet.Channel
{
	public readonly struct ChannelAccessor
	{
		public readonly short Id;
		readonly ChannelContext m_Context;

		internal ChannelAccessor(short channel, ChannelContext context)
		{
			Id = channel;
			m_Context = context;
		}

		public PeerChannelAccessor Target(int connectionId)
		{
			return new PeerChannelAccessor(Id, connectionId, m_Context);
		}

		public PeerChannelAccessor Target(Peer peer)
		{
			return new PeerChannelAccessor(Id, peer.ConnectionId, m_Context);
		}

		public void Broadcast(byte[] buf)
		{
			m_Context.Broadcast(Id, buf, 0, buf.Length);
		}

		public void Broadcast(byte[] buf, int offset, int size)
		{
			m_Context.Broadcast(Id, buf, offset, size);
		}

		public void Broadcast<T>(Action<Stream, T> write, in T obj)
		{
			m_Context.Broadcast(Id, write, obj);
		}

		public static implicit operator short(ChannelAccessor channel)
		{
			return channel.Id;
		}


	}
}