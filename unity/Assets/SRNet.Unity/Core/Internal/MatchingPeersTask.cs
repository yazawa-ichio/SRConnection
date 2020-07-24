using SRNet.Packet;
using SRNet.Stun;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SRNet
{

	internal class MatchingPeersTask
	{
		static readonly string DefaultStunURL = "stun.l.google.com";

		UdpSocket m_Socket;
		Func<StunResult, CancellationToken, Task<P2PSettings>> m_Func;
		string m_StunURL;
		CancellationToken m_Token;

		public MatchingPeersTask(string postUrl, string stunURL, CancellationToken token)
		{
			m_Func = (ret, t) => MatchingRequest(postUrl, ret, t);
			m_StunURL = stunURL ?? DefaultStunURL;
			m_Token = token;
		}

		public MatchingPeersTask(Func<StunResult, CancellationToken, Task<P2PSettings>> func, string stunURL, CancellationToken token)
		{
			m_Func = func;
			m_StunURL = stunURL ?? DefaultStunURL;
			m_Token = token;
		}

		public async Task<P2PConnectionImpl> Run()
		{
			try
			{
				m_Socket = new UdpSocket();
				m_Socket.Bind();
				var stunResult = await StunClient.Run(m_Socket.m_UdpClient, m_StunURL, 19302, m_Token);
				var response = await m_Func(stunResult, m_Token);

				m_Token.ThrowIfCancellationRequested();

				return new P2PConnectionImpl(response, m_Socket);
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
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

		async Task<P2PSettings> MatchingRequest(string url, StunResult ret, CancellationToken token)
		{
			var json = $"{{\"endpoint\":\"{ret.EndPoint}\",\"local_endpoint\":\"{ret.LocalEndPoint}\", \"nattype\": \"{ret.NatType}\"}}";
			var content = new StringContent(json);
			using (var client = new HttpClient())
			{
				var response = await client.PostAsync(url, content, token);
				if (response.StatusCode == HttpStatusCode.OK)
				{
					json = await response.Content.ReadAsStringAsync();
					var res = Json.From<Response>(json);
					return new P2PSettings
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