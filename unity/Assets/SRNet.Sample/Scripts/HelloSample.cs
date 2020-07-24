using System;
using System.Net;
using System.Text;
using UnityEngine;

namespace SRNet.Sample
{
	public class HelloSample : MonoBehaviour
	{
		const int Port = 54809;

		Connection m_Server;
		ClientConnection m_Client;
		string m_ServerMessage;
		bool m_Destroy;

		void Awake()
		{
			Log.Init();
			Log.Level = Log.LogLevel.Trace;
			var config = ServerConfig.FromXML(Resources.Load<TextAsset>("private").text, Port);
			m_Server = Connection.StartServer(config);
			Connect();
		}

		void OnDestroy()
		{
			m_Destroy = true;
			m_Server?.Dispose();
			m_Client?.Dispose();
		}

		async void Connect()
		{
			try
			{
				var ep = new IPEndPoint(IPAddress.Loopback, Port);
				var settings = ServerConnectSettings.FromXML(Resources.Load<TextAsset>("public").text, ep);
				m_Client = await Connection.Connect(settings);
				if (m_Destroy)
				{
					m_Client.Dispose();
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		void Update()
		{
			while (m_Server.TryReceive(out var message))
			{
				var text = Encoding.UTF8.GetString(message);
				var buf = Encoding.UTF8.GetBytes("Hello " + text);
				message.ResponseTo(buf);
			}
			while (m_Client != null && m_Client.TryReceive(out var message))
			{
				m_ServerMessage = Encoding.UTF8.GetString(message);
				Debug.Log("From " + message.Peer.ConnectionId + " : " + m_ServerMessage);
			}
		}

		string m_Message = "TestMessage";
		void OnGUI()
		{
			Vector3 scale = new Vector3(2, 2, 1.0f);
			GUI.matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, scale);

			if (m_Client == null)
			{
				GUILayout.Label("Connect...");
				return;
			}

			GUILayout.Label("ConnectId:" + m_Client.SelfId);

			GUILayout.Label("Input Message");
			m_Message = GUILayout.TextField(m_Message);

			if (GUILayout.Button("Send"))
			{
				var buf = Encoding.UTF8.GetBytes(m_Message);
				m_Client.Server.Send(buf);
			}

			GUILayout.Label("From Server:" + m_ServerMessage);

		}


	}
}