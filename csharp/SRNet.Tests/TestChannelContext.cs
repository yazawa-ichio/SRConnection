using Microsoft.VisualStudio.TestTools.UnitTesting;
using SRNet.Channel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SRNet.Tests
{
	public class TestChannelContext : IChannelContext
	{
		byte[] IChannelContext.SharedSendBuffer { get; } = new byte[Fragment.Size + 100];

		public Queue<byte[]> SentQueue = new Queue<byte[]>();
		System.Random m_Random = new System.Random();

		public bool Send(int connectionId, byte[] buf, int offset, int size, bool encrypt)
		{
			byte[] data = new byte[size];
			Buffer.BlockCopy(buf, offset, data, 0, size);
			SentQueue.Enqueue(data);
			return true;
		}

		public void ShuffleSentQueue()
		{
			var tmp = new List<byte[]>(SentQueue);
			SentQueue.Clear();
			foreach (var buf in tmp.OrderBy(x => Guid.NewGuid()))
			{
				SentQueue.Enqueue(buf);
			}
		}

		public bool Receive(UnreliableFlowControl control, out List<Fragment> output)
		{
			output = new List<Fragment>();
			while (SentQueue.Count > 0)
			{
				var receive = SentQueue.Dequeue();
				int offset = 0;
				if (!UnreliableData.TryUnpack(receive, ref offset, out var data))
				{
					Assert.Fail();
				}
				control.Enqueue(data);
				if (control.TryDequeue(output))
				{
					return true;
				}
			}
			return false;
		}

		public bool Receive(ReliableFlowControl control, out List<Fragment> output, double loss = 0)
		{
			int sendCount = 0;
			int lossCount = 0;
			output = new List<Fragment>();
			while (SentQueue.Count > 0)
			{
				var receive = SentQueue.Dequeue();
				sendCount++;
				if (m_Random.NextDouble() < loss)
				{
					lossCount++;
					if (SentQueue.Count == 0) control.Update(TimeSpan.FromMilliseconds(100));
					continue;
				}
				int offset = 0;
				if (ReliableAckData.TryUnpack(receive, ref offset, out var ack))
				{
					control.ReceiveAck(ack);
					if (SentQueue.Count == 0)
					{
						control.Update(TimeSpan.FromMilliseconds(100));
					}
					continue;
				}
				offset = 0;
				if (!ReliableData.TryUnpack(receive, ref offset, out var data))
				{
					Assert.Fail();
				}
				if (control.Enqueue(data))
				{
					control.Update(TimeSpan.FromMilliseconds(100));
				}
				if (control.TryDequeue(output))
				{
					return true;
				}
			}
			return false;
		}
	}
}
