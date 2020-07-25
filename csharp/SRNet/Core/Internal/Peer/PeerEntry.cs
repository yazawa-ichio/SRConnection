using SRNet.Packet;
using System;
using System.Net;

namespace SRNet
{
	internal class PeerEntry : IDisposable
	{
		static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

		public bool Disposed;
		public readonly int ConnectionId;
		public int ClientConnectionId;
		public readonly Encryptor Encryptor;
		public short ReceiveSequence { get; private set; }
		public short RemoteReceiveSequence { get; private set; }
		public IPEndPoint EndPoint;

		TimeSpan m_TimeoutTimer;
		int m_CheckTimerSeq;
		int m_TimerSeq;
		short m_SendSequence;

		public TimeSpan Timeout = DefaultTimeout;

		public PeerEntry(int connectionId, int clientId, Encryptor encryptor, IPEndPoint endPoint)
		{
			ConnectionId = connectionId;
			ClientConnectionId = clientId;
			Encryptor = encryptor;
			EndPoint = endPoint;
			m_TimeoutTimer = Timeout;

			//初回送信時のSeqを適当なランダム値に変更
			var rand = (short)(Random.GenShort() / 2);
			if (rand < 0)
			{
				rand = (short)(-rand);
			}
			m_SendSequence = (short)((short.MaxValue / 4) + rand);
		}

		public short IncrementSendSequence()
		{
			return m_SendSequence++;
		}

		public void Update(IPEndPoint endPoint, short sendSeq, short ackSeq)
		{
			EndPoint = endPoint;
			ClientConnectionId = 0;
			ReceiveSequence = sendSeq;
			RemoteReceiveSequence = ackSeq;
			m_TimerSeq++;
		}

		public void Update(PeerInfo info)
		{
			if (EndPoint.Port != info.EndPont.Port && EndPoint.Address.ToString() != info.EndPont.Address)
			{
				EndPoint = info.EndPont.To();
			}
		}

		public void Dispose()
		{
			Disposed = true;
			Encryptor?.Dispose();
		}

		public bool CheckTimeout(TimeSpan delta)
		{
			if (m_TimerSeq != m_CheckTimerSeq)
			{
				m_TimeoutTimer = Timeout;
				m_CheckTimerSeq = m_TimerSeq;
			}
			m_TimeoutTimer -= delta;
			return m_TimeoutTimer <= TimeSpan.Zero;
		}

	}
}