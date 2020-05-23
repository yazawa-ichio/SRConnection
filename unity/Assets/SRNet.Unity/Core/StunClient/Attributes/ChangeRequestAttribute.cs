namespace SRNet.Stun
{
	public class ChangeRequestAttribute : StunAttribute
	{
		static readonly byte[] s_Buffer = new byte[4];

		public bool ChangeIP { get; set; }

		public bool ChangePort { get; set; }

		public ChangeRequestAttribute() : base(AttributeType.ChangeRequest) { }

		public override int GetLength()
		{
			return 4;
		}

		protected override void WriteBody(byte[] buf, ref int offset)
		{
			int flag = 0;
			if (ChangeIP) flag |= 4;
			if (ChangePort) flag |= 2;
			s_Buffer[3] = (byte)flag;
			NetBinaryUtil.Write(s_Buffer, buf, ref offset);
		}

		protected override void ReadBody(byte[] buf, int offset, int size)
		{
			offset += 3;
			int flag = buf[offset++];
			ChangeIP = ((flag & 4) != 0);
			ChangePort = ((flag & 2) != 0);
		}

	}

}