namespace SRNet.Packet
{
	internal interface IEncryptPacket
	{
		int Pack(int id, byte[] buf, Encryptor encryptor);
		int Pack(byte[] buf, Encryptor encryptor);
	}
}