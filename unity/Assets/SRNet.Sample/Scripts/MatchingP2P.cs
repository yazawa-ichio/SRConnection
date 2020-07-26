using ILib.FSM;
using ILib.FSM.Provider;
using System;
using UnityEngine;

namespace SRNet.Sample
{
	public class MatchingP2P : MonoBehaviour
	{
		public Connection Conn { get; set; }
		StateMachine m_StateMachine;

		void Start()
		{
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

		class StateBase : State
		{
			public new MatchingP2P Owner => base.Owner as MatchingP2P;

			public virtual void OnGUI() { }
		}

		enum Event
		{
			Boot,
			Connect,
			Run,
			Error,
		}

		[DirectTransition(Event.Boot, UseInit = true)]
		class Boot : StateBase
		{
			string m_URL = "http://localhost:8080/";

			protected override void OnEnter()
			{
				Owner.Conn?.Dispose();
				Owner.Conn = null;
			}

			public override void OnGUI()
			{
				m_URL = GUILayout.TextField(m_URL);
				if (GUILayout.Button("Connect"))
				{
					Transition(Event.Connect, m_URL);
				}
			}
		}

		[DirectTransition(Event.Connect)]
		class Connect : StateBase
		{
			protected override async void OnEnter()
			{
				try
				{
					Owner.Conn = await Connection.P2PMatching(Param.ToString());
					Transition(Event.Run, Owner.Conn);
				}
				catch (Exception ex)
				{
					Transition(Event.Error, ex);
				}
			}

			public override void OnGUI()
			{
				GUILayout.Label("Connect");
				GUILayout.Label(Param.ToString());
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
					while (m_Connection.Update(out var message))
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

				foreach (var peer in m_Connection.Peers)
				{
					if (GUILayout.Button("Send:" + peer.ConnectionId))
					{
						peer.Send(System.Text.Encoding.UTF8.GetBytes("Message" + System.DateTime.Now));
					}
				}

				if (GUILayout.Button("Broadcast"))
				{
					foreach (var peer in m_Connection.Peers)
					{
						peer.Send(System.Text.Encoding.UTF8.GetBytes("Message" + System.DateTime.Now));
					}
				}
			}
		}

		[DirectTransition(Event.Error)]
		class Error : StateBase
		{
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