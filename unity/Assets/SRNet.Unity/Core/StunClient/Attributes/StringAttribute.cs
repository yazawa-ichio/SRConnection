using System.Text;

namespace SRNet.Stun
{
	public class StringAttribute : StunAttribute
	{
		public string Text { get; set; }

		public StringAttribute(AttributeType type) : base(type) { }

		public override int GetLength()
		{
			return Encoding.UTF8.GetByteCount(Text);
		}

		protected override void ReadBody(byte[] buf, int offset, int size)
		{
			NetBinaryUtil.ReadString(buf, size, ref offset);
		}

		protected override void WriteBody(byte[] buf, ref int offset)
		{
			NetBinaryUtil.Write(Text, buf, ref offset);
		}

	}


}