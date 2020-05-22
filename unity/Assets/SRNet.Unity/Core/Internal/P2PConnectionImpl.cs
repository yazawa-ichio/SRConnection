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
		DiscoveryService m_DiscoveryService;

		protected override bool IsHost => m_IsOwner;

		protected override bool UseP2P => true;

		public bool DisposeOnDisconnectOwnder { get; set; } = true;

		internal P2PConnectionImpl(LocalHostConfig config) : base(new UdpSocket(), new EncryptorGenerator())
		{
			m_IsOwner = true;
			m_CookieProvider.Update();
			m_Socket.Bind(new IPEndPoint(config.Address, 0), false);
			SelfId = Random.GenInt();
			m_P2PTaskManager.CreateHostRandamKey();
			var randamKey = m_P2PTaskManager.GetHostRandamKey();
			var data = new PeerToPeerRoomData(SelfId, m_CookieProvider.Cookie, randamKey).Pack();
			m_DiscoveryService = new DiscoveryService(config.RoomName, m_Socket.LocalEP, data, config.DiscoveryServicePort);
			m_DiscoveryService.OnHolePunchRequest += (ep) =>
			{
				lock (m_Socket)
				{
					var packet = new DiscoveryHolePunch().Pack();
					m_Socket.Send(packet, 0, packet.Length, ep);
				}
			};
			m_DiscoveryService.Start(config.DiscoveryQueryMatch);
		}

		internal P2PConnectionImpl(int selfId, UdpSocket socket, PeerEntry owner, EncryptorGenerator encryptorGenerator) : base(socket, encryptorGenerator)
		{
			SelfId = selfId;
			m_CookieProvider.Update();
			m_Owner = owner;
			m_IsOwner = false;
			m_PeerManager.Add(m_Owner);
		}

		internal P2PConnectionImpl(P2PSettings setting, UdpSocket socket) : base(socket, new EncryptorGenerator())
		{
			m_CookieProvider.Update();
			m_IsOwner = false;
			SelfId = setting.SelfId;
			UpdateConnectPeerList(setting.Peers, true);
		}

		protected override void Dispose(bool disposing)
		{
			m_DiscoveryService?.Dispose();
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
			if (DisposeOnDisconnectOwnder && m_Owner != null && m_Owner.ConnectionId == peer.ConnectionId)
			{
				Dispose();
			}
		}

		public void StopMatching()
		{
			m_DiscoveryService?.Dispose();
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
