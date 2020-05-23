using SRNet.Packet;
using System;

namespace SRNet
{
	internal struct EncryptorKey
	{
		static readonly int HeaderKeySize = 2 + 4 + 4;
		public const int RandamKey = 22;

		public byte[] Cookie;
		public byte MajorVersion;
		public byte MinorVersion;
		public int Nonce;
		public int ConnectionId;
		public ArraySegment<byte> Random;

		public EncryptorKey(byte[] cookie, byte majorVersion, byte minorVersion, int nonce, int connectionId, byte[] random)
		{
			Cookie = cookie;
			MajorVersion = majorVersion;
			MinorVersion = minorVersion;
			Nonce = nonce;
			ConnectionId = connectionId;
			Random = new ArraySegment<byte>(random);
		}

		public EncryptorKey(HandshakeRequest request, HandshakeRequestPayload payload, int connectionId)
		{
			Cookie = request.Cookie;
			MajorVersion = request.MajorVersion;
			MinorVersion = request.MinorVersion;
			Nonce = payload.ClientId;
			ConnectionId = connectionId;
			Random = payload.Randam;
		}

		public EncryptorKey(PeerToPeerRequest request, byte[] randam, int nonce)
		{
			Cookie = request.Cookie;
			MajorVersion = request.MajorVersion;
			MinorVersion = request.MinorVersion;
			Nonce = nonce;
			ConnectionId = request.ConnectionId;
			Random = new ArraySegment<byte>(randam);
		}

		public static int GetInputKey(in EncryptorKey key, ref byte[] buf)
		{
			if (buf.Length < HeaderKeySize + key.Random.Count)
			{
				Array.Resize(ref buf, HeaderKeySize + key.Random.Count);
			}
			int offset = 0;
			buf[offset++] = key.MajorVersion;
			buf[offset++] = key.MinorVersion;
			BinaryUtil.Write(key.Nonce, buf, ref offset);
			BinaryUtil.Write(key.ConnectionId, buf, ref offset);
			BinaryUtil.Write(key.Random, buf, ref offset);
			return offset;
		}

		public override string ToString()
		{
			return $"{MajorVersion}:{MinorVersion}:{Nonce}:{ConnectionId}";
		}
	}

}