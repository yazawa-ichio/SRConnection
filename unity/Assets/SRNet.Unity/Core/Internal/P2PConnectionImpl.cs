using SRNet.Packet;
using System;
using System.Net;

namespace SRNet
{

	internal class P2PConnectionImpl : ConnectionImpl
	{
		PeerEntry m_Owner;
		bool m_IsOwner;
		bool m_PeerToPeerListDirty = true;
		PeerToPeerList m_PeerToPeerList = default;

		public DiscoveryService DiscoveryService { get; private set; }

		protected override bool IsHost => m_IsOwner;

		protected override bool UseP2P => true;

		internal P2PConnectionImpl(string roomName, IPAddress address) : base(new UdpSocket(), new EncryptorGenerator())
		{
			m_CookieProvider.Update();
			m_Socket.Bind(new IPEndPoint(address, 0), false);
			SelfId = Random.GenInt();
			m_P2PTaskManager.CreateHostRandamKey();
			var randamKey = m_P2PTaskManager.GetHostRandamKey();
			var data = new PeerToPeerRoomData(SelfId, m_CookieProvider.Cookie, randamKey).Pack();
			DiscoveryService = new DiscoveryService(roomName, m_Socket.LocalEP, data);
			DiscoveryService.OnHolePunchRequest += (ep) =>
			{
				lock (m_Socket)
				{
					var packet = new DiscoveryHolePunch().Pack();
					m_Socket.Send(packet, 0, packet.Length, ep);
				}
			};
			m_IsOwner = true;
		}

		internal P2PConnectionImpl(int selfId, UdpSocket socket, PeerEntry owner, EncryptorGenerator encryptorGenerator) : base(socket, encryptorGenerator)
		{
			SelfId = selfId;
			m_CookieProvider.Update();
			m_Owner = owner;
			m_PeerManager.Add(m_Owner);
		}

		internal P2PConnectionImpl(P2PSetting setting, UdpSocket socket) : base(socket, new EncryptorGenerator())
		{
			m_CookieProvider.Update();
			m_IsOwner = false;
			SelfId = setting.SelfId;
			UpdateConnectPeerList(setting.Peers);
		}

		protected override void Dispose(bool disposing)
		{
			DiscoveryService?.Dispose();
			BroadcastDisconnect();
			base.Dispose(disposing);
		}

		protected internal override void OnAdd(PeerEntry peer)
		{
			m_PeerToPeerListDirty = true;
			base.OnAdd(peer);
		}

		protected internal override void OnRemove(PeerEntry peer)
		{
			base.OnRemove(peer);
			m_PeerToPeerListDirty = true;
			if (!m_IsOwner && m_Owner.ConnectionId == peer.ConnectionId)
			{
				Dispose();
			}
		}

		protected override void TimerUpdate(TimeSpan delta)
		{
			base.TimerUpdate(delta);
			if (IsHost && !Disposed)
			{
				UpdateP2PList();
			}
		}

		public override void SendPeerToPeerList()
		{
			if (IsHost && !Disposed)
			{
				UpdateP2PList();
			}
		}

		void SendP2PList(PeerEntry peer, PeerToPeerList data)
		{
			var offset = data.Pack(m_SendBuffer, peer.Encryptor);
			m_Socket.Send(m_SendBuffer, 0, offset, peer.EndPoint);
		}

		Action<PeerEntry, PeerToPeerList> m_SendP2PList;
		void UpdateP2PList()
		{
			if (m_SendP2PList == null) m_SendP2PList = SendP2PList;

			if (m_PeerToPeerListDirty)
			{
				m_PeerToPeerListDirty = false;
				var revision = m_PeerToPeerList.Revision;
				revision++;
				var randamKey = m_P2PTaskManager.GetHostRandamKey();
				m_PeerToPeerList = new PeerToPeerList(SelfId, revision, m_PeerManager.CreatePeerInfoList(randamKey));
			}
			lock (m_Socket)
			{
				m_PeerManager.ForEach(m_SendP2PList, m_PeerToPeerList);
			}
		}

	}

}
