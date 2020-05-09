using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SRNet.Channel
{
	internal class MessageReader : Stream
	{
		public override bool CanRead => m_Offset < m_Size;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override long Length => m_Size;

		public override long Position { get => m_Offset; set => throw new System.NotImplementedException(); }

		int m_Offset;
		int m_Size;
		int m_FragmentOffset;
		int m_Index;
		List<Fragment> m_List = new List<Fragment>();

		internal byte m_Revision;

		public Peer Peer { get; private set; }

		public short Channel { get; private set; }

		public void Set(List<Fragment> list, Peer peer, short channel)
		{
			m_Revision++;
			Peer = peer;
			Channel = channel;
			m_Offset = 0;
			m_Index = 0;
			m_FragmentOffset = 0;
			m_List.RemoveRef(true);
			foreach (var fragment in list)
			{
				m_Size += fragment.DataSize;
				m_List.Add(fragment);
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			m_List.RemoveRef(true);
		}

		public override void Flush() { }

		public override int Read(byte[] buffer, int offset, int count)
		{
			int tmpCount = count;
			while (m_Index < m_List.Count && count > 0)
			{
				var fragment = m_List[m_Index];
				var remain = fragment.DataSize - m_FragmentOffset;
				var readSize = remain > count ? count : remain;

				System.Buffer.BlockCopy(fragment.Data, m_FragmentOffset, buffer, offset, readSize);
				m_FragmentOffset += readSize;
				m_Offset += readSize;
				offset += readSize;
				count -= readSize;

				if (fragment.DataSize == m_FragmentOffset)
				{
					m_FragmentOffset = 0;
					m_Index++;
				}
			}
			return tmpCount - count;
		}

		public byte[] ToArray()
		{
			return m_List.ToBytes();
		}

		public MessageReader CreateCopy()
		{
			MessageReader ret = new MessageReader();
			var list = m_List.ToList();
			list.AddRef();
			ret.Set(list, Peer, Channel);
			return ret;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new System.NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new System.NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new System.NotImplementedException();
		}
	}
}
