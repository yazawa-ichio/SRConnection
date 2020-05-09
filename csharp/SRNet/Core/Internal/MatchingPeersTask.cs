using SRNet.Packet;
using SRNet.Stun;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SRNet
{

	internal class MatchingPeersTask
	{
		static readonly string DefaultStunURL = "stun.l.google.com";

		UdpSocket m_Socket;
		Func<StunResult, Task<P2PSetting>> m_Func;
		string m_StunURL;

		public MatchingPeersTask(string postUrl, string stunURL = null)
		{
			m_Func = (ret) => MatchingRequest(postUrl, ret);
			m_StunURL = stunURL ?? DefaultStunURL;
		}

		public MatchingPeersTask(Func<StunResult, Task<P2PSetting>> func, string stunURL = null)
		{
			m_Func = func;
			m_StunURL = stunURL ?? DefaultStunURL;
		}

		public async Task<P2PConnectionImpl> Run()
		{
			try
			{
				m_Socket = new UdpSocket();
				m_Socket.Bind();
				var stunResult = await m_Socket.StunQuery(m_StunURL, 19302);
				var response = await m_Func(stunResult);
				return new P2PConnectionImpl(response, m_Socket);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				m_Socket.Dispose();
				throw;
			}

		}

		[Serializable]
		internal class Peer
		{
			public int id = 0;
			public string endpoint = null;
			public string local_endpoint = null;
			public string randam = null;

			public PeerInfo Create()
			{
				return new PeerInfo(id, new PeerEndPoint(endpoint), new PeerEndPoint(local_endpoint), Convert.FromBase64String(randam));
			}

		}

		[Serializable]
		internal class Response
		{
			public int id = 0;
			public Peer[] peers = null;
		}

		async Task<P2PSetting> MatchingRequest(string url, StunResult ret)
		{
			var json = $"{{\"endpoint\":\"{ret.EndPoint}\",\"local_endpoint\":\"{ret.LocalEndPoint}\", \"nattype\": \"{ret.NatType}\"}}";
			var content = new StringContent(json);
			using (var client = new HttpClient())
			{
				var response = await client.PostAsync(url, content);
				if (response.StatusCode == HttpStatusCode.OK)
				{
					json = await response.Content.ReadAsStringAsync();
					var res = Json.From<Response>(json);
					return new P2PSetting
					{
						SelfId = res.id,
						Peers = res.peers.Select(x => x.Create()).ToArray()
					};
				}
				else if (response.StatusCode == HttpStatusCode.RequestTimeout)
				{
					throw new Exception("Matching　Timeout");
				}
			}
			throw new Exception("Matching　fail");
		}

	}
}