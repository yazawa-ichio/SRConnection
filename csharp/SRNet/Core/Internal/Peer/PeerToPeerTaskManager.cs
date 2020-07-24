using SRNet.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SRNet
{
	internal class PeerToPeerTaskManager
	{
		ConnectionImpl m_Connection;
		PeerManager m_PeerManager;
		List<ConnectToPeerTask> m_ConnectToPeerTaskList = new List<ConnectToPeerTask>();
		EncryptorGenerator m_EncryptorGenerator;
		CookieProvider m_CookieProvider;
		PeerToPeerList m_P2PList;
		PeerInfo[] m_PeerInfoList = Array.Empty<PeerInfo>();
		byte[] m_RandamKey;
		Queue<Action> m_Complete = new Queue<Action>();

		public PeerToPeerTaskManager(ConnectionImpl connection, CookieProvider cookieProvider, PeerManager manager)
		{
			m_Connection = connection;
			m_EncryptorGenerator = connection.m_EncryptorGenerator;
			m_CookieProvider = cookieProvider;
			m_PeerManager = manager;
		}

		public void CreateHostRandamKey()
		{
			m_RandamKey = Random.GenBytes(EncryptorKey.RandamKey);
		}

		public byte[] GetHostRandamKey()
		{
			return m_RandamKey;
		}

		public void Update(TimeSpan delta)
		{
			lock (m_ConnectToPeerTaskList)
			{
				foreach (var task in m_ConnectToPeerTaskList)
				{
					task.Update(delta);
				}
			}
		}

		ConnectToPeerTask GetTask(int id)
		{
			lock (m_ConnectToPeerTaskList)
			{
				foreach (var task in m_ConnectToPeerTaskList)
				{
					if (task.ConnectionId == id)
					{
						return task;
					}
				}
				return null;
			}
		}

		void RemoveTask(int id, bool cancel = false)
		{
			lock (m_ConnectToPeerTaskList)
			{
				for (int i = m_ConnectToPeerTaskList.Count - 1; i >= 0; i--)
				{
					var task = m_ConnectToPeerTaskList[i];
					if (id != task.ConnectionId)
					{
						continue;
					}
					if (cancel)
					{
						task.Dispose();
					}
					m_ConnectToPeerTaskList.RemoveAt(i);
				}
				if (m_ConnectToPeerTaskList.Count == 0)
				{
					while (m_Complete.Count > 0) m_Complete.Dequeue()();
				}
			}
		}

		public void HandshakeAccept(PeerToPeerRequest packet, IPEndPoint remoteEP)
		{
			if (!m_PeerManager.TryGetValue(packet.ConnectionId, out var peer))
			{
				int nonce = Random.GenInt();
				var randamKey = m_RandamKey ?? GetPeerRandamKey(packet.ConnectionId);
				var key = new EncryptorKey(packet, randamKey, nonce);
				var encryptor = m_EncryptorGenerator.Generate(in key);
				peer = new PeerEntry(packet.ConnectionId, nonce, encryptor, remoteEP);
				m_PeerManager.Add(peer);
				m_Connection.SendPeerToPeerList();
			}
			lock (m_Connection.m_Socket)
			{
				if (m_Connection.TryGetPeerToPeerList(out var list))
				{
					var size = new PeerToPeerAccept(m_Connection.SelfId, peer.ClientConnectionId, list).Pack(m_Connection.m_SendBuffer, peer.Encryptor);
					m_Connection.m_Socket.Send(m_Connection.m_SendBuffer, 0, size, remoteEP);
				}
				else
				{
					var size = new PeerToPeerAccept(m_Connection.SelfId, peer.ClientConnectionId).Pack(m_Connection.m_SendBuffer, peer.Encryptor);
					m_Connection.m_Socket.Send(m_Connection.m_SendBuffer, 0, size, remoteEP);
				}
			}
			RemoveTask(packet.ConnectionId);
		}

		public void OnPeerToPeerHello(PeerToPeerHello packet, IPEndPoint remoteEP)
		{
			if (packet.CookieSize > 0)
			{
				StartHandshake(packet, remoteEP);
			}
			else
			{
				//こちらのCookieを返す
				lock (m_Connection.m_Socket)
				{
					var size = new PeerToPeerHello(m_Connection.SelfId, m_CookieProvider.Cookie).Pack(m_Connection.m_SendBuffer);
					m_Connection.m_Socket.Send(m_Connection.m_SendBuffer, 0, size, remoteEP);
				}
			}
		}

		void StartHandshake(PeerToPeerHello packet, IPEndPoint remoteEP)
		{
			GetTask(packet.ConnectionId)?.OnPeerToPeerHello(packet, remoteEP);
		}

		public void HandshakeComplete(byte[] buf, int size, IPEndPoint remoteEP)
		{
			int offset = 1;
			int connectionId = BinaryUtil.ReadInt(buf, ref offset);
			lock (m_ConnectToPeerTaskList)
			{
				PeerEntry peer = null;
				if (GetTask(connectionId)?.OnHandshakeAccept(buf, size, out peer, remoteEP) ?? false)
				{
					m_PeerManager.Add(peer);
					RemoveTask(connectionId);
				}
			}
		}

		public void OnPeerToPeerList(byte[] buf, int size)
		{
			int offset = 1;
			var connectionId = BinaryUtil.ReadInt(buf, ref offset);
			var revision = buf[offset++];
			if (revision == m_P2PList.Revision)
			{
				return;
			}

			if (!m_PeerManager.TryGetValue(connectionId, out PeerEntry peer))
			{
				return;
			}

			if (PeerToPeerList.TryUnpack(buf, size, peer.Encryptor, out var packet))
			{
				m_P2PList = packet;
				m_Connection.UpdateConnectPeerList(packet.Peers, true);
			}
		}

		byte[] GetPeerRandamKey(int id)
		{
			var list = m_PeerInfoList;
			if (list == null) return null;
			foreach (var info in list)
			{
				if (info.ConnectionId == id)
				{
					return info.Randam;
				}
			}
			return null;
		}


		public void UpdateList(PeerInfo[] list, bool init)
		{
			if (init)
			{
				UpdateListImpl(list);
			}
			else
			{
				foreach (var info in list)
				{
					Add(info);
				}
			}
		}

		public void Add(PeerInfo info)
		{
			if (m_PeerInfoList.Any(x => x.ConnectionId == info.ConnectionId))
			{
				return;
			}
			Array.Resize(ref m_PeerInfoList, m_PeerInfoList.Length + 1);
			m_PeerInfoList[m_PeerInfoList.Length - 1] = info;
			UpdateListImpl(m_PeerInfoList);
		}

		public void Remove(int connectionId)
		{
			UpdateListImpl(m_PeerInfoList.Where(x => x.ConnectionId != connectionId).ToArray());
		}

		void UpdateListImpl(PeerInfo[] list)
		{
			m_PeerInfoList = list;
			lock (m_ConnectToPeerTaskList)
			{
				bool requestFlag = true;
				var removeList = new HashSet<int>(m_ConnectToPeerTaskList.Select(x => x.ConnectionId));
				foreach (var info in m_PeerInfoList)
				{
					removeList.Remove(info.ConnectionId);
					if (m_PeerManager.TryGetValue(info.ConnectionId, out PeerEntry peer))
					{
						peer.Update(info);
						continue;
					}
					//接続リクエストはリストの自分よりも上のPeerに対して行う
					if (info.ConnectionId == m_Connection.SelfId)
					{
						requestFlag = false;
						continue;
					}
					var task = GetTask(info.ConnectionId);
					task?.UpdateInfo(info);
					if (task == null)
					{
						m_ConnectToPeerTaskList.Add(new ConnectToPeerTask(m_Connection, info, requestFlag));
					}
				}
				foreach (var id in removeList)
				{
					RemoveTask(id, true);
				}
			}
		}

		public Task WaitTaskComplete()
		{
			lock (m_ConnectToPeerTaskList)
			{
				if (m_ConnectToPeerTaskList.Count == 0)
				{
					return Task.FromResult(true);
				}
				var future = new TaskCompletionSource<bool>();
				m_Complete.Enqueue(() =>
				{
					future.TrySetResult(true);
				});
				return future.Task;
			}
		}

	}

}