using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SRNet.Tests
{
	[TestClass]
	public class DiscoveryTest
	{

		string To(byte[] buf)
		{
			return System.Convert.ToBase64String(buf);
		}

		[TestMethod, Timeout(10000)]
		public async Task Discovery経由のP2P通信()
		{
			byte[] data = Random.GenBytes(64);
			using var server = new UdpClient();
			server.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
			var serverIP = server.Client.LocalEndPoint as IPEndPoint;
			using var service = new DiscoveryService("DataTest", serverIP, data);
			service.Start();
			using var discoveryClient = new DiscoveryClient();
			discoveryClient.Start();
			DiscoveryRoom room = await discoveryClient.GetRoomAsync("DataTest");
			Assert.AreEqual("DataTest", room.Name, "ルーム名が正しい");
			Assert.AreEqual(To(data), To(room.Data), "設定したデータを受け取れる");

			service.OnHolePunchRequest += (ep) =>
			{
				var packet = new Packet.DiscoveryHolePunch().Pack();
				server.Send(packet, packet.Length, ep);
			};

			using var client = new UdpClient();

			{
				var packet = new Packet.DiscoveryHolePunch().Pack();
				client.Send(packet, packet.Length, new IPEndPoint(room.Address, room.DiscoveryPort));
				var res = await client.ReceiveAsync();
				var ret = Packet.DiscoveryHolePunch.TryUnpack(res.Buffer, res.Buffer.Length, out _);
				Assert.IsTrue(ret, "ホールパンチのリクエストを受け取れる");
				Assert.AreEqual(room.Address, res.RemoteEndPoint.Address, "サーバーのアドレスから来ている");
				Assert.AreEqual(room.Port, res.RemoteEndPoint.Port, "サーバーのアドレスから来ている");
			}
			{
				client.Send(data, data.Length, new IPEndPoint(room.Address, room.Port));
				var res = await server.ReceiveAsync();
				Assert.AreEqual(To(data), To(res.Buffer), "受け取ったエンドポイントに送信出来る");
			}

		}

		[TestMethod, Timeout(10000)]
		public async Task ルームのクエリ機能()
		{
			byte[] data = Random.GenBytes(64);
			using var server = new UdpClient();
			server.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
			var serverIP = server.Client.LocalEndPoint as IPEndPoint;
			using (var service = new DiscoveryService("NameMatchingTest", serverIP, data))
			using (var client = new DiscoveryClient())
			{
				service.Start(nameMatch: true);

				client.Start("UnMatchName");
				await Task.Delay(2000);
				Assert.AreEqual(0, client.GetRooms().Length, "名前一致ではないので部屋が見つからない");
				client.Start("NameMatchingTest");
				var room = await client.GetRoomAsync("NameMatchingTest");
				Assert.AreEqual("NameMatchingTest", room.Name, "部屋が見つかる");
			}

			using (var service = new DiscoveryService("CustomMatchingTest", serverIP, data))
			using (var client = new DiscoveryClient())
			{
				service.Start(x => x == "Match");

				client.Start("UnMatch");
				await Task.Delay(2000);
				Assert.AreEqual(0, client.GetRooms().Length, "不正なクエリなので部屋が見つからない");
				client.Start("Match");
				var room = await client.GetRoomAsync("CustomMatchingTest");
				Assert.AreEqual("CustomMatchingTest", room.Name, "部屋が見つかる");
			}
		}

	}
}