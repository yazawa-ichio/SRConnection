using System.Net;
using System.Net.Sockets;

namespace SRNet.Stun
{
	public class AddressAttribute : StunAttribute
	{
		protected byte[] m_AddressBytes;
		protected IPEndPoint m_EndPoint;
		public IPEndPoint EndPoint
		{
			get => m_EndPoint;
			set
			{
				m_EndPoint = value;
				m_AddressBytes = m_EndPoint.Address.GetAddressBytes();
			}
		}

		public AddressAttribute(AttributeType type) : base(type) { }

		public override int GetLength()
		{
			return sizeof(byte) + sizeof(byte) + sizeof(ushort) + m_AddressBytes.Length;
		}

		protected override void ReadBody(byte[] buf, int offset, int size)
		{
			var family = buf[offset++];
			var port = (int)NetBinaryUtil.ReadUShort(buf, ref offset);
			byte[] address = NetBinaryUtil.ReadBytes(buf, family == 1 ? 4 : 16, ref offset);
			EndPoint = new IPEndPoint(new IPAddress(address), port);
		}

		protected override void WriteBody(byte[] buf, ref int offset)
		{
			buf[offset++] = 0;
			buf[offset++] = (byte)(m_EndPoint.AddressFamily == AddressFamily.InterNetwork ? 1 : 2);
			NetBinaryUtil.Write((ushort)m_EndPoint.Port, buf, ref offset);
			NetBinaryUtil.Write(m_AddressBytes, buf, ref offset);
		}
	}

}