using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SRNet.Tests
{

	[TestClass]
	public class ConnectionTest
	{
		public ConnectionTest()
		{
			Log.Init();
			Log.Level = Log.LogLevel.Trace;
		}

		byte[] To(string text)
		{
			return System.Text.Encoding.UTF8.GetBytes(text);
		}

		string To(byte[] buf)
		{
			return System.Text.Encoding.UTF8.GetString(buf);
		}

		[TestMethod, Timeout(10000)]
		public async Task エコーサーバーテスト()
		{
			using (var server = new EchoServer())
			using (var conn1 = await Connection.ConnectToServer(server.GetConnectSettings()))
			using (var conn2 = await Connection.ConnectToServer(server.GetConnectSettings()))
			{
				for (int i = 0; i < 100; i++)
				{
					var text = "Text:" + (i + 1);
					conn1.Server.Send(To(text));
					conn2.Server.Send(To(text));
					Message message;
					while (!conn1.PollTryReadMessage(out message, TimeSpan.FromSeconds(1))) ;
					Assert.AreEqual(text, To(message));
					while (!conn2.PollTryReadMessage(out message, TimeSpan.FromSeconds(1))) ;
					Assert.AreEqual(text, To(message));
				}
			}
		}

		[TestMethod, Timeout(50000)]
		public async Task サーバーランダムテスト()
		{
			using (var server = new EchoServer())
			{
				List<Task> tasks = new List<Task>();
				for (int i = 0; i < 1000; i++)
				{
					tasks.Add(RandamServerRequest(server.GetConnectSettings()));
				}
				await Task.WhenAll(tasks.ToArray());
			}
		}

		Task RandamServerRequest(ServerConnectSettings settings)
		{
			return Task.Run(async () =>
			{
				try
				{
					var start = DateTime.Now;
					using (var conn = await Connection.ConnectToServer(settings))
					{
						while ((DateTime.Now - start) < TimeSpan.FromSeconds(1))
						{
							await Task.Delay(100);
							var randam = System.Convert.ToBase64String(Random.GenBytes(1000));
							conn.Server.Send(To(randam));
							Message message;
							while (!conn.PollTryReadMessage(out message, TimeSpan.FromSeconds(1))) ;
							Assert.AreEqual(randam, To(message));
						}
					};
				}
				catch { }
			});
		}

		[TestMethod, Timeout(50000)]
		public async Task ユーザー切断テスト()
		{
			using (var server = new EchoServer())
			using (var conn = await Connection.ConnectToServer(server.GetConnectSettings()))
			{
				Assert.IsTrue(server.PeerEvents.Count > 0);
				Assert.IsTrue(server.PeerEvents.TryDequeue(out var e));
				Assert.AreEqual(e.EventType, PeerEvent.Type.Add);
				Assert.AreEqual(e.Peer.ConnectionId, conn.SelfId);
				conn.Dispose();
				while (true)
				{
					await Task.Delay(100);
					if (server.PeerEvents.Count > 0)
					{
						Assert.IsTrue(server.PeerEvents.TryDequeue(out e));
						Assert.AreEqual(e.EventType, PeerEvent.Type.Remove);
						Assert.AreEqual(e.Peer.ConnectionId, conn.SelfId);
						break;
					}
				}
				Assert.AreEqual(server.Conn.Peers.Count, 0);
			}
		}

		[TestMethod, Timeout(10000)]
		public async Task サーバー切断テスト()
		{
			using (var server = new EchoServer())
			using (var conn1 = await Connection.ConnectToServer(server.GetConnectSettings()))
			using (var conn2 = await Connection.ConnectToServer(server.GetConnectSettings()))
			using (var conn3 = await Connection.ConnectToServer(server.GetConnectSettings()))
			{
				string sendmessage = "サーバー切断テスト";
				server.Conn.Reliable.Broadcast(To(sendmessage));
				server.Dispose();
				string msg1 = null;
				string msg2 = null;
				string msg3 = null;
				while (!conn1.Disposed)
				{
					if (conn1.TryReadMessage(out var m))
					{
						Assert.IsNull(msg1);
						msg1 = To(m);
					}
				}
				while (!conn2.Disposed)
				{
					if (conn2.TryReadMessage(out var m))
					{
						Assert.IsNull(msg2);
						msg2 = To(m);
					}
				}
				while (!conn3.Disposed)
				{
					if (conn3.TryReadMessage(out var m))
					{
						Assert.IsNull(msg3);
						msg3 = To(m);
					}
				}
				Assert.AreEqual(sendmessage, msg1);
				Assert.AreEqual(sendmessage, msg2);
				Assert.AreEqual(sendmessage, msg3);
			}
		}

		[TestMethod, Timeout(10000)]
		public async Task アクセサテスト()
		{
			using (var server = new EchoServer())
			using (var conn = await Connection.ConnectToServer(server.GetConnectSettings()))
			{
				server.Conn.Reliable.Target(conn.SelfId).Send(To("PeerChannelAccessor"));

				Message message;
				while (!conn.PollTryReadMessage(out message, TimeSpan.FromSeconds(1))) ;
				Assert.AreEqual("PeerChannelAccessor", To(message), "PeerChannelAccessor経由で送信");
				Assert.AreEqual(DefaultChannel.Reliable, message.ChannelId, "PeerChannelAccessor経由で送信");

				message.PeerChannel.Send(To("Message.PeerChannelAccessor"));
				while (!conn.PollTryReadMessage(out message, TimeSpan.FromSeconds(1))) ;
				Assert.AreEqual("Message.PeerChannelAccessor", To(message), "MessageのPeerChannelAccessor経由で送信。エコーが返る");


			}
		}

		[TestMethod, Timeout(10000)]
		public async Task クライアントキャンセルテスト()
		{
			using (var server = new EchoServer())
			{
				await Assert.ThrowsExceptionAsync<OperationCanceledException>(() =>
				{
					var cancellationTokenSource = new CancellationTokenSource();
					cancellationTokenSource.Cancel();
					return Connection.ConnectToServer(server.GetConnectSettings(), cancellationTokenSource.Token);
				}, "即時キャンセルの場合");
				await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
				{
					var cancellationTokenSource = new CancellationTokenSource();
					var task = Connection.ConnectToServer(server.GetConnectSettings(), cancellationTokenSource.Token);
					await Task.Yield();
					cancellationTokenSource.Cancel();
					await task;
				}, "即時キャンセルではない場合");
			}
		}

		[TestMethod, Timeout(10000)]
		public async Task ローカルP2Pテスト()
		{
			using (var host = Connection.StartLocalHost("TestRoom"))
			using (var server = new EchoServer(host))
			{
				DiscoveryRoom room = null;
				using (var discovery = new DiscoveryClient())
				{
					discovery.Start();
					room = await discovery.GetRoomAsync("TestRoom");
				}
				using (var conn1 = await Connection.ConnectToRoom(room))
				using (var conn2 = await Connection.ConnectToRoom(room))
				{
					for (int i = 0; i < 100; i++)
					{
						var text = "Text:" + (i + 1);
						conn1.Send(host.SelfId, To(text));
						conn2.Send(host.SelfId, To(text));
						Message message;
						while (!conn1.PollTryReadMessage(out message, TimeSpan.FromSeconds(1))) ;
						Assert.AreEqual(text, To(message));
						Assert.AreEqual(host.SelfId, message.Peer.ConnectionId);
						while (!conn2.PollTryReadMessage(out message, TimeSpan.FromSeconds(1))) ;
						Assert.AreEqual(text, To(message));
						Assert.AreEqual(host.SelfId, message.Peer.ConnectionId);

						conn1.Send(conn2.SelfId, To(text));
						conn2.Send(conn1.SelfId, To(text));
						while (!conn1.PollTryReadMessage(out message, TimeSpan.FromSeconds(1))) ;
						Assert.AreEqual(text, To(message));
						Assert.AreEqual(conn2.SelfId, message.Peer.ConnectionId);
						while (!conn2.PollTryReadMessage(out message, TimeSpan.FromSeconds(1))) ;
						Assert.AreEqual(text, To(message));
						Assert.AreEqual(conn1.SelfId, message.Peer.ConnectionId);

						host.Send(conn1.SelfId, To(text));
						host.Send(conn2.SelfId, To(text));
						while (!conn1.PollTryReadMessage(out message, TimeSpan.FromSeconds(1))) ;
						Assert.AreEqual(text, To(message));
						Assert.AreEqual(host.SelfId, message.Peer.ConnectionId);
						while (!conn2.PollTryReadMessage(out message, TimeSpan.FromSeconds(1))) ;
						Assert.AreEqual(text, To(message));
						Assert.AreEqual(host.SelfId, message.Peer.ConnectionId);


					}
				}

			}
		}

		[TestMethod, Timeout(10000)]
		public async Task ローカルP2Pキャンセルテスト()
		{
			using (var host = Connection.StartLocalHost("TestRoom"))
			using (var server = new EchoServer(host))
			{
				DiscoveryRoom room = null;
				using (var discovery = new DiscoveryClient())
				{
					discovery.Start();
					room = await discovery.GetRoomAsync("TestRoom");
				}
				await Assert.ThrowsExceptionAsync<OperationCanceledException>(() =>
				{
					var cancellationTokenSource = new CancellationTokenSource();
					cancellationTokenSource.Cancel();
					return Connection.ConnectToRoom(room, token: cancellationTokenSource.Token);
				}, "即時キャンセルの場合");
				await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
				{
					var cancellationTokenSource = new CancellationTokenSource();
					var task = Connection.ConnectToRoom(room, token: cancellationTokenSource.Token);
					await Task.Yield();
					cancellationTokenSource.Cancel();
					await task;
				}, "即時キャンセルではない場合");
			}
		}

		[TestMethod, Timeout(30000)]
		public async Task HttpマッチングP2Pテスト()
		{
			using (var matching = new TestMatchingServer())
			{
				matching.Start();
				await Task.WhenAll(GetConn(7));
			}
		}

		[TestMethod, Timeout(30000)]
		public async Task Httpマッチングタイムアウトテスト()
		{
			using (var matching = new TestMatchingServer())
			{
				matching.Start();
				bool error = false;
				try
				{
					await P2PTestConn();
				}
				catch
				{
					error = true;
				}
				Assert.IsTrue(error, "タイムアウトする");
			}
		}

		[TestMethod, Timeout(30000)]
		public async Task Httpマッチングキャンセルテスト()
		{
			using (var matching = new TestMatchingServer())
			{
				matching.Start();
				await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
				{
					CancellationTokenSource source = new CancellationTokenSource();
					source.Cancel();
					await P2PTestConn(source.Token);
				}, "即時キャンセルの場合");
				await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
				{
					CancellationTokenSource source = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
					await P2PTestConn(source.Token);
				}, "即時キャンセルではない場合");
			}
		}

		IEnumerable<Task> GetConn(int num)
		{
			for (int i = 0; i < num; i++)
			{
				yield return P2PTestConn();
			}
		}

		async Task P2PTestConn(CancellationToken token = default)
		{
			using (var conn = await Connection.P2PMatching("http://localhost:8080", token: token))
			{
				bool success = false;
				for (int i = 0; i < 10; i++)
				{
					await Task.Delay(100);
					conn.Reliable.Broadcast(System.Text.Encoding.UTF8.GetBytes("Message" + DateTime.Now));
					while (conn.TryReadMessage(out var _))
					{
						success = true;
					}
				}
				Assert.IsTrue(success, "メッセージを受け取れる");
			}
		}

	}
}