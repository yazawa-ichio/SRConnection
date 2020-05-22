using SRNet.Packet;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace SRNet
{
	internal class PeerToPeerTaskManager
	{
		ConnectionImpl m_Connection;
		PeerManager m_PeerManager;
		List<ConnectToPeerTask> m_ConnectToPeerTaskList = new List<ConnectToPeerTask>();
		TaskCompletionSource<bool> m_ConnectToPeerComplete;
		EncryptorGenerator m_EncryptorGenerator;
		CookieProvider m_CookieProvider;
		PeerToPeerList m_P2PList;
		PeerInfo[] m_PeerInfoList = Array.Empty<PeerInfo>();
		byte[] m_RandamKey;

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
			}
			if (peer != null)
			{
				lock (m_Connection.m_Socket)
				{
					var size = new PeerToPeerAccept(m_Connection.SelfId, peer.ClientConnectionId).Pack(m_Connection.m_SendBuffer, peer.Encryptor);
					m_Connection.m_Socket.Send(m_Connection.m_SendBuffer, 0, size, remoteEP);
				}
				m_Connection.SendPeerToPeerList();
			}
			lock (m_ConnectToPeerTaskList)
			{
				for (int i = m_ConnectToPeerTaskList.Count - 1; i >= 0; i--)
				{
					if (packet.ConnectionId != m_ConnectToPeerTaskList[i].ConnectionId)
					{
						continue;
					}
					m_ConnectToPeerTaskList.RemoveAt(i);
				}
				if (m_ConnectToPeerTaskList.Count == 0)
				{
					if (m_ConnectToPeerComplete != null)
					{
						m_ConnectToPeerComplete.TrySetResult(true);
						m_ConnectToPeerComplete = null;
					}
				}
			}
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
			lock (m_ConnectToPeerTaskList)
			{
				foreach (var task in m_ConnectToPeerTaskList)
				{
					if (packet.ConnectionId != task.ConnectionId)
					{
						continue;
					}
					task.OnPeerToPeerHello(packet, remoteEP);
				}
			}
		}

		public void HandshakeComplete(byte[] buf, int size, IPEndPoint remoteEP)
		{
			int offset = 1;
			int connectionId = BinaryUtil.ReadInt(buf, ref offset);
			lock (m_ConnectToPeerTaskList)
			{
				for (int i = m_ConnectToPeerTaskList.Count - 1; i >= 0; i--)
				{
					if (connectionId != m_ConnectToPeerTaskList[i].ConnectionId)
					{
						continue;
					}
					if (m_ConnectToPeerTaskList[i].OnHandshakeAccept(buf, size, out PeerEntry peer, remoteEP))
					{
						m_ConnectToPeerTaskList.RemoveAt(i);
						m_PeerManager.Add(peer);
					}
				}
				if (m_ConnectToPeerTaskList.Count == 0)
				{
					if (m_ConnectToPeerComplete != null)
					{
						m_ConnectToPeerComplete.TrySetResult(true);
						m_ConnectToPeerComplete = null;
					}
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

		List<PeerInfo> m_TempList = new List<PeerInfo>();
		public void UpdateList(PeerInfo[] list, bool init)
		{
			if (init)
			{
				UpdateListImpl(list);
			}
			else
			{
				m_TempList.Clear();
				m_TempList.AddRange(m_PeerInfoList);
				foreach (var info in list)
				{
					if (Array.Exists(m_PeerInfoList, x => info.ConnectionId == x.ConnectionId))
					{
						continue;
					}
					m_TempList.Add(info);
				}
				var newList = m_TempList.ToArray();
				m_TempList.Clear();
				UpdateListImpl(newList);
			}
		}

		public void Add(PeerInfo info)
		{
			if (Array.Exists(m_PeerInfoList, x => info.ConnectionId == x.ConnectionId))
			{
				return;
			}
			Array.Resize(ref m_PeerInfoList, m_PeerInfoList.Length + 1);
			m_PeerInfoList[m_PeerInfoList.Length - 1] = info;
			UpdateListImpl(m_PeerInfoList);
		}

		public void Remove(int connectionId)
		{
			m_TempList.Clear();
			foreach (var info in m_PeerInfoList)
			{
				if (connectionId == info.ConnectionId)
				{
					continue;
				}
				m_TempList.Add(info);
			}
			var newList = m_TempList.ToArray();
			m_TempList.Clear();
			UpdateListImpl(newList);
		}

		void UpdateListImpl(PeerInfo[] list)
		{
			m_PeerInfoList = list;
			bool requestFlag = true;
			foreach (var info in m_PeerInfoList)
			{
				if (m_PeerManager.TryGetValue(info.ConnectionId, out PeerEntry peer))
				{
					if (peer.EndPoint.Port != info.EndPont.Port && peer.EndPoint.Address.ToString() != info.EndPont.Address)
					{
						peer.EndPoint = info.EndPont.To();
					}
					continue;
				}
				//接続リクエストはリストの自分よりも上のPeerに対して行う
				if (info.ConnectionId == m_Connection.SelfId)
				{
					requestFlag = false;
					continue;
				}
				bool hit = false;
				lock (m_ConnectToPeerTaskList)
				{
					foreach (var task in m_ConnectToPeerTaskList)
					{
						if (task.ConnectionId == info.ConnectionId)
						{
							task.UpdateInfo(info);
							hit = true;
							break;
						}
					}
					if (!hit)
					{
						m_ConnectToPeerTaskList.Add(new ConnectToPeerTask(m_Connection, info, requestFlag));
					}
				}
			}
			lock (m_ConnectToPeerTaskList)
			{
				for (int i = m_ConnectToPeerTaskList.Count - 1; i >= 0; i--)
				{
					bool hit = false;
					foreach (var info in list)
					{
						if (info.ConnectionId == m_ConnectToPeerTaskList[i].ConnectionId)
						{
							hit = true;
							break;
						}
					}
					if (!hit)
					{
						m_ConnectToPeerTaskList[i].Dispose();
						m_ConnectToPeerTaskList.RemoveAt(i);
					}
				}
				if (m_ConnectToPeerTaskList.Count > 0)
				{
					if (m_ConnectToPeerComplete == null)
					{
						m_ConnectToPeerComplete = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
					}
				}
				else
				{
					if (m_ConnectToPeerComplete != null)
					{
						m_ConnectToPeerComplete.TrySetResult(true);
						m_ConnectToPeerComplete = null;
					}
				}
			}
		}

		public async Task WaitTaskComplete()
		{
			var tempTask = m_ConnectToPeerComplete;
			if (tempTask == null)
			{
				await Task.Delay(100);
			}
			Task<bool> task = null;
			lock (m_ConnectToPeerTaskList)
			{
				task = m_ConnectToPeerComplete?.Task ?? Task.FromResult(true);
			}
			await task;
		}

	}

}
