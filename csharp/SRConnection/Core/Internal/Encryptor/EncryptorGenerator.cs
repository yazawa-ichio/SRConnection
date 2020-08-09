using SRConnection.Crypto;
using System;

namespace SRConnection
{
	internal class EncryptorGenerator : IDisposable
	{
		static readonly byte[] s_Info = System.Text.Encoding.UTF8.GetBytes("SRConnectionEncryptorKeyInfo");

		static int HmacKeySize = 32;
		static int AesKeySize = 32;

		HKDF m_HKDF;
		byte[] m_InputKey = new byte[32];
		byte[] m_OutputKey = new byte[HmacKeySize + AesKeySize];

		public EncryptorGenerator()
		{
			m_HKDF = new HKDF(new HMACSHA256());
		}

		public Encryptor Generate(in EncryptorKey key)
		{
			int size = EncryptorKey.GetInputKey(in key, ref m_InputKey);
			var ikm = new ArraySegment<byte>(m_InputKey, 0, size);
			m_HKDF.DeriveKey(key.Cookie, ikm, s_Info, m_OutputKey);

			byte[] aesKey = new byte[AesKeySize];
			Buffer.BlockCopy(m_OutputKey, 0, aesKey, 0, AesKeySize);

			byte[] hmacKey = new byte[HmacKeySize];
			Buffer.BlockCopy(m_OutputKey, AesKeySize, hmacKey, 0, HmacKeySize);

			return new Encryptor(aesKey, hmacKey);
		}

		public void Dispose()
		{
			m_HKDF?.Dispose();
			m_HKDF = null;
		}
	}

}