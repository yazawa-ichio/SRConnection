using System.Text;

namespace SRNet.Stun
{
	public class ErrorCodeAttribute : StunAttribute
	{
		public byte ErrorClass;
		public byte ErrorNumber;
		public string ReasonPhrase = "";


		public ErrorCodeAttribute(AttributeType type) : base(type)
		{
		}

		public override int GetLength()
		{
			return 4 + Encoding.UTF8.GetByteCount(ReasonPhrase);
		}

		protected override void ReadBody(byte[] buf, int offset, int size)
		{
			offset += 2;
			ErrorClass = buf[offset++];
			ErrorNumber = buf[offset++];
			ReasonPhrase = NetBinaryUtil.ReadString(buf, size - 2, ref offset);
		}

		protected override void WriteBody(byte[] buf, ref int offset)
		{
			buf[offset++] = 0;
			buf[offset++] = 0;
			buf[offset++] = ErrorClass;
			buf[offset++] = ErrorNumber;
			NetBinaryUtil.Write(ReasonPhrase, buf, ref offset);
		}
	}
}