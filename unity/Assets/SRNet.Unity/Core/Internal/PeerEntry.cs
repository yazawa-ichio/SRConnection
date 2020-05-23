using System;
using System.Net;

namespace SRNet
{
	internal class PeerEntry : IDisposable
	{
		static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

		public bool Disposed;
		public readonly int ConnectionId;
		public int ClientConnectionId;
		public readonly Encryptor Encryptor;
		public short SendSequence { get; private set; }
		public short ReceiveSequence { get; private set; }
		public short RemoteReceiveSequence { get; private set; }
		public IPEndPoint EndPoint;

		TimeSpan m_TimeoutTimer;
		int m_CheckTimerSeq;
		int m_TimerSeq;

		public PeerEntry(int connectionId, int clientId, Encryptor encryptor, IPEndPoint endPoint)
		{
			ConnectionId = connectionId;
			ClientConnectionId = clientId;
			Encryptor = encryptor;
			EndPoint = endPoint;
			m_TimeoutTimer = Timeout;
		}

		public short IncrementSendSequence()
		{
			return SendSequence++;
		}

		public void Update(IPEndPoint endPoint, short sendSeq, short ackSeq)
		{
			EndPoint = endPoint;
			ClientConnectionId = 0;
			ReceiveSequence = sendSeq;
			RemoteReceiveSequence = ackSeq;
			m_TimerSeq++;
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