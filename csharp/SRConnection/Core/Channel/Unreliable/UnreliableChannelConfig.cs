namespace SRConnection.Channel
{
	public class UnreliableChannelConfig : IConfig
	{
		public int MaxBufferSize = 64;

		public bool Encrypt = true;

		public bool Ordered = false;

		public IChannel Create()
		{
			return new UnreliableChannel(this);
		}
	}
}