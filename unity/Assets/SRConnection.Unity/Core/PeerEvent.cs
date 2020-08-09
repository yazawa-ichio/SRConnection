namespace SRConnection
{
	public readonly struct PeerEvent
	{
		public enum Type
		{
			Add,
			Remove,
		}

		public readonly Type EventType;
		public readonly Peer Peer;

		public PeerEvent(Type type, Peer peer)
		{
			EventType = type;
			Peer = peer;
		}
	}

}