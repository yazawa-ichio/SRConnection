using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using static SRNet.Channel.FragmentUtil;

namespace SRNet.Channel
{

	internal class ChannelManager
	{

		IChannelContext m_Context;
		ConcurrentDictionary<short, IChannel> m_Channels = new ConcurrentDictionary<short, IChannel>();
		List<int> m_Peers = new List<int>();

		List<Fragment> m_FragmentList = new List<Fragment>();
		MessageReader m_Reader = new MessageReader();
		MessageWriter m_Writer = new MessageWriter();
		int m_FragmentSize = Fragment.Size;
		short m_FragmentId = 1;


		public ChannelManager(ConnectionImpl impl) : this(new ChannelContext(impl))
		{
		}

		public ChannelManager(IChannelContext ctx)
		{
			m_Context = ctx;
		}

		public void Bind(short id, IConfig config)
		{
			var channel = config.Create();
			channel.Init(id, m_Context);
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

		public void OnUpdateStatus(in TimeSpan delta)
		{
			foreach (var channel in m_Channels.Values)
			{
				channel.Update(delta);
			}
		}

		public void AddPeer(int id)
		{
			m_Peers.Add(id);
			foreach (var kvp in m_Channels)
			{
				kvp.Value.AddPeer(id);
			}
		}

		public void RemovePeer(int id)
		{
			m_Peers.Remove(id);
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
				foreach (var id in m_Peers)
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
				foreach (var id in m_Peers)
				{
					c.Send(id, m_FragmentList);
				}
			}
			finally
			{
				m_FragmentList.RemoveRef(true);
			}
		}

		public bool TryRead(Peer peer, byte[] buf, int size, out Message message)
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
				var id = peer.ConnectionId;
				offset = 0;
				channel.OnReceive(id, buf, offset, size);
				m_FragmentList.Clear();
				if (channel.TryRead(id, m_FragmentList))
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