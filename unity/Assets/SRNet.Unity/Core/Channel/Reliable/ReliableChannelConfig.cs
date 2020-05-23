namespace SRNet.Channel
{
	public class ReliableChannelConfig : IConfig
	{
		public int MaxWindowSize = 32;

		public bool Encrypt = true;

		public IChannel Create()
		{
			return new ReliableChannel(this);
		}
	}
}