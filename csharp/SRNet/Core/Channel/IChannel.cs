using System;
using System.Collections.Generic;

namespace SRNet.Channel
{
	public interface IChannel : IDisposable
	{
		IConfig Config { get; }
		void Init(short channelId, IChannelContext ctx);
		void Send(int id, List<Fragment> input);
		void OnReceive(int id, byte[] buf, int offset, int size);
		bool TryRead(int id, List<Fragment> output);
		void RemovePeer(int id);
		void Update(in TimeSpan delta);
	}
}