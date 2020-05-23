using System.Net;
using System.Net.Sockets;

namespace SRNet.Stun
{
	public class XorAddressAttribute : AddressAttribute
	{
		static readonly byte[] s_Buffer = new byte[16];

		public XorAddressAttribute(AttributeType type) : base(type)
		{
		}

		public override int GetLength()
		{
			return sizeof(byte) + sizeof(byte) + sizeof(ushort) + m_AddressBytes.Length;
		}

		protected override void ReadBody(byte[] buf, int offset, int size)
		{
			offset++;
			var family = buf[offset++];
			var port = (int)(NetBinaryUtil.ReadUShort(buf, ref offset) ^ (ushort)(StunMessage.MagicCookie >> 16));
			byte[] address = NetBinaryUtil.ReadBytes(buf, family == 1 ? 4 : 16, ref offset);
			for (int i = 0; i < address.Length; i++)
			{
				address[i] = (byte)(address[i] ^ buf[i + 4]);
			}
			EndPoint = new IPEndPoint(new IPAddress(address), port);
		}

		protected override void WriteBody(byte[] buf, ref int offset)
		{
			buf[offset++] = 0;
			buf[offset++] = (byte)(m_EndPoint.AddressFamily == AddressFamily.InterNetwork ? 1 : 2);
			var port = (ushort)((ushort)m_EndPoint.Port ^ (ushort)(StunMessage.MagicCookie >> 16));
			NetBinaryUtil.Write(port, buf, ref offset);
			for (int i = 0; i < m_AddressBytes.Length; i++)
			{
				s_Buffer[i] = (byte)(m_AddressBytes[i] ^ buf[i + 4]);
			}
			NetBinaryUtil.Write(s_Buffer, m_AddressBytes.Length, buf, ref offset);
		}
	}

}