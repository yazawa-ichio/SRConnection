namespace SRConnection.Channel
{
	internal class ChannelContext : IChannelContext
	{
		ConnectionImpl m_Impl;

		public ChannelContext(ConnectionImpl impl)
		{
			m_Impl = impl;
		}

		public byte[] SharedSendBuffer { get; } = new byte[Fragment.Size + 100];

		public bool Send(int connectionId, byte[] buf, int offset, int size, bool encrypt)
		{
			return m_Impl.Send(connectionId, buf, offset, size, encrypt);
		}

		public void DisconnectError(short channelId, int connectionId, string reason)
		{
			//TODO:Reasonをアプリレイヤーに引き渡す
			m_Impl.SendDisconnect(connectionId);
		}

	}

}