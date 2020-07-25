using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using static SRNet.Channel.FragmentUtil;

namespace SRNet.Channel
{
	public interface IChannelContext
	{
		byte[] SharedSendBuffer { get; }
		bool Send(int connectionId, byte[] buf, int offset, int size, bool encrypt);
	}

	internal class ChannelContext : IChannelContext, IDisposable
	{

		bool m_InitRead;
		ConnectionImpl m_Impl;
		ConcurrentDictionary<int, Peer> m_Peers;
		ConcurrentDictionary<short, IChannel> m_Channels = new ConcurrentDictionary<short, IChannel>();
		List<Fragment> m_FragmentList = new List<Fragment>();
		MessageReader m_Reader = new MessageReader();
		MessageWriter m_Writer = new MessageWriter();
		int m_FragmentSize = Fragment.Size;
		short m_FragmentId = 1;
		byte[] m_ReceiveBuffer = new byte[Fragment.Size + 100];
		Queue<Message> m_BufferingMessage = new Queue<Message>();
		TimeSpan m_AutoReadTimer = TimeSpan.Zero;

		public TimeSpan AutoReadTime = TimeSpan.FromMilliseconds(200);

		byte[] IChannelContext.SharedSendBuffer { get; } = new byte[Fragment.Size + 100];

		public ChannelContext(ConnectionImpl impl, ConcurrentDictionary<int, Peer> peers)
		{
			m_Impl = impl;
			m_Peers = peers;
		}

		public void Bind(short id, IConfig config)
		{
			var channel = config.Create();
			channel.Init(id, this);
			m_Channels.TryAdd(id, channel);
		}

		public void Unbind(short id)
		{
			if (m_Channels.TryRemove(id, out var channel))
			{
				channel.Dispose();
			}
		}

		public bool Contains(short id)
		{
			return m_Channels.ContainsKey(id);
		}

		public IConfig GetConfig(short id)
		{
			return m_Channels[id].Config;
		}

		public void Dispose()
		{
			foreach (var channel in m_Channels.Values)
			{
				channel.Dispose();
			}
			m_Channels.Clear();
		}

		public void Update(in TimeSpan delta)
		{
			foreach (var channel in m_Channels.Values)
			{
				channel.Update(delta);
			}
			if (!m_InitRead)
			{
				m_AutoReadTimer -= delta;
				if (m_AutoReadTimer < TimeSpan.Zero)
				{
					PreReadMessage();
				}
			}
		}

		public void AddPeer(int id)
		{
			foreach (var kvp in m_Channels)
			{
				kvp.Value.AddPeer(id);
			}
		}

		public void RemovePeer(int id)
		{
			foreach (var kvp in m_Channels)
			{
				kvp.Value.RemovePeer(id);
			}
		}

		public void Send(short channel, int connectionId, byte[] buf, int offset, int size)
		{
			try
			{
				GetFragments(buf, offset, size, m_FragmentId++, m_FragmentSize, m_FragmentList);
				m_Channels[channel].Send(connectionId, m_FragmentList);
			}
			finally
			{
				m_FragmentList.Clear();
			}
		}

		public void Send<T>(short channel, int connectionId, Action<Stream, T> write, in T obj)
		{
			try
			{
				m_Writer.Set(m_FragmentId++, m_FragmentSize);
				write(m_Writer, obj);
				m_Writer.GetFragments(m_FragmentList);
				m_Channels[channel].Send(connectionId, m_FragmentList);
			}
			finally
			{
				m_FragmentList.Clear();
			}
		}

		public void Broadcast(short channel, byte[] buf, int offset, int size)
		{
			try
			{
				GetFragments(buf, offset, size, m_FragmentId++, m_FragmentSize, m_FragmentList);
				m_FragmentList.AddRef();
				var c = m_Channels[channel];
				foreach (var id in m_Peers.Keys)
				{
					c.Send(id, m_FragmentList);
				}
			}
			finally
			{
				m_FragmentList.RemoveRef(true);
			}
		}

		public void Broadcast<T>(short channel, Action<Stream, T> write, in T obj)
		{
			try
			{
				m_Writer.Set(m_FragmentId++, m_FragmentSize);
				write(m_Writer, obj);
				m_Writer.GetFragments(m_FragmentList);
				m_FragmentList.AddRef();
				var c = m_Channels[channel];
				foreach (var id in m_Peers.Keys)
				{
					c.Send(id, m_FragmentList);
				}
			}
			finally
			{
				m_FragmentList.RemoveRef(true);
			}
		}

		void ResetAutoReadTime()
		{
			m_AutoReadTimer = AutoReadTime;
		}

		bool IChannelContext.Send(int connectionId, byte[] buf, int offset, int size, bool encrypt)
		{
			return m_Impl.Send(connectionId, buf, offset, size, encrypt);
		}

		public bool TryReadMessage(out Message message)
		{
			m_InitRead = true;
			return TryReadMessageImpl(out message, false);
		}

		public void PreReadMessage()
		{
			if (!m_Impl.Disposed)
			{
				TryReadMessageImpl(out _, true);
			}
		}

		bool TryReadMessageImpl(out Message message, bool buffering)
		{
			ResetAutoReadTime();
			while (!buffering && m_BufferingMessage.Count > 0)
			{
				message = m_BufferingMessage.Dequeue();
				//切断済みのPeerのメッセージは飛ばさない
				if (!m_Peers.ContainsKey(message.Peer.ConnectionId))
				{
					continue;
				}
				return true;
			}
			int size = 0;
			int id = 0;
			while (m_Impl.TryReceiveFrom(m_ReceiveBuffer, 0, ref size, ref id))
			{
				if (TryReadImpl(out message, id, m_ReceiveBuffer, size, buffering))
				{
					if (buffering)
					{
						m_BufferingMessage.Enqueue(message);
						continue;
					}
					return true;
				}
			}
			message = default;
			return false;
		}

		bool TryReadImpl(out Message message, int id, byte[] buf, int size, bool buffering)
		{
			if (size < 3)
			{
				message = default;
				return false;
			}
			int offset = 0;
			var channelId = BinaryUtil.ReadShort(buf, ref offset);
			if (m_Channels.TryGetValue(channelId, out var channel))
			{
				offset = 0;
				channel.OnReceive(id, buf, offset, size);
				m_FragmentList.Clear();
				if (channel.TryRead(id, m_FragmentList) && m_Peers.TryGetValue(id, out var peer))
				{
					var reader = m_Reader;
					if (buffering)
					{
						reader = new MessageReader();
					}
					reader.Set(m_FragmentList, peer, channelId);
					message = new Message(reader);
					return true;
				}
			}
			message = default;
			return false;
		}

	}
}