using System;
using System.Threading;

namespace SRNet
{
	internal class ConnectionStatusUpdater : IDisposable
	{
		public int UpdateCountdown { get; set; } = 64;

		public bool UseTimer
		{
			get => m_UseTimer;
			set
			{
				m_UseTimer = value;
				SetupTimer();
			}
		}

		bool m_Disposed;
		Connection m_Connection;
		Timer m_Timer;
		bool m_UseTimer;
		bool m_Unread = true;
		int m_Countdown = 64;

		internal ConnectionStatusUpdater(Connection connection)
		{
			m_Connection = connection;
			SetupTimer();
		}

		void SetupTimer()
		{
			if (m_Disposed) return;
			bool on = m_UseTimer || m_Unread;
			if (on)
			{
				if (m_Timer == null)
				{
					m_Timer = new Timer(TimerUpdate, null, 100, 100);
				}
			}
			else
			{
				m_Timer?.Dispose();
				m_Timer = null;
			}
		}

		public void TryUpdate(bool force)
		{
			if (force)
			{
				m_Connection.UpdateStatus();
			}
			else
			{
				m_Countdown--;
				if (m_Countdown <= 0)
				{
					m_Connection.UpdateStatus();
				}

			}
		}

		public void OnPreRead()
		{
			if (m_Unread)
			{
				m_Unread = false;
				SetupTimer();
			}
		}

		public void OnUpdate()
		{
			m_Countdown = UpdateCountdown;
		}

		bool m_RunTimerUpdate;
		void TimerUpdate(object _)
		{
			if (m_Disposed)
			{
				m_Timer?.Dispose();
				m_Timer = null;
				return;
			}
			if (m_RunTimerUpdate) return;
			try
			{
				m_RunTimerUpdate = true;
				m_Connection.UpdateStatus();
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				m_Timer?.Dispose();
				m_Timer = null;
				m_Connection.Dispose();
			}
			finally
			{
				m_RunTimerUpdate = false;
			}
		}

		public void Dispose()
		{
			if (m_Disposed) return;
			m_Disposed = true;
			m_Timer?.Dispose();
			m_Timer = null;
			m_Connection = null;
		}

	}

}