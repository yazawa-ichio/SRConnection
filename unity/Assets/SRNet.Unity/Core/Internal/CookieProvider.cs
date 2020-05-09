using System;

namespace SRNet
{
	using Packet;

	public class CookieProvider : IDisposable
	{
		public const int CookieSize = 32;

		byte[] m_Cookie = new byte[CookieSize];
		byte[] m_PrevCookie = new byte[CookieSize];
		RandomProvider m_Random = new RandomProvider();

		public byte[] Cookie => m_Cookie;

		public CookieProvider()
		{
			m_Random.GenBytes(m_PrevCookie);
			m_Random.GenBytes(m_Cookie);
		}

		public byte[] CreatePacket()
		{
			return CreatePacket(Protocol.MajorVersion, Protocol.MinorVersion);
		}

		public byte[] CreatePacket(byte major, byte minor)
		{
			Update();
			return new ServerHello(major, minor, m_Cookie).Pack();
		}

		public bool Check(byte[] buf, int offset, out byte[] hitCookie)
		{
			hitCookie = null;
			bool currentHit = true;
			bool prevHit = true;
			for (int i = 0; i < CookieSize; i++)
			{
				byte b = buf[i + offset];
				if (m_Cookie[i] != b)
				{
					currentHit = false;
				}
				if (m_PrevCookie[i] != b)
				{
					prevHit = false;
				}
				if (!currentHit && !prevHit)
				{
					hitCookie = null;
					return false;
				}
			}
			if (currentHit)
			{
				hitCookie = m_Cookie;
			}
			if (prevHit)
			{
				hitCookie = m_PrevCookie;
			}
			return true;
		}

		public void Dispose()
		{
			m_Random?.Dispose();
			m_Random = null;
		}

		public void Update()
		{
			var tmp = m_Cookie;
			m_Cookie = m_PrevCookie;
			m_PrevCookie = tmp;
			m_Random.GenBytes(m_Cookie);
		}

	}
}