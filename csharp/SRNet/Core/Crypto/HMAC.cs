using System;
using System.Security.Cryptography;

namespace SRNet.Crypto
{

	public abstract class HMAC : KeyedHashAlgorithm
	{

		IHashGenerator m_Hash;
		int m_HashSize;
		int m_BlockSize;
		byte[] m_InputPad;
		byte[] m_OutputPad;
		IHashGenerator m_InputState;
		IHashGenerator m_OutputState;
		bool m_Final;

		public override byte[] Key
		{
			get => KeyValue;
			set => SetKey(value);
		}

		public HMAC(IHashGenerator hash) : this(hash, null) { }

		public HMAC(IHashGenerator hash, byte[] key)
		{
			m_Hash = hash;
			m_HashSize = hash.HashSize;
			m_BlockSize = hash.BlockSize;
			m_InputPad = new byte[m_BlockSize];
			m_OutputPad = new byte[m_BlockSize];
			HashSizeValue = m_HashSize * 8;
			SetKey(key);
		}

		void SetKey(byte[] key)
		{
			m_Final = false;
			m_Hash.Initialize();

			if (key == null)
			{
				key = Random.GenBytes(m_BlockSize);
			}

			int keyLength = key.Length;
			if (keyLength > m_BlockSize)
			{
				KeyValue = new byte[m_HashSize];
				m_Hash.HashCore(key, 0, key.Length);
				m_Hash.HashFinal(KeyValue, 0);
				m_Hash.Initialize();
				keyLength = m_HashSize;
			}
			else
			{
				KeyValue = key;
			}

			Array.Copy(KeyValue, 0, m_InputPad, 0, keyLength);
			Array.Clear(m_InputPad, keyLength, m_BlockSize - keyLength);
			Array.Copy(m_InputPad, 0, m_OutputPad, 0, m_BlockSize);

			Xor(m_InputPad, 0x36);
			Xor(m_OutputPad, 0x5C);

			if (m_OutputState == null)
			{
				m_OutputState = m_Hash.Clone();
			}
			else
			{
				m_OutputState.Initialize();
			}
			m_OutputState.HashCore(m_OutputPad, 0, m_OutputPad.Length);

			m_Hash.HashCore(m_InputPad, 0, m_InputPad.Length);
			if (m_InputState == null)
			{
				m_InputState = m_Hash.Clone();
			}
			else
			{
				m_InputState.CopyFrom(m_Hash);
			}
		}

		public override void Initialize()
		{
			m_Hash.CopyFrom(m_InputState);
			m_Final = false;
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
			if (m_Final)
			{
				throw new InvalidOperationException();
			}
			m_Hash.HashCore(array, ibStart, cbSize);
		}

		protected override byte[] HashFinal()
		{
			byte[] hash = new byte[32];
			HashFinal(hash, 0);
			return hash;
		}

		void HashFinal(byte[] output, int offset)
		{
			if (m_Final)
			{
				throw new InvalidOperationException();
			}

			m_Hash.HashFinal(output, offset);

			m_Hash.CopyFrom(m_OutputState);

			m_Hash.HashCore(output, offset, m_HashSize);

			m_Hash.HashFinal(output, offset);

			m_Final = true;

		}

		static void Xor(byte[] buf, byte x)
		{
			for (int i = 0; i < buf.Length; i++)
			{
				buf[i] ^= x;
			}
		}

	}
}