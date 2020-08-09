using SRConnection.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DiscoveryPacket = SRConnection.Packet.Discovery;

namespace SRConnection
{
	public class DiscoveryClient : IDisposable
	{
		class Entry
		{
			public DiscoveryRoom Room;
			public DateTime ReceiveAt;
		}

		UdpSocket m_Socket = new UdpSocket();
		Dictionary<string, Entry> m_Room = new Dictionary<string, Entry>();
		bool m_Run;
		IPAddress m_Address;
		DiscoveryPacket m_DiscoveryPacket;
		int m_DiscoveryPort;
		Queue<TaskCompletionSource<DiscoveryRoom>> m_WaitNewRooms = new Queue<TaskCompletionSource<DiscoveryRoom>>();

		public DiscoveryClient() : this(IPAddress.Any) { }

		public DiscoveryClient(IPAddress localAdress, int servicePort = DiscoveryService.Port)
		{
			m_Address = localAdress;
			m_DiscoveryPort = servicePort;
		}

		public void Start() => Start("");

		public void Start(string query)
		{
			if (m_Socket == null)
			{
				throw new InvalidOperationException("すでに解放済みです");
			}
			m_DiscoveryPacket = new DiscoveryPacket(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(query)));
			if (m_Run)
			{
				return;
			}
			m_Run = true;
			m_Socket.Bind(new IPEndPoint(m_Address, 0), false);
			DiscoveryImpl();
		}

		async void DiscoveryImpl()
		{
			try
			{
				Task<UdpReceiveResult> receive = null;
				while (m_Run)
				{
					var buf = m_DiscoveryPacket.Pack();
					m_Socket.Broadcast(buf, 0, buf.Length, m_DiscoveryPort);
					do
					{
						if (receive == null)
						{
							receive = m_Socket.ReceiveAsync();
						}
						await Task.WhenAny(receive, Task.Delay(1000));
						if (!m_Run) return;
						TryReceive(ref receive);
					} while (m_Run && receive == null);
				}
			}
			catch (Exception ex)
			{
				if (m_Run)
				{
					Log.Exception(ex);
				}
			}
		}

		void TryReceive(ref Task<UdpReceiveResult> receive)
		{
			if (receive.IsCompleted)
			{
				if (receive.IsFaulted)
				{
					return;
				}
				var ret = receive.Result;
				receive = null;
				if (DiscoveryResponse.TryUnpack(ret.Buffer, ret.Buffer.Length, out var packet))
				{
					lock (m_Room)
					{
						var room = packet.CreateRoom(ret.RemoteEndPoint.Address, m_DiscoveryPort);
						var key = room.Name + ":" + room.Port;
						if (!m_Room.TryGetValue(key, out var entry))
						{
							m_Room[key] = entry = new Entry
							{
								Room = room,
							};
						}
						entry.ReceiveAt = DateTime.UtcNow;
						while (m_WaitNewRooms.Count > 0)
						{
							m_WaitNewRooms.Dequeue().TrySetResult(room);
						}
					}
				}
			}
		}

		public DiscoveryRoom[] GetRooms()
		{
			lock (m_Room)
			{
				return m_Room.Values.Select(x => x.Room).ToArray();
			}
		}

		public async Task<DiscoveryRoom[]> GetRoomsAsync(CancellationToken token = default)
		{
			var rooms = GetRooms();
			if (rooms.Length > 0) return rooms;
			await GetNewRoom(token);
			return GetRooms();
		}

		public DiscoveryRoom GetRoom(string name)
		{
			lock (m_Room)
			{
				foreach (var entry in m_Room.Values)
				{
					if (entry.Room.Name == name)
					{
						return entry.Room;
					}
				}
			}
			return null;
		}

		public async Task<DiscoveryRoom> GetRoomAsync(string name, CancellationToken token = default)
		{
			while (m_Run)
			{
				var room = GetRoom(name);
				if (room != null) return room;
				await GetNewRoom(token);
			}
			throw new Exception("Stop DiscoveryClient");
		}

		async Task<DiscoveryRoom> GetNewRoom(CancellationToken token = default)
		{
			var task = new TaskCompletionSource<DiscoveryRoom>(TaskCreationOptions.RunContinuationsAsynchronously);
			if (default != token)
			{
				token.Register(() => task.TrySetCanceled(token));
			}
			lock (m_Room)
			{
				m_WaitNewRooms.Enqueue(task);
			}
			return await task.Task;
		}


		public void Stop()
		{
			m_Run = false;
			m_Socket?.Dispose();
			lock (m_Room)
			{
				m_Room.Clear();
				while (m_WaitNewRooms.Count > 0)
				{
					m_WaitNewRooms.Dequeue().TrySetException(new Exception("Stop DiscoveryClient"));
				}
			}
		}

		public void Dispose()
		{
			Stop();
			m_Socket = null;
		}
	}

}