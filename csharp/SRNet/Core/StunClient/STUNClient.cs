using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SRNet.Stun
{
	public static class StunClient
	{
		public static Task<StunResult> Run()
		{
			return Run("stun.l.google.com", 19302);
		}

		public static async Task<StunResult> Run(string host, int port, CancellationToken token = default)
		{
			using (var client = new UdpClient())
			{
				return await Run(client, host, port, token);
			}
		}

		public static Task<StunResult> Run(UdpClient client, string host, int port, CancellationToken token = default)
		{

			var query = new StunQuery(client.Client, host, port);
			var task = query.Run(token);
			Task.Run(() =>
			{
				while (!task.IsCompleted)
				{
					if (client.Client.Poll(1000 * 1000, SelectMode.SelectRead))
					{
						IPEndPoint remoteEP = null;
						var buf = client.Receive(ref remoteEP);
						query.TryReceive(buf);
					}
				}
			});
			return task;
		}

	}
}