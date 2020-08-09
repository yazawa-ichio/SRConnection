namespace SRConnection.Unity.Tests
{

	public class TestEchoServer : ServerBase
	{
		public int AddCount;
		public int RemoveCount;

		public bool EchoBroadcast { get; set; }

		protected override void OnAddPeer(Peer peer) { AddCount++; }

		protected override void OnRemovePeer(Peer peer) { RemoveCount++; }

		protected override void OnMessage(Message message)
		{
			if (!EchoBroadcast)
			{
				//そのまま返す
				message.ResponseTo(message);
			}
			else
			{
				//送信されたチャンネルにブロードキャスト
				message.Channel.Broadcast(message);
			}
		}

	}

}