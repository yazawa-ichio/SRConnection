using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using UnityEngine.TestTools;
using TimeoutAttribute = NUnit.Framework.TimeoutAttribute;

namespace SRNet.Unity.Tests
{
	public class ServerClientTests : ServerClientTestsBase
	{
		public override bool UseRSA => true;

		public override bool UseIPV6 => false;
	}

	public class ServerClientTests_UnuseRSA : ServerClientTestsBase
	{
		public override bool UseRSA => false;

		public override bool UseIPV6 => false;
	}

	public class ServerClientTests_IPV6 : ServerClientTestsBase
	{
		public override bool UseRSA => false;

		public override bool UseIPV6 => true;
	}


	public abstract class ServerClientTestsBase
	{
		public abstract bool UseRSA { get; }
		public abstract bool UseIPV6 { get; }

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
			using (var server = new ServerContext<TestEchoServer>(!UseIPV6, UseRSA))
			using (var client1 = new ClientContext())
			using (var client2 = new ClientContext())
			{
				Assert.AreEqual(0, server.Server.Peers.Count, "誰もいない");
				bool handleConnect = false;
				bool handleDisconnect = false;

				client1.Client.OnConnect += () => handleConnect = true;
				client1.Client.OnDisconnect += () => handleDisconnect = true;

				await client1.Connect(server.GetConnectSettings());
				Assert.AreEqual(1, server.Server.Peers.Count, "接続完了");
				Assert.AreEqual(1, server.Server.AddCount, "接続完了");

				Assert.IsTrue(handleConnect, "接続完了");
				Assert.IsFalse(handleDisconnect, "接続完了");

				var client1Id = client1.Client.SelfId;

				Assert.IsNotNull(server.Server.GetPeer(client1Id), "クライアントのコネクションIDが同じ");
				await client2.Connect(server.GetConnectSettings());
				Assert.AreEqual(2, server.Server.Peers.Count, "接続完了");
				Assert.AreEqual(2, server.Server.AddCount, "接続完了");

				//切断を行う
				client1.Client.Disconnect();
				Assert.IsFalse(client1.Client.IsConnection, "切断されている");

				await Task.Delay(100);
				Assert.AreEqual(1, server.Server.Peers.Count, "切断される");
				Assert.AreEqual(1, server.Server.RemoveCount, "切断される");
				Assert.IsTrue(handleDisconnect, "切断される");

				Assert.IsNull(server.Server.GetPeer(client1Id), "切断されているのでNullが返る");
				Assert.IsNotNull(server.Server.GetPeer(client2.Client.SelfId), "切断していない方は生きている");

				//サーバー側を切断
				handleDisconnect = false;
				client2.Client.OnDisconnect += () => handleDisconnect = true;


				server.Server.Disconnect();
				await Task.Delay(100);

				Assert.IsFalse(client2.Client.IsConnection, "サーバー側の切断を検知出来ている");
				Assert.IsTrue(handleDisconnect, "サーバー側の切断を検知出来ている");
			}
		});

		[UnityTest, Timeout(10000)]
		public IEnumerator エコーサーバーテスト() => Async(async () =>
		{
			using (var server = new ServerContext<TestEchoServer>(!UseIPV6, UseRSA))
			using (var client1 = new ClientContext())
			using (var client2 = new ClientContext())
			{
				await client1.Connect(server.GetConnectSettings());
				await client2.Connect(server.GetConnectSettings());

				const string TestMessage = "エコーテスト";
				for (int i = 0; i < 5; i++)
				{
					client1.Send(TestMessage + i, reliable: true);
					Assert.AreEqual(TestMessage + i, await client1.GetMessage(), "エコーが返ってくる");
				}
				for (int i = 0; i < 5; i++)
				{
					client1.Send(TestMessage + i, reliable: false);
					Assert.AreEqual(TestMessage + i, await client1.GetMessage(), "エコーが返ってくる");
				}

				Assert.AreEqual(10, client1.MessageReceiveCount, "送信回数分受け取っている");
				Assert.AreEqual(0, client2.MessageReceiveCount, "こちらには送信されない");

				//Broadcastを行うように変更
				server.Server.EchoBroadcast = true;

				var task = Task.WhenAll(
					client1.GetMessage(),
					client2.GetMessage(),
					client1.GetMessage(),
					client2.GetMessage(),
					client1.GetMessage(),
					client2.GetMessage()
				);

				client2.Send(TestMessage + "Broadcast:reliable", true);
				client2.Send(TestMessage + "Broadcast:unreliable", false);

				// サーバーからの送信。待たないとクライアントの送信よりも早く届いてしまうので待つ。
				await Task.Delay(300);
				server.Broadcast(TestMessage + "From Server");

				var ret = await task;

				Assert.AreEqual(13, client1.MessageReceiveCount, "受け取り回数が正しい");
				Assert.AreEqual(3, client2.MessageReceiveCount, "受け取り回数が正しい");

				Assert.AreEqual(TestMessage + "Broadcast:reliable", ret[0], "受け取れている");
				Assert.AreEqual(TestMessage + "Broadcast:reliable", ret[1], "受け取れている");
				Assert.AreEqual(TestMessage + "Broadcast:unreliable", ret[2], "受け取れている");
				Assert.AreEqual(TestMessage + "Broadcast:unreliable", ret[3], "受け取れている");
				Assert.AreEqual(TestMessage + "From Server", ret[4], "受け取れている");
				Assert.AreEqual(TestMessage + "From Server", ret[5], "受け取れている");


			}
		});
	}

}