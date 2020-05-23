using System.Collections.Generic;

namespace SRNet.Stun
{

	public class StunMessage
	{
		public const int HeaderSize = 20;
		public const int TransactionIdSize = 12;

		public MessageType MessageType { get; set; }
		public ushort Length { get; set; }
		public const uint MagicCookie = 0x2112A442;
		public byte[] TransactionId { get; set; }

		public List<StunAttribute> Attributes { get; set; }

		public AddressAttribute MappedAddress => Attributes.Find(x => x.Type == AttributeType.MappedAddress || x.Type == AttributeType.XorMappedAddress) as AddressAttribute;

		public AddressAttribute ChangedAddress => Attributes.Find(x => x.Type == AttributeType.ChangedAddress) as AddressAttribute;

		public StunMessage(MessageType type) : this(type, Random.GenBytes(TransactionIdSize))
		{
		}

		public StunMessage(MessageType type, byte[] transactionId)
		{
			MessageType = type;
			TransactionId = transactionId;
			Attributes = new List<StunAttribute>();
		}

		public bool TryParse(byte[] buffer)
		{
			int offset = 0;
			MessageType = (MessageType)NetBinaryUtil.ReadShort(buffer, ref offset);
			int messageLength = (ushort)NetBinaryUtil.ReadShort(buffer, ref offset);
			if (MagicCookie != NetBinaryUtil.ReadUInt(buffer, ref offset))
			{
				return false;
			}
			TransactionId = NetBinaryUtil.ReadBytes(buffer, TransactionIdSize, ref offset);
			Attributes.Clear();
			while (offset < messageLength + 20)
			{
				var attr = GetAttribute(buffer, ref offset);
				if (attr != null)
				{
					Attributes.Add(attr);
				}
			}
			return true;
		}


		public int Write(ref byte[] buf)
		{
			int offset = 0;
			NetBinaryUtil.Write((ushort)MessageType, buf, ref offset);
			int attributesLength = 0;
			foreach (var attribute in Attributes)
			{
				attributesLength += StunAttribute.HeaderSize + attribute.GetPaddedLength();
			}
			NetBinaryUtil.Write((ushort)attributesLength, buf, ref offset);
			NetBinaryUtil.Write(MagicCookie, buf, ref offset);
			NetBinaryUtil.Write(TransactionId, TransactionIdSize, buf, ref offset);
			foreach (var attribute in Attributes)
			{
				attribute.Write(ref buf, ref offset);
			}
			return offset;
		}


		StunAttribute GetAttribute(byte[] buf, ref int offset)
		{
			int tmp = offset;
			var type = (AttributeType)NetBinaryUtil.ReadUShort(buf, ref tmp);
			switch (type)
			{
				case AttributeType.MappedAddress:
				case AttributeType.SourceAddress:
				case AttributeType.ChangedAddress:
				case AttributeType.ReflectedFrom:
				case AttributeType.ResponseAddress:
					{
						var attr = new AddressAttribute(type);
						attr.Read(buf, ref offset);
						return attr;
					}
				case AttributeType.XorMappedAddress:
					{
						var attr = new XorAddressAttribute(type);
						attr.Read(buf, ref offset);
						return attr;
					}
				case AttributeType.ErrorCode:
					{
						var attr = new ErrorCodeAttribute(type);
						attr.Read(buf, ref offset);
						return attr;
					}
				case AttributeType.ServerName:
				case AttributeType.Username:
				case AttributeType.Password:
					{
						var attr = new ErrorCodeAttribute(type);
						attr.Read(buf, ref offset);
						return attr;
					}
			}
			var len = NetBinaryUtil.ReadUShort(buf, ref tmp);
			offset += len + StunAttribute.GetPadOffset(len);
			return null;
		}

	}

}