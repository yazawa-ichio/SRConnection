namespace SRNet.Channel
{
	public interface IChannelContext
	{
		byte[] SharedSendBuffer { get; }
		bool Send(int connectionId, byte[] buf, int offset, int size, bool encrypt);
	}

}