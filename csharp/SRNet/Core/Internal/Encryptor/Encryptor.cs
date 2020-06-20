using System;
using System.Security.Cryptography;
using HMAC = SRNet.Crypto.HMAC;
using HMACSHA256 = SRNet.Crypto.HMACSHA256;

namespace SRNet
{

	internal class Encryptor : IDisposable
	{
		readonly Aes m_Aes;
		readonly HMAC m_EncryptHMAC;
		readonly HMAC m_DecryptHMAC;
		readonly byte[] m_DecryptHMACBuf;
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
			m_EncryptHMAC = new HMACSHA256(hmackey);
			m_DecryptHMAC = new HMACSHA256(hmackey);

			m_Aes.Mode = CipherMode.ECB;
			//アロケーションを避けるため自前でPKCS7を実装する
			m_Aes.Padding = PaddingMode.Zeros;
			//m_Aes.Padding = PaddingMode.PKCS7;
			m_Encryptor = m_Aes.CreateEncryptor();
			m_Decryptor = m_Aes.CreateDecryptor();
			m_BlockSize = m_Aes.BlockSize / 8;
			m_HashLength = m_EncryptHMAC.HashSize / 8;
			m_DecryptHMACBuf = new byte[m_HashLength];
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

			m_EncryptHMAC.ComputeHash(buf, 0, totalSize, buf, totalSize);

			totalSize += m_HashLength;
		}

		public bool TryDecrypt(byte[] buf, int start, ref int totalSize)
		{
			totalSize -= m_HashLength;

			if (totalSize < 0) return false;

			m_DecryptHMAC.ComputeHash(buf, 0, totalSize, m_DecryptHMACBuf, 0);

			for (int i = 0; i < m_DecryptHMACBuf.Length; i++)
			{
				if (m_DecryptHMACBuf[i] != buf[totalSize + i])
				{
					return false;
				}
			}

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

	}

}