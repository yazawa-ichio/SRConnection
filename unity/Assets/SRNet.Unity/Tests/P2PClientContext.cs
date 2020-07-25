using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SRNet.Unity.Tests
{

	public class P2PClientContext : IDisposable
	{
		public P2PClient Client { get; }

		public int MessageReceiveCount;

		GameObject m_Owner;

		Queue<TaskCompletionSource<string>> m_Message = new Queue<TaskCompletionSource<string>>();

		public int AddPeerCount;
		public int RemovePeerCount;

		public P2PClientContext(string name)
		{
			m_Owner = new GameObject("P2PClient:" + name);
			Client = m_Owner.AddComponent<P2PClient>();
			Client.OnMessage += (peer, buf) =>
			{
				MessageReceiveCount++;
				if (m_Message.Count > 0)
				{
					var message = Encoding.UTF8.GetString(buf);
					m_Message.Dequeue().TrySetResult(message);
				}
			};

			Client.OnAddPeer += (_) => AddPeerCount++;
			Client.OnRemovePeer += (_) => RemovePeerCount++;

		}

		public void StartHost(string roomName)
		{
			Client.StartHost(roomName);
		}

		public Task ConnectHost(string roomName)
		{
			return Client.ConnectHost(roomName, new CancellationTokenSource(3000).Token);
		}

		public Task<string> GetMessage()
		{
			var future = new TaskCompletionSource<string>();
			m_Message.Enqueue(future);
			return future.Task;
		}

		public void Send(int id, string message, bool reliable = true)
		{
			var buf = Encoding.UTF8.GetBytes(message);
			Client.GetPeer(id).Send(buf, reliable);
		}

		public void Dispose()
		{
			Client.Disconnect();
			GameObject.Destroy(m_Owner);
		}

	}

}