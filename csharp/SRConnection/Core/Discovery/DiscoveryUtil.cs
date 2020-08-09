using System.Threading;
using System.Threading.Tasks;

namespace SRConnection
{
	public static class DiscoveryUtil
	{
		public static Task<DiscoveryRoom> GetRoom(string roomName, int timeoutMilliseconds = 10000)
		{
			return GetRoom(roomName, new CancellationTokenSource(timeoutMilliseconds).Token);
		}

		public static async Task<DiscoveryRoom> GetRoom(string roomName, CancellationToken token)
		{
			using (var client = new DiscoveryClient())
			{
				client.Start();
				return await client.GetRoomAsync(roomName, token);
			}
		}

		public static Task<DiscoveryRoom[]> GetRooms(int timeoutMilliseconds = 10000)
		{
			return GetRooms(new CancellationTokenSource(timeoutMilliseconds).Token);
		}

		public static async Task<DiscoveryRoom[]> GetRooms(CancellationToken token)
		{
			using (var client = new DiscoveryClient())
			{
				client.Start();
				return await client.GetRoomsAsync(token);
			}
		}

	}
}