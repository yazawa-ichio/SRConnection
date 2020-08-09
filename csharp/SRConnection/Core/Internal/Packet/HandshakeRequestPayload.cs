using System;

namespace SRConnection.Packet
{
	internal readonly struct HandshakeRequestPayload
	{
		public readonly int ClientId;
		public readonly byte RandamSize;
		public readonly ArraySegment<byte> Randam;

		public HandshakeRequestPayload(int clientId, byte[] randam)
		{
			ClientId = clientId;
			RandamSize = (byte)randam.Length;
			Randam = new ArraySegment<byte>(randam);
		}

		public HandshakeRequestPayload(int clientId, ArraySegment<byte> randam)
		{
			ClientId = clientId;
			RandamSize = (byte)randam.Count;
			Randam = randam;
		}

		public int GetSize()
		{
			return sizeof(int) + sizeof(byte) + RandamSize;
		}

		public byte[] Pack()
		{
			byte[] buf = new byte[GetSize()];
			int offset = 0;
			BinaryUtil.Write(ClientId, buf, ref offset);
			buf[offset++] = RandamSize;
			BinaryUtil.Write(Randam, buf, ref offset);
			return buf;
		}

		public static bool TryUnpack(byte[] buf, out HandshakeRequestPayload payload)
		{
			int offest = 0;
			var clientId = BinaryUtil.ReadInt(buf, ref offest);
			var size = buf[offest++];
			var randam = BinaryUtil.ReadArraySegment(buf, size, ref offest);
			payload = new HandshakeRequestPayload(clientId, randam);
			return true;
		}

	}

}