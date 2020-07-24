using SRNet.Channel;
using System;

namespace SRNet
{
	public static class DefaultChannel
	{
		public const short Reliable = 1;
		public const short Unreliable = 2;
	}

	public class ChannelMapAccessor
	{

		ChannelContext m_Context;

		public ChannelAccessor Reliable => Get(DefaultChannel.Reliable);

		public ChannelAccessor Unreliable => Get(DefaultChannel.Unreliable);


		public TimeSpan AutoReadTime
		{
			get => m_Context.AutoReadTime;
			set => m_Context.AutoReadTime = value;
		}

		internal ChannelMapAccessor(ChannelContext context)
		{
			m_Context = context;
			m_Context.Bind(DefaultChannel.Reliable, new ReliableChannelConfig());
			m_Context.Bind(DefaultChannel.Unreliable, new UnreliableChannelConfig());
		}

		public T Bind<T>(short id, Action<T> action = null) where T : IConfig, new()
		{
			T config = new T();
			action?.Invoke(config);
			Bind(id, config);
			return config;
		}

		public void Bind(short id, IConfig config)
		{
			if (id <= 100) throw new ArgumentException("user channel is greater than 100", nameof(id));
			m_Context.Bind(id, config);
		}

		public void Unbind(short id)
		{
			if (id <= 100) throw new ArgumentException("user channel is greater than 100", nameof(id));
			m_Context.Unbind(id);
		}

		public IConfig GetConfig(short id) => m_Context.GetConfig(id);

		public ChannelAccessor Get(short id)
		{
			if (!m_Context.Contains(id)) throw new System.Collections.Generic.KeyNotFoundException(string.Format("not found channel {0}", id));
			return new ChannelAccessor(id, m_Context);
		}

		public ChannelAccessor this[short id]
		{
			get => Get(id);
		}


	}

}