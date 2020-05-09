using System;
using System.Collections.Generic;
using System.IO;

namespace SRNet.Channel
{
	internal class MessageWriter : Stream
	{
		public override bool CanRead => false;

		public override bool CanSeek => true;

		public override bool CanWrite => true;

		public override long Length => GetLength();

		public override long Position { get => m_Pos; set => Seek(value, SeekOrigin.Begin); }

		int m_Pos;
		List<Fragment> m_Fragments = new List<Fragment>();
		int m_FragmentSize;
		short m_FragmentId;

		public void Set(short id, int size)
		{
			m_Fragments.TryReturn();
			m_FragmentId = id;
			m_FragmentSize = size;
			m_Pos = 0;
		}

		public void GetFragments(List<Fragment> output)
		{
			output.Clear();
			for (short i = 0; i < m_Fragments.Count; i++)
			{
				Fragment fragment = m_Fragments[i];
				fragment.Index = i;
				fragment.Length = (short)m_Fragments.Count;
				output.Add(fragment);
			}
			//outputが利用するのでPoolに返してはいけない
			m_Fragments.Clear();
		}

		public override void Flush()
		{
			//throw new NotImplementedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
				case SeekOrigin.Begin:
					m_Pos = 0;
					break;
				case SeekOrigin.Current:
					m_Pos += (int)offset;
					break;
				case SeekOrigin.End:
					m_Pos = GetLength() + (int)offset;
					break;
			}
			if (GetLength() < m_Pos)
			{
				SetLength(m_Pos);
			}
			return m_Pos;
		}

		public override void SetLength(long value) => SetLength((int)value);

		void SetLength(int value)
		{
			if (value == 0)
			{
				m_Fragments.TryReturn();
				m_Pos = 0;
				return;
			}
			var length = GetLength();
			if (length == value)
			{
				return;
			}
			else if (length > value)
			{
				var count = length - value;
				while (count > 0)
				{
					var fragment = m_Fragments[m_Fragments.Count - 1];
					if (fragment.DataSize < count)
					{
						count -= fragment.DataSize;
						fragment.TryReturn();
						m_Fragments.RemoveAt(m_Fragments.Count - 1);
					}
					else
					{
						fragment.DataSize -= (short)count;
						Array.Clear(fragment.Data, fragment.DataSize, count);
						count = 0;
					}
				}
				m_Pos = GetLength();
			}
			else
			{
				var count = value - length;
				while (count > 0)
				{
					var fragment = m_Fragments[m_Fragments.Count - 1];
					if (fragment.DataSize + count < m_FragmentSize)
					{
						fragment.DataSize += (short)count;
						count = 0;
					}
					else
					{
						var add = m_FragmentSize - fragment.DataSize;
						count -= add;
						fragment.DataSize = (short)m_FragmentSize;
						GetFragment(m_Fragments.Count);
					}
				}
			}
		}

		int GetLength()
		{
			int size = 0;
			foreach (var fragment in m_Fragments)
			{
				size += fragment.DataSize;
			}
			return size;
		}

		int GetIndex(int offset)
		{
			return (int)(offset / (double)m_FragmentSize);
		}

		Fragment GetFragment(int index)
		{
			while (index >= m_Fragments.Count)
			{
				var fragment = FragmentPool.Get();
				fragment.Id = m_FragmentId;
				m_Fragments.Add(fragment);
			}
			return m_Fragments[index];
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			while (count > 0)
			{
				var fragment = GetFragment(GetIndex(m_Pos));
				var fragmentPos = m_Pos % m_FragmentSize;
				var writeSize = (m_FragmentSize - fragmentPos) > count ? count : (m_FragmentSize - fragmentPos);
				Buffer.BlockCopy(buffer, offset, fragment.Data, fragmentPos, writeSize);
				if (fragment.DataSize < fragmentPos + writeSize)
				{
					fragment.DataSize = (short)(fragmentPos + writeSize);
				}
				m_Pos += writeSize;
				offset += writeSize;
				count -= writeSize;
			}
		}
	}
}
