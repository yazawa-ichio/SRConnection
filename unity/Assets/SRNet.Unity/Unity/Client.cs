using SRNet.Channel;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SRNet.Unity
{
	public class Client : MonoBehaviour
	{
		public event Action OnConnect;

		public event Action OnDisconnect;

		public event Action<byte[]> OnMessage;

		public event Action<Message> OnRawMessage;

		public bool IsConnection => m_Connection != null && !m_Connection.Disposed;

		public int SelfId => m_Connection.SelfId;

		public ChannelMapAccessor Channel => m_Connection.Channel;

		public bool AutoDispatch { get; set; } = true;

		ClientConnection m_Connection;
		CancellationTokenSource m_Cancellation = default;

		public Task Connect(string host, int port, CancellationToken token = default)
		{
			var settings = ServerConnectSettings.Create(host, port);
			return Connect(settings, token);
		}

		public Task Connect(string publicKey, string host, int port, CancellationToken token = default)
		{
			var settings = ServerConnectSettings.FromXML(publicKey, host, port);
			return Connect(settings, token);
		}

		public async Task Connect(ServerConnectSettings settings, CancellationToken token = default)
		{
			Disconnect();
			m_Cancellation?.Cancel();
			m_Cancellation = new CancellationTokenSource();
			token.Register(m_Cancellation.Cancel);
			m_Connection = await Connection.ConnectToServer(settings, m_Cancellation.Token);
			m_Cancellation = null;
			OnConnect?.Invoke();
			Update();
		}

		public void Send(byte[] buf, bool reliable = true)
		{
			m_Connection.Server.Send(buf, 0, buf.Length, reliable);
		}

		public void Send(byte[] buf, int offset, int size, bool reliable = true)
		{
			m_Connection.Server.Send(buf, offset, size, reliable);
		}

		public void Send<T>(Action<Stream, T> write, in T obj, bool reliable = true)
		{
			m_Connection.Server.Send(write, in obj, reliable);
		}

		public PeerChannelAccessor PeerChannel(short channel) => m_Connection.Server.Channel(channel);

		public void Disconnect()
		{
			if (m_Connection == null)
			{
				m_Cancellation?.Cancel();
				m_Cancellation = null;
			}
			else
			{
				if (!m_Connection.Disposed)
				{
					m_Connection.BroadcastDisconnect();
					m_Connection.Dispose();
					m_Connection = null;
				}
				OnDisconnect?.Invoke();
			}
		}

		public void Dispatch()
		{
			if (m_Connection == null) return;

			if (m_Connection.Disposed)
			{
				Disconnect();
				return;
			}

			while (m_Connection.TryReadMessage(out var message))
			{
				OnRawMessage?.Invoke(message);

				OnMessage?.Invoke(message.ToArray());

				if (m_Connection == null)
				{
					return;
				}

				if (m_Connection.Disposed)
				{
					Disconnect();
					return;
				}

			}
		}

		void Update()
		{
			if (AutoDispatch)
			{
				Dispatch();
			}
		}

		void OnDestroy()
		{
			Disconnect();
		}

	}

}