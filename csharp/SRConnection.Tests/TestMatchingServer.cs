
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace SRConnection.Tests
{

	[Serializable]
	public class Request
	{
		public string endpoint = null;
		public string local_endpoint = null;
		public string nattype = null;
	}

	public class Response
	{
		public class Peer
		{
			public int id;
			public string endpoint;
			public string local_endpoint;
			public string randam;
		}

		public int id;
		public Peer[] peers;
	}


	public class TimeoutException : Exception
	{
		public TimeoutException() : base("Timeout") { }
	}

	public class TestMatchingServer : IDisposable
	{
		public class Entry
		{
			public int Id;
			public int Salt;
			public Request Request;
			public TaskCompletionSource<Response> TaskCompletionSource = new TaskCompletionSource<Response>();
		}

		Thread m_MatchingLoop;
		Thread m_ListenerLoop;
		bool m_Dispose;
		BlockingCollection<Entry> m_Request = new BlockingCollection<Entry>();
		HMAC m_Hmac;
		byte[] m_HmacInput = new byte[4 * 4];
		int m_MaxMember = 4;
		TimeSpan m_Timeout = TimeSpan.FromSeconds(3);
		CancellationTokenSource m_Cancellation = new CancellationTokenSource();
		HttpListener m_Listener = new HttpListener();

		public void Start()
		{
			m_Hmac = new HMACSHA256();
			m_MatchingLoop = new Thread(MatchingLoop);
			m_MatchingLoop.Start();
			m_ListenerLoop = new Thread(ListenerLoop);
			m_ListenerLoop.Start();
		}


		void MatchingLoop()
		{
			try
			{
				List<Entry> matching = new List<Entry>();
				while (!m_Dispose)
				{
					var first = m_Request.Take(m_Cancellation.Token);
					matching.Add(first);
					var start = DateTime.UtcNow;

					while (matching.Count < m_MaxMember && !IsTimeout(start) && !m_Dispose)
					{
						if (m_Request.TryTake(out var entry, m_Timeout - (start - DateTime.UtcNow)))
						{
							matching.Add(entry);
						}
					}

					if (m_Dispose) return;

					if (matching.Count == 1)
					{
						var entry = matching[0];
						matching.Clear();
						entry.TaskCompletionSource.TrySetException(new TimeoutException());
					}
					else
					{
						var entries = matching.ToArray();
						matching.Clear();
						Matching(entries);
					}
				}
			}
			catch
			{
				if (m_Dispose) return;
				throw;
			}
		}

		bool IsTimeout(in DateTime start)
		{
			return (DateTime.UtcNow - start) > m_Timeout - TimeSpan.FromMilliseconds(100);
		}

		void Matching(Entry[] entries)
		{
			foreach (var entry in entries)
			{
				entry.Id = Random.GenInt();
				entry.Salt = Random.GenInt();
			}

			foreach (var target in entries)
			{
				var res = new Response()
				{
					id = target.Id,
					peers = new Response.Peer[entries.Length],
				};
				for (int i = 0; i < entries.Length; i++)
				{
					Entry entry = entries[i];
					Entry left = target.Salt < entry.Salt ? entry : target;
					Entry right = target.Salt < entry.Salt ? target : entry;
					int offset = 0;
					BinaryUtil.Write(left.Id, m_HmacInput, ref offset);
					BinaryUtil.Write(left.Salt, m_HmacInput, ref offset);
					BinaryUtil.Write(right.Id, m_HmacInput, ref offset);
					BinaryUtil.Write(right.Salt, m_HmacInput, ref offset);
					var randam = m_Hmac.ComputeHash(m_HmacInput);
					res.peers[i] = new Response.Peer
					{
						id = entry.Id,
						endpoint = entry.Request.endpoint,
						local_endpoint = entry.Request.local_endpoint,
						randam = Convert.ToBase64String(randam, 0, 22),
					};
				}
				target.TaskCompletionSource.SetResult(res);
			}
		}

		void ListenerLoop()
		{
			try
			{
				m_Listener.Prefixes.Add("http://localhost:8080/");
				m_Listener.Start();
				while (!m_Dispose)
				{
					var context = m_Listener.GetContextAsync().Result;
					Request(context);
				}
			}
			catch
			{
				if (m_Dispose) return;
				throw;
			}
		}

		async void Request(HttpListenerContext context)
		{
			try
			{
				var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
				var json = await reader.ReadToEndAsync();
				var req = Json.From<Request>(json);
				var entry = new Entry { Request = req };
				m_Request.Add(entry);
				var res = await entry.TaskCompletionSource.Task;
				using var w = new StreamWriter(context.Response.OutputStream);
				context.Response.StatusCode = (int)HttpStatusCode.OK;
				await w.WriteLineAsync(Json.To(res));
			}
			catch (TimeoutException ex)
			{
				using var w = new StreamWriter(context.Response.OutputStream);
				context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
				await w.WriteLineAsync(ex.Message);
			}
			catch
			{
				context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				context.Response.Close();
			}
		}

		public void Dispose()
		{
			if (m_Dispose) return;
			m_Dispose = true;
			m_Request.Dispose();
			m_Listener.Close();
			m_Cancellation.Cancel();
			m_MatchingLoop.Join();
			m_ListenerLoop.Join();
		}
	}

}