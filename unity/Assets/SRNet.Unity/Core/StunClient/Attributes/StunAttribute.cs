namespace SRNet.Stun
{
	public abstract class StunAttribute
	{
		public const int HeaderSize = 4;

		public AttributeType Type { get; protected set; }

		public StunAttribute(AttributeType type)
		{
			Type = type;
		}

		public static int GetPadOffset(int len)
		{
			return len % 4 == 0 ? 0 : (4 - len % 4);
		}

		public int GetPaddedLength() => GetLength() + GetPadOffset(GetLength());

		public abstract int GetLength();

		public void Write(ref byte[] buf, ref int offset)
		{
			var totalSize = 4 + GetPaddedLength() + offset;
			if (buf.Length < totalSize)
			{
				System.Array.Resize(ref buf, System.Math.Max(buf.Length * 2, totalSize));
			}

			NetBinaryUtil.Write((ushort)Type, buf, ref offset);
			NetBinaryUtil.Write((ushort)GetLength(), buf, ref offset);
			WriteBody(buf, ref offset);
			offset += GetPadOffset(GetLength());
		}

		public void Read(byte[] buf, ref int offset)
		{
			Type = (AttributeType)NetBinaryUtil.ReadUShort(buf, ref offset);
			var size = NetBinaryUtil.ReadUShort(buf, ref offset);
			ReadBody(buf, offset, size);
			offset += size + GetPadOffset(size);
		}

		protected abstract void WriteBody(byte[] buf, ref int offset);

		protected abstract void ReadBody(byte[] buf, int offset, int size);

	}

}