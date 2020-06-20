namespace SRNet.Crypto
{
	public interface IHashGenerator
	{
		int HashSize { get; }

		int BlockSize { get; }

		void Initialize();

		IHashGenerator Clone();

		void CopyFrom(IHashGenerator from);

		void HashCore(byte[] array, int ibStart, int cbSize);

		void HashFinal(byte[] output, int offset);
	}
}