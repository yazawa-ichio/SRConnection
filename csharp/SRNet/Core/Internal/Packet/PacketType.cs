namespace SRNet.Packet
{
	internal enum PacketType : byte
	{
		None,
		ClientHello = 1,
		ServerHello = 2,
		//署名用の証明書をランタイムで交換する用
		//CertificateRequest = 3,
		//ServerCertificate = 4,
		HandshakeRequest = 5,
		HandshakeAccept = 6,
		PeerToPeerHello = 10,
		PeerToPeerRequest = 11,
		PeerToPeerAccept = 12,
		PeerToPeerList = 13,

		Disconnect = 100,
		Ping = 101,
		Pong = 102,

		PlainMessage = 150,
		EncryptMessage = 151,

		Discovery = 200,
		DiscoveryResponse = 201,
		DiscoveryHolePunch = 202,

	}
}