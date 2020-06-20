using System;

namespace SRNet.Crypto
{
	public class HKDF : IDisposable
	{
		HMAC m_HMAC;
		int m_Length;
		byte[] m_T;
		byte[] m_EmptySalt;
		byte[] m_Prk;
		byte[] m_Temp;

		public HKDF(HMAC hmac)
		{
			m_HMAC = hmac;
			m_Length = hmac.HashSize / 8;
			m_Prk = new byte[m_Length];
			m_Temp = new byte[m_Length];
		}

		public void DeriveKey(byte[] salt, byte[] ikm, byte[] info, byte[] okm)
		{
			DeriveKey(salt, new ArraySegment<byte>(ikm), info, okm);
		}

		public void DeriveKey(byte[] salt, ArraySegment<byte> ikm, byte[] info, byte[] okm)
		{
			Extract(salt, ikm, m_Prk);
			Expand(m_Prk, info, okm);
		}

		void Extract(byte[] salt, ArraySegment<byte> ikm, byte[] prk)
		{
			if (salt != null && salt.Length != m_Length)
			{
				Array.Resize(ref salt, m_Length);
			}
			m_HMAC.Key = salt ?? m_EmptySalt ?? (m_EmptySalt = new byte[m_Length]);
			m_HMAC.ComputeHash(ikm.Array, ikm.Offset, ikm.Count, prk, 0);
		}

		void Expand(byte[] prk, byte[] info, byte[] okm)
		{
			if (info == null) info = new byte[0];

			m_HMAC.Key = prk;

			var length = okm.Length;

			if (m_T == null || m_T.Length != m_Length + info.Length + sizeof(byte))
				m_T = new byte[m_Length + info.Length + sizeof(byte)];

			int offset = 0;
			byte counter = 1;
			while (offset < length)
			{
				if (offset == 0)
				{
					Buffer.BlockCopy(info, 0, m_T, 0, info.Length);
					m_T[info.Length] = counter++;
					m_HMAC.ComputeHash(m_T, 0, info.Length + 1, m_Temp, 0);
				}
				else
				{
					Buffer.BlockCopy(m_Temp, 0, m_T, 0, m_Temp.Length);
					Buffer.BlockCopy(info, 0, m_T, m_Temp.Length, info.Length);
					m_T[m_Temp.Length + info.Length] = counter++;
					m_HMAC.ComputeHash(m_T, 0, m_Temp.Length + info.Length + 1, m_Temp, 0);
				}
				if (offset + m_Length > length)
				{
					Buffer.BlockCopy(m_Temp, 0, okm, offset, length - offset);
				}
				else
				{
					Buffer.BlockCopy(m_Temp, 0, okm, offset, m_Length);
				}
				offset += m_Length;
			}
		}

		public void Dispose()
		{
			m_HMAC?.Dispose();
		}
	}

}