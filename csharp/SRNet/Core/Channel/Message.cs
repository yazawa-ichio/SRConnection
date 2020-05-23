using System;
using System.IO;
namespace SRNet
{
	using Channel;

	public readonly struct Message
	{
		public Peer Peer
		{
			get
			{
				CheckRevision();
				return m_Reader.Peer;
			}
		}

		public short Channel
		{
			get
			{
				CheckRevision();
				return m_Reader.Channel;
			}
		}

		public Stream Stream
		{
			get
			{
				CheckRevision();
				return m_Reader.CreateCopy();
			}
		}

		public Stream TempStream
		{
			get
			{
				CheckRevision();
				return m_Reader;
			}
		}

		readonly byte m_Revision;
		readonly MessageReader m_Reader;

		internal Message(MessageReader reader)
		{
			m_Revision = reader.m_Revision;
			m_Reader = reader;
		}

		void CheckRevision()
		{
			if (m_Revision != m_Reader.m_Revision)
			{
				throw new Exception("fail check revision");
			}
		}

		public byte[] ToArray()
		{
			CheckRevision();
			return m_Reader.ToArray();
		}

		public Message Copy()
		{
			return new Message(m_Reader.CreateCopy());
		}

		public void CopyTo(Stream stream)
		{
			var fragment = FragmentPool.Get();
			try
			{
				int read;
				while ((read = m_Reader.Read(fragment.Data, 0, fragment.Data.Length)) > 0)
				{
					stream.Write(fragment.Data, 0, read);
				}
			}
			finally
			{
				fragment.TryReturn();
			}
		}

		public static implicit operator Stream(Message message)
		{
			return message.Stream;
		}

		public static implicit operator Peer(Message message)
		{
			return message.Peer;
		}

		public static implicit operator byte[](Message message)
		{
			return message.ToArray();
		}


	}

}