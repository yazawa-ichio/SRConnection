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

		ConnectionImpl m_Impl;
		ConcurrentDictionary<int, Peer> m_Peers;
		Dictionary<short, IChannel> m_Channels = new Dictionary<short, IChannel>();
		List<Fragment> m_FragmentList = new List<Fragment>();
		MessageReader m_Reader = new MessageReader();
		MessageWriter m_Writer = new MessageWriter();
		int m_FragmentSize = Fragment.Size;
		short m_FragmentId = 1;
		byte[] m_ReceiveBuffer = new byte[Fragment.Size + 100];

		byte[] IChannelContext.SharedSendBuffer { get; } = new byte[Fragment.Size + 100];

		public ChannelContext(ConnectionImpl impl, ConcurrentDictionary<int, Peer> peers)
		{
			m_Impl = impl;
			m_Peers = peers;
		}

		public void Bind(short id, IConfig config)
		{
			lock (m_Channels)
			{
				var channel = config.Create();
				channel.Init(id, this);
				m_Channels.Add(id, channel);
			}
		}

		public void Unbind(short id)
		{
			lock (m_Channels)
			{
				if (m_Channels.TryGetValue(id, out var channel))
				{
					m_Channels.Remove(id);
					channel.Dispose();
				}
			}
		}

		public void Dispose()
		{
			lock (m_Channels)
			{
				foreach (var channel in m_Channels.Values)
				{
					channel.Dispose();
				}
				m_Channels.Clear();
			}
		}

		public void Update(in TimeSpan delta)
		{
			lock (m_Channels)
			{
				foreach (var channel in m_Channels.Values)
				{
					channel.Update(delta);
				}
			}
		}

		public void AddPeer(int id)
		{
		}

		public void RemovePeer(int id)
		{
			lock (m_Channels)
			{
				foreach (var kvp in m_Channels)
				{
					kvp.Value.RemovePeer(id);
				}
			}
		}

		public void Send(short channel, int connectionId, byte[] buf, int offset, int size)
		{
			lock (m_Channels)
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
		}

		public void Send<T>(short channel, int connectionId, Action<Stream, T> write, in T obj)
		{
			lock (m_Channels)
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
		}

		public bool TryRead(out Message message)
		{
			lock (m_ReceiveBuffer)
			{
				int size = 0;
				int id = 0;
				while (m_Impl.TryReceiveFrom(m_ReceiveBuffer, 0, ref size, ref id))
				{
					if (TryReadImpl(out message, id, m_ReceiveBuffer, size))
					{
						return true;
					}
				}
			}
			message = default;
			return false;
		}

		public bool TryPollRead(out Message message, int microSeconds, bool retryNoMessageIfReceive = true)
		{
			lock (m_ReceiveBuffer)
			{
				int size = 0;
				int id = 0;
				while (m_Impl.TryPollReceiveFrom(m_ReceiveBuffer, 0, ref size, ref id, microSeconds))
				{
					if (TryReadImpl(out message, id, m_ReceiveBuffer, size))
					{
						return true;
					}
					if (!retryNoMessageIfReceive)
					{
						break;
					}
				}
			}
			message = default;
			return false;
		}

		bool IChannelContext.Send(int connectionId, byte[] buf, int offset, int size, bool encrypt)
		{
			return m_Impl.Send(connectionId, buf, offset, size, encrypt);
		}

		bool TryReadImpl(out Message message, int id, byte[] buf, int size)
		{
			if (size < 3)
			{
				message = default;
				return false;
			}
			lock (m_Channels)
			{
				int offset = 0;
				var channelId = BinaryUtil.ReadShort(buf, ref offset);
				if (m_Channels.TryGetValue(channelId, out var channel))
				{
					offset = 0;
					channel.OnReceive(id, buf, offset, size);
					m_FragmentList.Clear();
					if (channel.TryRead(id, m_FragmentList) && m_Peers.TryGetValue(id, out var peer))
					{
						m_Reader.Set(m_FragmentList, peer, channelId);
						message = new Message(m_Reader);
						return true;
					}
				}
				message = default;
				return false;
			}
		}

	}
}