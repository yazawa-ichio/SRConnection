namespace SRConnection
{
	public class RuntimeConfig
	{

		public bool AutoDisposeOnDisconnectOwner
		{
			get => m_Impl.DisposeOnDisconnectOwner;
			set => m_Impl.DisposeOnDisconnectOwner = value;
		}

		public int StatusUpdateCountdown { get => m_Updater.UpdateCountdown; set => m_Updater.UpdateCountdown = value; }

		public bool UseTimerStatusUpdate { get => m_Updater.UseTimer; set => m_Updater.UseTimer = value; }

		Connection m_Connection;
		ConnectionImpl m_Impl;
		ConnectionStatusUpdater m_Updater;

		internal RuntimeConfig(Connection connection, ConnectionImpl impl, ConnectionStatusUpdater updater)
		{
			m_Connection = connection;
			m_Impl = impl;
			m_Updater = updater;
		}

	}

}