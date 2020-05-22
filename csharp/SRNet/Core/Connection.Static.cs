using SRNet.Packet;
using SRNet.Stun;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SRNet
{
	public partial class Connection : IDisposable
	{
		public static Connection StartServer(ServerConfig config)
		{
			return new Connection(new ServerConnectionImpl(config));
		}

		public static Task<P2PHostConnection> StartLocalHost(string roomName)
		{
			return StartLocalHost(new LocalHostConfig { RoomName = roomName });
		}

		public static Task<P2PHostConnection> StartLocalHost(LocalHostConfig config)
		{
			var impl = new P2PConnectionImpl(config);
			return Task.FromResult(new P2PHostConnection(impl));
		}

		public static async Task<ClientConnection> Connect(ServerConnectSettings settings)
		{
			return new ClientConnection(await new ConnectToServerTask(settings).Run());
		}

		public static async Task<Connection> Connect(DiscoveryRoom room)
		{
			var remoteEP = new IPEndPoint(room.Address, room.Port);
			if (!PeerToPeerRoomData.TryUnpack(room.Data, room.Data.Length, out var data))
			{
				throw new Exception("unpack room info");
			}
			var impl = await new ConnectToLocalOwnerTask(remoteEP, data, room.DiscoveryPort).Run();
			return new Connection(impl);
		}

		public static async Task<Connection> P2PMatching(string url, string stunURL = null)
		{
			var impl = await new MatchingPeersTask(url, stunURL).Run();
			return new Connection(impl);
		}

		public static async Task<Connection> P2PMatching(Func<StunResult, Task<P2PSettings>> func, string stunURL = null)
		{
			var impl = await new MatchingPeersTask(func, stunURL).Run();
			return new Connection(impl);
		}

	}

}