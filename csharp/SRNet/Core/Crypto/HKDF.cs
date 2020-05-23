using System;
using System.Security.Cryptography;

namespace SRNet.Crypto
{

	public class HKDF : IDisposable
	{
		HMAC m_HMAC;
		int m_Length;
		byte[] m_T;
		byte[] m_EmptySalt;

		public HKDF(HMAC hmac)
		{
			m_HMAC = hmac;
			m_Length = hmac.HashSize / 8;
		}

		public void DeriveKey(byte[] salt, byte[] ikm, byte[] info, byte[] okm)
		{
			DeriveKey(salt, new ArraySegment<byte>(ikm), info, okm);
		}

		public void DeriveKey(byte[] salt, ArraySegment<byte> ikm, byte[] info, byte[] okm)
		{
			var prk = Extract(salt, ikm);
			Expand(prk, info, okm);
		}

		byte[] Extract(byte[] salt, ArraySegment<byte> ikm)
		{
			if (salt != null && salt.Length != m_Length)
			{
				Array.Resize(ref salt, m_Length);
			}
			m_HMAC.Key = salt ?? m_EmptySalt ?? (m_EmptySalt = new byte[m_Length]);
			return m_HMAC.ComputeHash(ikm.Array, ikm.Offset, ikm.Count);
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
			byte[] prev = Array.Empty<byte>();
			while (offset < length)
			{
				if (prev.Length > 0)
				{
					Buffer.BlockCopy(prev, 0, m_T, 0, prev.Length);
				}
				Buffer.BlockCopy(info, 0, m_T, prev.Length, info.Length);
				m_T[prev.Length + info.Length] = counter++;
				prev = m_HMAC.ComputeHash(m_T, 0, prev.Length + info.Length + 1);
				if (offset + m_Length > length)
				{
					Buffer.BlockCopy(prev, 0, okm, offset, length - offset);
				}
				else
				{
					Buffer.BlockCopy(prev, 0, okm, offset, m_Length);
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