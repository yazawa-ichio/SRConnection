using System;

namespace SRConnection.Crypto
{
	public class SHA256 : System.Security.Cryptography.SHA256, IHashGenerator
	{
		public const int BlockSize = 64;

		static readonly uint[] s_K = {
			0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5,
			0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
			0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3,
			0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
			0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc,
			0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
			0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7,
			0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
			0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13,
			0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
			0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3,
			0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
			0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5,
			0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
			0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208,
			0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
		};


		uint[] m_W = new uint[64];
		byte[] m_Temp = new byte[BlockSize];
		int m_TempOffset;

		uint m_H1 = 0x6a09e667;
		uint m_H2 = 0xbb67ae85;
		uint m_H3 = 0x3c6ef372;
		uint m_H4 = 0xa54ff53a;
		uint m_H5 = 0x510e527f;
		uint m_H6 = 0x9b05688c;
		uint m_H7 = 0x1f83d9ab;
		uint m_H8 = 0x5be0cd19;
		ulong m_Len;

		int IHashGenerator.HashSize => 32;

		int IHashGenerator.BlockSize => BlockSize;

		public SHA256() : base()
		{
			HashSizeValue = 256;
		}

		public SHA256(SHA256 from) : base()
		{
			HashSizeValue = 256;
			CopyFrom(from);
		}

		IHashGenerator IHashGenerator.Clone()
		{
			return new SHA256(this);
		}

		void IHashGenerator.CopyFrom(IHashGenerator from)
		{
			CopyFrom(from as SHA256);
		}

		void IHashGenerator.HashCore(byte[] array, int ibStart, int cbSize)
		{
			HashCore(array, ibStart, cbSize);
		}

		void IHashGenerator.HashFinal(byte[] output, int offset)
		{
			HashFinal(output, offset);
		}

		public override void Initialize()
		{
			m_H1 = 0x6a09e667;
			m_H2 = 0xbb67ae85;
			m_H3 = 0x3c6ef372;
			m_H4 = 0xa54ff53a;
			m_H5 = 0x510e527f;
			m_H6 = 0x9b05688c;
			m_H7 = 0x1f83d9ab;
			m_H8 = 0x5be0cd19;
			Array.Clear(m_Temp, 0, m_TempOffset);
			m_TempOffset = 0;
			m_Len = 0;
		}

		public void CopyFrom(SHA256 from)
		{
			m_H1 = from.m_H1;
			m_H2 = from.m_H2;
			m_H3 = from.m_H3;
			m_H4 = from.m_H4;
			m_H5 = from.m_H5;
			m_H6 = from.m_H6;
			m_H7 = from.m_H7;
			m_H8 = from.m_H8;
			Array.Copy(from.m_Temp, 0, m_Temp, 0, m_Temp.Length);
			m_TempOffset = from.m_TempOffset;
			m_Len = from.m_Len;
			State = from.State;
		}

		public void ComputeHash(byte[] input, byte[] output)
		{
			ComputeHash(input, 0, input.Length, output, 0);
		}

		public void ComputeHash(byte[] input, int inputOffset, int size, byte[] output, int outputOffset)
		{
			HashCore(input, inputOffset, size);
			HashFinal(output, outputOffset);
			Initialize();
		}

		protected override void HashCore(byte[] array, int ibStart, int cbSize)
		{
			if (m_TempOffset != 0 && cbSize + m_TempOffset >= BlockSize)
			{
				int remain = BlockSize - m_TempOffset;
				Buffer.BlockCopy(array, ibStart, m_Temp, m_TempOffset, remain);
				ProcessBlock(m_Temp, 0);
				m_TempOffset = 0;
				ibStart += remain;
				cbSize -= remain;

			}

			int offset = 0;

			while (cbSize - offset >= BlockSize)
			{
				ProcessBlock(array, ibStart + offset);
				offset += BlockSize;
			}

			if (cbSize - offset > 0)
			{
				var remain = cbSize - offset;
				Buffer.BlockCopy(array, ibStart + offset, m_Temp, m_TempOffset, remain);
				m_TempOffset += remain;
			}

		}

		protected override byte[] HashFinal()
		{
			byte[] hash = new byte[32];
			HashFinal(hash, 0);
			return hash;
		}

		void HashFinal(byte[] output, int offset)
		{
			ProcessFinalBlock();
			Pack.Write(m_H1, output, ref offset);
			Pack.Write(m_H2, output, ref offset);
			Pack.Write(m_H3, output, ref offset);
			Pack.Write(m_H4, output, ref offset);
			Pack.Write(m_H5, output, ref offset);
			Pack.Write(m_H6, output, ref offset);
			Pack.Write(m_H7, output, ref offset);
			Pack.Write(m_H8, output, ref offset);
		}

		void ProcessBlock(byte[] array, int offset)
		{
			m_Len += BlockSize;

			var w = m_W;
			for (int i = 0; i < 16; i++)
			{
				w[i] = Pack.ReadUInt(array, ref offset);
			}

			for (int i = 16; i < 64; i++)
			{
				var t1 = w[i - 15];
				t1 = (((t1 >> 7) | (t1 << 25)) ^ ((t1 >> 18) | (t1 << 14)) ^ (t1 >> 3));

				var t2 = w[i - 2];
				t2 = (((t2 >> 17) | (t2 << 15)) ^ ((t2 >> 19) | (t2 << 13)) ^ (t2 >> 10));
				w[i] = t2 + w[i - 7] + t1 + w[i - 16];
			}

			var a = m_H1;
			var b = m_H2;
			var c = m_H3;
			var d = m_H4;
			var e = m_H5;
			var f = m_H6;
			var g = m_H7;
			var h = m_H8;

			for (int i = 0; i < 64; i++)
			{
				var t1 = h + (((e >> 6) | (e << 26)) ^ ((e >> 11) | (e << 21)) ^ ((e >> 25) | (e << 7))) + ((e & f) ^ (~e & g)) + s_K[i] + w[i];
				var t2 = (((a >> 2) | (a << 30)) ^ ((a >> 13) | (a << 19)) ^ ((a >> 22) | (a << 10))) + ((a & b) ^ (a & c) ^ (b & c));
				h = g;
				g = f;
				f = e;
				e = d + t1;
				d = c;
				c = b;
				b = a;
				a = t1 + t2;
			}

			m_H1 += a;
			m_H2 += b;
			m_H3 += c;
			m_H4 += d;
			m_H5 += e;
			m_H6 += f;
			m_H7 += g;
			m_H8 += h;
		}


		void ProcessFinalBlock()
		{
			var len = (m_Len + (ulong)m_TempOffset) << 3;
			var offset = m_TempOffset;
			m_Temp[m_TempOffset++] = 0x80;
			if (offset < 56)
			{
				Array.Clear(m_Temp, m_TempOffset, BlockSize - m_TempOffset - 8);
			}
			else
			{
				Array.Clear(m_Temp, m_TempOffset, BlockSize - m_TempOffset);
				ProcessBlock(m_Temp, 0);
				Array.Clear(m_Temp, 0, BlockSize);
			}
			m_TempOffset = BlockSize - 8;
			Pack.Write(len, m_Temp, ref m_TempOffset);
			ProcessBlock(m_Temp, 0);
			m_TempOffset = 0;
		}



	}
}