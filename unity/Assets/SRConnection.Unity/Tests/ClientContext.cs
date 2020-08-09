using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRConnection.Unity.Tests
{
	public class ClientContext : IDisposable
	{
		public Client Client { get; }

		public int MessageReceiveCount;

		GameObject m_Owner;

		Queue<TaskCompletionSource<string>> m_Message = new Queue<TaskCompletionSource<string>>();

		public ClientContext()
		{
			m_Owner = new GameObject("Client");
			Client = m_Owner.AddComponent<Client>();
			Client.OnMessage += (buf) =>
			{
				MessageReceiveCount++;
				if (m_Message.Count > 0)
				{
					var message = Encoding.UTF8.GetString(buf);
					m_Message.Dequeue().TrySetResult(message);
				}
			};
		}

		public Task<string> GetMessage()
		{
			var future = new TaskCompletionSource<string>();
			m_Message.Enqueue(future);
			return future.Task;
		}

		public void Send(string message, bool reliable = true)
		{
			var buf = Encoding.UTF8.GetBytes(message);
			Client.Send(buf, reliable);
		}

		public Task Connect(ServerConnectSettings settings)
		{
			return Client.Connect(settings);
		}

		public void Dispose()
		{
			GameObject.Destroy(m_Owner);
		}

	}
}