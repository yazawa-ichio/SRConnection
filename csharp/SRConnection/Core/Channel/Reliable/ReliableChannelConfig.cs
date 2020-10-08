using System;

namespace SRConnection.Channel
{
	public class ReliableChannelConfig : IConfig
	{
		public int MaxWindowSize = 32;

		public bool Encrypt = true;

		public bool Ordered = true;

		public TimeSpan Timeout = TimeSpan.FromSeconds(10);

		public IChannel Create()
		{
			return new ReliableChannel(this);
		}
	}
}