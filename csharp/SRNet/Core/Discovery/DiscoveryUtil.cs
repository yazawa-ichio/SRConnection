using System.Threading;
using System.Threading.Tasks;

namespace SRNet
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
	}
}