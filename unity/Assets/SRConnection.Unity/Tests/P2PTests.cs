using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using UnityEngine.TestTools;
using TimeoutAttribute = NUnit.Framework.TimeoutAttribute;

namespace SRConnection.Unity.Tests
{
	public class P2PTests
	{
		IEnumerator Async(Func<Task> func)
		{
			var task = func();
			while (!task.IsCompleted) yield return null;
			if (task.IsFaulted)
			{
				ExceptionDispatchInfo.Capture(task.Exception.InnerException).Throw();
			}
			Assert.IsFalse(task.IsCanceled);
		}

		[UnityTest, Timeout(10000)]
		public IEnumerator 接続テスト() => Async(async () =>
		{
			using (var host = new P2PClientContext("host"))
			using (var client1 = new P2PClientContext("client1"))
			using (var client2 = new P2PClientContext("client2"))
			{
				const string RoomName = "TestRoom";
				host.StartHost(RoomName);

				bool handleConnect = false;
				bool handleDisconnect = false;

				client1.Client.OnConnect += () => handleConnect = true;
				client1.Client.OnDisconnect += () => handleDisconnect = true;

				await client1.ConnectHost(RoomName);

				Assert.IsTrue(handleConnect, "接続完了イベントが取れている");
				foreach (var ctx in new P2PClientContext[] { host, client1 })
				{
					Assert.AreEqual(1, ctx.Client.Peers.Count, "参加が出来た" + ctx.Client.name);
					Assert.AreEqual(1, ctx.AddPeerCount, "参加が出来た" + ctx.Client.name);
				}

				await client2.ConnectHost(RoomName);

				foreach (var ctx in new P2PClientContext[] { host, client1, client2 })
				{
					Assert.AreEqual(2, ctx.Client.Peers.Count, "参加が出来た");
					Assert.AreEqual(2, ctx.AddPeerCount, "参加が出来た");
				}

				host.Client.StopHostMatching();
				using (var client3 = new P2PClientContext("client3"))
				{
					try
					{
						await client3.ConnectHost(RoomName);
						Assert.IsTrue(false, "ホストの受付が終わっているので接続できない");
					}
					catch { }
				}

				//切断

				var client1Id = client1.Client.SelfId;
				client1.Client.Disconnect();
				Assert.IsFalse(client1.Client.IsConnection, "切断されている");

				await Task.Delay(100);

				Assert.AreEqual(1, host.Client.Peers.Count, "切断される");
				Assert.AreEqual(1, host.RemovePeerCount, "切断される");
				Assert.IsTrue(handleDisconnect, "切断される");

				Assert.IsNull(host.Client.GetPeer(client1Id), "切断されているのでNullが返る");
				Assert.IsNull(client2.Client.GetPeer(client1Id), "切断されているのでNullが返る");
				Assert.IsNotNull(host.Client.GetPeer(client2.Client.SelfId), "切断していない方は生きている");

				handleDisconnect = false;
				client2.Client.OnDisconnect += () => handleDisconnect = true;

				host.Client.Disconnect();
				await Task.Delay(100);

				Assert.IsFalse(client2.Client.IsConnection, "ホスト側の切断を検知出来ている");
				Assert.IsTrue(handleDisconnect, "ホスト切断を検知出来ている");

			}
		});


		[UnityTest, Timeout(10000)]
		public IEnumerator 送信テスト() => Async(async () =>
		{
			using (var host = new P2PClientContext("host"))
			using (var client1 = new P2PClientContext("client1"))
			using (var client2 = new P2PClientContext("client2"))
			{
				const string RoomName = "TestRoom2";
				host.StartHost(RoomName);

				await client1.ConnectHost(RoomName);
				await client2.ConnectHost(RoomName);

				const string TestMessage = "エコーテスト";
				for (int i = 0; i < 5; i++)
				{
					var task = host.GetMessage();
					client1.Send(host.Client.SelfId, TestMessage + i, reliable: true);
					Assert.AreEqual(TestMessage + i, await task, "Host=>Clientで受け取れている");
				}
				for (int i = 0; i < 5; i++)
				{
					var task = client1.GetMessage();
					host.Send(client1.Client.SelfId, TestMessage + i, reliable: false);
					Assert.AreEqual(TestMessage + i, await task, "Host=>Clientで受け取れている");
				}

				for (int i = 0; i < 5; i++)
				{
					var task = client2.GetMessage();
					client1.Send(client2.Client.SelfId, TestMessage + i, reliable: false);
					Assert.AreEqual(TestMessage + i, await task, "Client=>Clientで受け取れている");
				}

				Assert.AreEqual(5, host.MessageReceiveCount, "送信回数分受け取っている");
				Assert.AreEqual(5, client1.MessageReceiveCount, "送信回数分受け取っている");
				Assert.AreEqual(5, client2.MessageReceiveCount, "送信回数分受け取っている");

			}
		});


	}
}