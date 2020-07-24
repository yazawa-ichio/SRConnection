using System;
using System.Threading;
using System.Threading.Tasks;

namespace SRNet
{
	internal class TimeoutRetryableRequester<T>
	{
		public int Count { get; set; } = 5;

		public TimeSpan Time { get; set; } = TimeSpan.FromMilliseconds(500);

		Task<T> m_WaitResponse;
		Action m_Request;
		CancellationToken m_Token;

		public TimeoutRetryableRequester(Task<T> waitResponse, Action request, CancellationToken token)
		{
			m_WaitResponse = waitResponse;
			m_Request = request;
			m_Token = token;
		}

		public async Task<T> Run()
		{
			var task = m_WaitResponse;
			for (int i = 0; i < Count; i++)
			{
				m_Request();
				await Task.WhenAny(task, Task.Delay(Time, m_Token));
				if (task.IsCompleted)
				{
					if (task.IsFaulted)
					{
						Log.Warning(task.Exception.ToString());
						continue;
					}
					else
					{
						return task.Result;
					}
				}
			}
			throw new Exception("timeout");
		}
	}

}