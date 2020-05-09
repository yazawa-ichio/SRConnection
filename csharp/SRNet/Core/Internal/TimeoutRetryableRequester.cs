using System;
using System.Threading.Tasks;

namespace SRNet
{
	internal class TimeoutRetryableRequester<T>
	{
		public int Count { get; set; } = 5;

		public TimeSpan Time { get; set; } = TimeSpan.FromMilliseconds(500);

		Task<T> m_WaitResponse;
		Action m_Request;

		public TimeoutRetryableRequester(Task<T> waitResponse, Action request)
		{
			m_WaitResponse = waitResponse;
			m_Request = request;
		}

		public async Task<T> Run()
		{
			var task = m_WaitResponse;
			for (int i = 0; i < Count; i++)
			{
				m_Request();
				await Task.WhenAny(task, Task.Delay(Time));
				if (task.IsCompleted)
				{
					if (task.IsFaulted)
					{
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