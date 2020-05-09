using ILib.FSM;
using ILib.FSM.Provider;
using System;
using UnityEngine;

namespace SRNet.Sample
{
	public class LocalP2P : MonoBehaviour
	{
		public Connection Conn { get; set; }
		StateMachine m_StateMachine;

		void Start()
		{
			Log.Init();
			m_StateMachine = StateMachineProvider<StateBase>.Create(this);
		}

		void OnDestroy()
		{
			Conn?.Dispose();
		}

		void Update()
		{
			m_StateMachine.Update();
		}

		void OnGUI()
		{
			Vector3 scale = new Vector3(2, 2, 1.0f);
			GUI.matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, scale);

			m_StateMachine.Execute<StateBase>(state =>
			{
				state.OnGUI();
			});

		}

		enum Event
		{
			Boot,
			CreateRoom,
			StartRoom,
			SelectRoom,
			ConnectRoom,
			Run,
			Error,
		}

		class StateBase : State
		{
			public new LocalP2P Owner => base.Owner as LocalP2P;

			public virtual void OnGUI() { }
		}

		[DirectTransition(Event.Boot, UseInit = true)]
		class Boot : StateBase
		{
			protected override void OnEnter()
			{
				Owner.Conn?.Dispose();
				Owner.Conn = null;
			}

			public override void OnGUI()
			{
				if (GUILayout.Button("CreateRoom"))
				{
					Transition(Event.CreateRoom);
				}
				if (GUILayout.Button("SelectRoom"))
				{
					Transition(Event.SelectRoom);
				}
			}
		}

		[DirectTransition(Event.CreateRoom)]
		class CreateRoom : StateBase
		{
			string m_Room = System.Environment.UserName;

			public override void OnGUI()
			{
				GUILayout.Label("Room");
				m_Room = GUILayout.TextField(m_Room);
				if (GUILayout.Button("Start"))
				{
					Transition(Event.StartRoom, m_Room);
				}
			}
		}

		[DirectTransition(Event.StartRoom)]
		class StartRoom : StateBase
		{
			protected override async void OnEnter()
			{
				try
				{
					var connection = await Connection.StartHost(Param.ToString());
					connection.DiscoveryService.Start();
					Owner.Conn = connection;
					Transition(Event.Run, connection);
				}
				catch (Exception ex)
				{
					Transition(Event.Error, ex);
				}
			}

			public override void OnGUI()
			{
				GUILayout.Label("Start Room" + Param);
			}
		}

		[DirectTransition(Event.SelectRoom)]
		class SelectRoom : StateBase
		{
			DiscoveryClient m_DiscoveryClient;

			protected override void OnEnter()
			{
				if (m_DiscoveryClient != null)
				{
					m_DiscoveryClient.Dispose();
				}
				m_DiscoveryClient = new DiscoveryClient();
				m_DiscoveryClient.Start();
			}

			protected override void OnExit()
			{
				if (m_DiscoveryClient != null)
				{
					m_DiscoveryClient.Dispose();
					m_DiscoveryClient = null;
				}
			}

			public override void OnGUI()
			{
				GUILayout.Label("Room");
				foreach (var room in m_DiscoveryClient.GetRooms())
				{
					if (GUILayout.Button(room.Name + ":" + room.Address))
					{
						Transition(Event.ConnectRoom, room);
						return;
					}
				}
				if (GUILayout.Button("Back"))
				{
					Transition(Event.Boot);
					return;
				}
			}

		}

		[DirectTransition(Event.ConnectRoom)]
		class ConnectRoom : StateBase
		{
			protected override async void OnEnter()
			{
				try
				{
					var connection = await Connection.Connect((DiscoveryRoom)Param);
					Owner.Conn = connection;
					Transition(Event.Run, connection);
				}
				catch (Exception ex)
				{
					Transition(Event.Error, ex);
				}
			}
		}

		[DirectTransition(Event.Run)]
		class Main : StateBase
		{
			bool m_Run;
			Connection m_Connection;
			string m_Receive = "";

			protected override void OnEnter()
			{
				m_Run = true;
				m_Receive = "";
				m_Connection = Param as Connection;
			}

			protected override void OnExit()
			{
				m_Run = false;
				if (m_Connection != null)
				{
					m_Connection.Dispose();
					m_Connection = null;
					Owner.Conn = null;
				}
			}

			protected override void OnUpdate()
			{
				try
				{
					while (m_Connection.TryReceive(out var message))
					{
						m_Receive = "From" + message.Peer.ConnectionId + "Receive:" + System.Text.Encoding.UTF8.GetString(message);
					}
					if (m_Connection == null)
					{
						Transition(Event.Error, new NullReferenceException("connection null"));
					}
				}
				catch (Exception ex)
				{
					if (m_Run)
					{
						Transition(Event.Error, ex);
					}
				}
			}

			public override void OnGUI()
			{
				if (m_Connection == null) return;

				if (GUILayout.Button("Back"))
				{
					Transition(Event.Boot);
					return;
				}

				GUILayout.Label(m_Receive);

				foreach (var peer in m_Connection.GetPeers())
				{
					if (GUILayout.Button("Send:" + peer.ConnectionId))
					{
						peer.Send(System.Text.Encoding.UTF8.GetBytes("Message" + System.DateTime.Now));
					}
				}

				if (GUILayout.Button("Broadcast"))
				{
					foreach (var peer in m_Connection.GetPeers())
					{
						peer.Send(System.Text.Encoding.UTF8.GetBytes("Message" + System.DateTime.Now));
					}
				}
			}
		}

		[DirectTransition(Event.Error)]
		class Error : StateBase
		{
			protected override void OnEnter()
			{
				Owner.Conn?.Dispose();
				Owner.Conn = null;
			}

			public override void OnGUI()
			{
				if (GUILayout.Button("Reset"))
				{
					Transition(Event.Boot);
				}
				GUILayout.Label(Param?.ToString() ?? "Error");
			}
		}

	}
}