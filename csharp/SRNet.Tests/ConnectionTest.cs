using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SRNet.Tests
{

	[TestClass]
	public class ConnectionTest
	{
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
			using (var conn1 = await Connection.Connect(server.GetConnectSettings()))
			using (var conn2 = await Connection.Connect(server.GetConnectSettings()))
			{
				for (int i = 0; i < 100; i++)
				{
					var text = "Text:" + (i + 1);
					conn1.Server.Send(To(text));
					conn2.Server.Send(To(text));
					Message message;
					while (!conn1.TryPollReceive(out message, TimeSpan.FromSeconds(1))) ;
					Assert.AreEqual(text, To(message));
					while (!conn2.TryPollReceive(out message, TimeSpan.FromSeconds(1))) ;
					Assert.AreEqual(text, To(message));
				}
			}
		}


		[TestMethod, Timeout(50000)]
		public async Task ユーザー切断テスト()
		{
			Log.Init();
			Log.Level = Log.LogLevel.Trace;
			using (var server = new EchoServer())
			using (var conn = await Connection.Connect(server.GetConnectSettings()))
			{
				Assert.IsTrue(server.Conn.TryGetPeerEvent(out var e));
				Assert.AreEqual(e.EventType, PeerEvent.Type.Add);
				Assert.AreEqual(e.Peer.ConnectionId, conn.SelfId);
				conn.Dispose();
				while (true)
				{
					await Task.Delay(100);
					if (server.Conn.TryGetPeerEvent(out e))
					{
						Assert.AreEqual(e.EventType, PeerEvent.Type.Remove);
						Assert.AreEqual(e.Peer.ConnectionId, conn.SelfId);
						break;
					}
				}

				Assert.AreEqual(server.Conn.GetPeers().Count, 0);
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
				using (var conn1 = await Connection.Connect(room))
				using (var conn2 = await Connection.Connect(room))
				{
					await Task.WhenAll(conn1.WaitP2PConnectComplete(), conn2.WaitP2PConnectComplete());
					for (int i = 0; i < 100; i++)
					{
						var text = "Text:" + (i + 1);
						conn1.Send(host.SelfId, To(text));
						conn2.Send(host.SelfId, To(text));
						Message message;
						while (!conn1.TryPollReceive(out message, TimeSpan.FromSeconds(1))) ;
						Assert.AreEqual(text, To(message));
						Assert.AreEqual(host.SelfId, message.Peer.ConnectionId);
						while (!conn2.TryPollReceive(out message, TimeSpan.FromSeconds(1))) ;
						Assert.AreEqual(text, To(message));
						Assert.AreEqual(host.SelfId, message.Peer.ConnectionId);

						conn1.Send(conn2.SelfId, To(text));
						conn2.Send(conn1.SelfId, To(text));
						while (!conn1.TryPollReceive(out message, TimeSpan.FromSeconds(1))) ;
						Assert.AreEqual(text, To(message));
						Assert.AreEqual(conn2.SelfId, message.Peer.ConnectionId);
						while (!conn2.TryPollReceive(out message, TimeSpan.FromSeconds(1))) ;
						Assert.AreEqual(text, To(message));
						Assert.AreEqual(conn1.SelfId, message.Peer.ConnectionId);

						host.Send(conn1.SelfId, To(text));
						host.Send(conn2.SelfId, To(text));
						while (!conn1.TryPollReceive(out message, TimeSpan.FromSeconds(1))) ;
						Assert.AreEqual(text, To(message));
						Assert.AreEqual(host.SelfId, message.Peer.ConnectionId);
						while (!conn2.TryPollReceive(out message, TimeSpan.FromSeconds(1))) ;
						Assert.AreEqual(text, To(message));
						Assert.AreEqual(host.SelfId, message.Peer.ConnectionId);


					}
				}

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

		IEnumerable<Task> GetConn(int num)
		{
			for (int i = 0; i < num; i++)
			{
				yield return P2PTestConn();
			}
		}

		async Task P2PTestConn()
		{
			using (var conn = await Connection.P2PMatching("http://localhost:8080"))
			{
				await conn.WaitP2PConnectComplete();
				bool success = false;
				for (int i = 0; i < 10; i++)
				{
					await Task.Delay(100);
					foreach (var peer in conn.GetPeers())
					{
						peer.Send(System.Text.Encoding.UTF8.GetBytes("Message" + DateTime.Now));
					}
					while (conn.TryReceive(out var _))
					{
						success = true;
					}
				}
				Assert.IsTrue(success, "メッセージを受け取れる");
			}
		}

	}
}