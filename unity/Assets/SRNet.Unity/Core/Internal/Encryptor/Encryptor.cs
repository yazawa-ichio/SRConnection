using System;
using System.Security.Cryptography;

namespace SRNet
{

	internal class Encryptor : IDisposable
	{
		readonly Aes m_Aes;
		readonly HMAC m_EncryptHMAC;
		readonly HMAC m_DecryptHMAC;
		readonly ICryptoTransform m_Encryptor;
		readonly ICryptoTransform m_Decryptor;
		readonly int m_BlockSize;
		readonly int m_HashLength;

		public byte[] AesKey => m_Aes.Key;

		public int AesBlockSize => m_Aes.BlockSize;

		public byte[] HMACKey => m_EncryptHMAC.Key;

		public Encryptor(byte[] aeskey, byte[] hmackey)
		{
			m_Aes = Aes.Create();
			m_Aes.Key = aeskey;
			m_EncryptHMAC = new HMAC();
			m_EncryptHMAC.Key = hmackey;
			m_DecryptHMAC = new HMAC();
			m_DecryptHMAC.Key = m_EncryptHMAC.Key;

			m_Aes.Mode = CipherMode.ECB;
			//アロケーションを避けるため自前でPKCS7を実装する
			m_Aes.Padding = PaddingMode.Zeros;
			//m_Aes.Padding = PaddingMode.PKCS7;
			m_Encryptor = m_Aes.CreateEncryptor();
			m_Decryptor = m_Aes.CreateDecryptor();
			m_BlockSize = m_Aes.BlockSize / 8;
			m_HashLength = m_EncryptHMAC.HashLength;
		}

		public void Encrypt(byte[] buf, int start, ref int totalSize)
		{
			var padSize = m_BlockSize - ((totalSize - start) % m_BlockSize);
			for (int i = 0; i < padSize; i++)
			{
				buf[padSize - i + totalSize - 1] = (byte)padSize;
			}
			totalSize += padSize;
			m_Encryptor.TransformBlock(buf, start, totalSize - start, buf, start);

			m_EncryptHMAC.AppendHash(buf, 0, totalSize);
			totalSize += m_HashLength;
		}

		public bool TryDecrypt(byte[] buf, int start, ref int totalSize)
		{
			if (!m_DecryptHMAC.Check(buf, 0, totalSize))
			{
				return false;
			}
			totalSize -= m_HashLength;
			Decrypt(buf, start, ref totalSize);
			return true;
		}

		void Decrypt(byte[] buf, int start, ref int size)
		{
			m_Decryptor.TransformBlock(buf, start, size - start, buf, start);
			var padSize = buf[size - 1];
			size -= padSize;
		}

		public void Dispose()
		{
			m_Encryptor.Dispose();
			m_Decryptor.Dispose();
			m_Aes.Dispose();
			m_EncryptHMAC.Dispose();
		}

		class HMAC : HMACSHA256
		{
			public readonly int HashLength;

			public HMAC() : base()
			{
				HashLength = HashSize / 8;
			}

			public void AppendHash(byte[] data, int offset, int size)
			{
				HashCore(data, offset, size);
				var hash = HashFinal();
				Initialize();
				Buffer.BlockCopy(hash, 0, data, offset + size, HashLength);
			}

			public bool Check(byte[] data, int offset, int size)
			{
				HashCore(data, offset, size - HashLength);
				var hash = HashFinal();
				Initialize();
				for (int i = 0; i < HashLength; i++)
				{
					if (hash[i] != data[offset + size - HashLength + i])
					{
						return false;
					}
				}
				return true;
			}

		}
	}

}