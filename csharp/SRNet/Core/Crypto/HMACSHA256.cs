namespace SRNet.Crypto
{
	public class HMACSHA256 : HMAC
	{
		public HMACSHA256(byte[] key) : base(new SHA256(), key)
		{
		}
		public HMACSHA256() : base(new SHA256())
		{
		}
	}
}