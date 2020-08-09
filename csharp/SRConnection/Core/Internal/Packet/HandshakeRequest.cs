using System;
using System.Security.Cryptography;

namespace SRConnection.Packet
{

	internal readonly struct HandshakeRequest
	{
		public const PacketType Type = PacketType.HandshakeRequest;
		public readonly byte MajorVersion;
		public readonly byte MinorVersion;
		public readonly byte[] Cookie;
		public readonly byte[] Payload;

		public HandshakeRequest(byte[] cookie, byte[] payload)
		{
			MajorVersion = Protocol.MajorVersion;
			MinorVersion = Protocol.MinorVersion;
			Cookie = cookie;
			Payload = payload;
		}

		public HandshakeRequest(byte majorVersion, byte minorVersion, byte[] cookie, byte[] payload)
		{
			MajorVersion = majorVersion;
			MinorVersion = minorVersion;
			Cookie = cookie;
			Payload = payload;
		}

		public int GetSize()
		{
			return sizeof(byte) + sizeof(byte) + sizeof(byte) + Cookie.Length + Payload.Length;
		}

		public byte[] Pack()
		{
			byte[] buf = new byte[GetSize()];
			int offset = 0;
			buf[offset++] = (byte)Type;
			buf[offset++] = MajorVersion;
			buf[offset++] = MinorVersion;
			BinaryUtil.Write(Cookie, buf, ref offset);
			BinaryUtil.Write(Payload, buf, ref offset);
			return buf;
		}

		public static bool TryUnpack(CookieProvider cookieProvider, RSA rsa, byte[] buf, int size, out HandshakeRequest packet)
		{
			if (size < CookieProvider.CookieSize + 3 || !cookieProvider.Check(buf, 3, out var cookie))
			{
				packet = default;
				return false;
			}

			int offest = 1;
			byte majorVersion = buf[offest++];
			byte minorVersion = buf[offest++];

			var payload = new byte[size - (CookieProvider.CookieSize + 3)];
			Buffer.BlockCopy(buf, CookieProvider.CookieSize + 3, payload, 0, payload.Length);

			if (rsa != null)
			{
				payload = rsa.Decrypt(payload, RSAEncryptionPadding.Pkcs1);
			}

			packet = new HandshakeRequest(majorVersion, minorVersion, cookie, payload);
			return true;
		}

	}

}