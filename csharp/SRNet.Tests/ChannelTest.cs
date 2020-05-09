using Microsoft.VisualStudio.TestTools.UnitTesting;
using SRNet.Channel;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SRNet.Tests
{

	[TestClass]
	public class ChannelTest
	{
		System.Random m_Random = new System.Random();

		byte[] GetRandam(int size)
		{
			byte[] buf = new byte[size];
			m_Random.NextBytes(buf);
			return buf;
		}

		List<Fragment> GetFragments(byte[] buf, short id, int size)
		{
			List<Fragment> fragments = new List<Fragment>();
			FragmentUtil.GetFragments(buf, 0, buf.Length, id, size, fragments);
			return fragments;
		}

		bool Equals(byte[] x, byte[] y)
		{
			if (x.Length != y.Length) return false;
			for (int i = 0; i < x.Length; i++)
			{
				if (x[i] != y[i]) return false;
			}
			return true;
		}

		[TestMethod]
		public void 断片化のテスト()
		{
			var buf = GetRandam(100);
			var list = GetFragments(buf, 1, Fragment.Size);
			Assert.AreEqual(1, list.Count, "断片化サイズ以下なら1つ");
			list.TryReturn();

			list = GetFragments(buf, 1, 10);
			Assert.AreEqual(100 / 10, list.Count, "断片化sizeごとに分割される");
			list.TryReturn();

			list = GetFragments(buf, 1, 30);
			Assert.IsTrue(Equals(buf, list.ToBytes()), "再度結合しても問題ない");
			list.TryReturn();
		}


		List<Fragment> GetStreamFragments(byte[] buf, short id, int size, int minWriteSize, int maxWriteSize)
		{
			var writer = new MessageWriter();
			writer.Set(id, size);
			int offset = 0;
			while (offset < buf.Length)
			{
				var writeSize = minWriteSize + (int)(m_Random.NextDouble() * (maxWriteSize - minWriteSize));
				if (offset + writeSize > buf.Length)
				{
					writeSize = buf.Length - offset;
				}
				writer.Write(buf, offset, writeSize);
				offset += writeSize;
			}
			var list = new List<Fragment>();
			writer.GetFragments(list);
			return list;
		}

		[TestMethod]
		public void 断片化のストリームテスト()
		{
			var buf = GetRandam(100);

			var list = GetStreamFragments(buf, 1, Fragment.Size, 10, 1);
			Assert.AreEqual(1, list.Count, "断片化サイズ以下なら1つ");
			Assert.IsTrue(Equals(buf, list.ToBytes()), "再度結合しても問題ない");
			list.TryReturn();

			list = GetStreamFragments(buf, 1, 10, 10, 1);
			Assert.AreEqual(100 / 10, list.Count, "断片化sizeごとに分割される");
			Assert.IsTrue(Equals(buf, list.ToBytes()), "再度結合しても問題ない");
			list.TryReturn();

		}

		[TestMethod]
		public void 断片化のストリームシークテスト()
		{
			var buf = GetRandam(100);
			var header = GetRandam(20);
			var last = GetRandam(20);

			var writer = new MessageWriter();
			writer.Set(1, 16);
			writer.Write(buf, 0, buf.Length);
			Assert.AreEqual(buf.Length, writer.Position);

			writer.Seek(20, SeekOrigin.Current);
			Assert.AreEqual(buf.Length + 20, writer.Position);
			Assert.AreEqual(buf.Length + 20, writer.Length, "長さが拡張される");

			writer.Seek(-10, SeekOrigin.Current);
			Assert.AreEqual(buf.Length + 20, writer.Length, "長さはそのまま");

			writer.SetLength(buf.Length);
			Assert.AreEqual(buf.Length, writer.Length, "指定サイズになっている");
			Assert.AreEqual(buf.Length, writer.Position, "短くなったので終端位置が変更される");

			writer.Seek(0, SeekOrigin.Begin);
			Assert.AreEqual(0, writer.Position);

			writer.Write(header, 0, header.Length);
			Assert.AreEqual(header.Length, writer.Position);

			writer.Seek(-last.Length, SeekOrigin.End);
			Assert.AreEqual(buf.Length - last.Length, writer.Position, "終端位置からのシーク");
			writer.Write(last, 0, last.Length);

			var list = new List<Fragment>();
			writer.GetFragments(list);
			var combine = header.Concat(buf.Skip(header.Length).Take(buf.Length - header.Length - last.Length)).Concat(last);
			Assert.IsTrue(Equals(combine.ToArray(), list.ToBytes()), "再度結合しても問題ない");
			list.TryReturn();
		}

		[TestMethod]
		public void 断片化のストリーム読み込みテスト()
		{
			var buf = GetRandam(1000);
			using (var reader = new MessageReader())
			{
				var input = GetFragments(buf, 1, 32);
				input.AddRef();
				reader.Set(input, new Peer(null, null, null), 1);

				var ms = new MemoryStream();
				while (reader.Position < buf.Length)
				{
					var tmp = new byte[(int)(m_Random.NextDouble() * 32)];
					var read = reader.Read(tmp, 0, tmp.Length);
					ms.Write(tmp, 0, read);
				}
				Assert.IsTrue(Equals(buf, ms.ToArray()), "読み込み成功");
			}
		}

		[TestMethod]
		public void 到達保証なしチャンネル機能テスト()
		{
			var ctx = new TestChannelContext();
			using (var control = new UnreliableFlowControl(1, 1, ctx))
			{
				var buf = GetRandam(1000);

				foreach (var size in new int[] { Fragment.Size, 100, 10 })
				{
					var fragments = GetFragments(buf, 1, size);
					control.Send(fragments);
					Assert.AreEqual(fragments.Count, ctx.SentQueue.Count, "送信した分割サイズと同じ");

					Assert.IsTrue(ctx.Receive(control, out var output), "受信データをパース出来なかった");

					Assert.IsTrue(Equals(buf, output.ToBytes()), "データを受け取れている");

					output.RemoveRef(true);
				}

			}
		}

		[TestMethod]
		public void 到達保証なしチャンネルランダム受信テスト()
		{
			var ctx = new TestChannelContext();
			using (var control = new UnreliableFlowControl(1, 1, ctx))
			{
				List<byte[]> buf = new List<byte[]>();
				short id = 1;
				for (int i = 0; i < 10; i++)
				{
					buf.Add(GetRandam(2000));
					control.Send(GetFragments(buf[i], id++, Fragment.Size));
				}
				ctx.ShuffleSentQueue();
				int count = 0;
				while (ctx.Receive(control, out var output))
				{
					count++;
					var ret = output.ToBytes();
					Assert.IsTrue(buf.Any(x => Equals(x, ret)), "指定のデータが含まれている");
					output.RemoveRef(true);
				}
				Assert.AreEqual(buf.Count, count, "同じ数読み取れている");
			}
		}

		[TestMethod, Timeout(2000)]
		public void 到達保証ありチャンネル機能テスト()
		{
			var ctx = new TestChannelContext();
			using (var control = new ReliableFlowControl(1, 1, ctx))
			{
				var buf = GetRandam(1000);
				short id = 1;
				foreach (var size in new int[] { Fragment.Size, 100, 10 })
				{
					var fragments = GetFragments(buf, id++, size);
					control.Send(fragments);

					Assert.IsTrue(ctx.Receive(control, out var output), "受信データをパース出来なかった");

					Assert.IsTrue(Equals(buf, output.ToBytes()), "データを受け取れている");

					output.RemoveRef(true);
				}
			}
		}

		[TestMethod, Timeout(2000)]
		public void 到達保証ありチャンネルパケロステスト()
		{
			var ctx = new TestChannelContext();
			using (var control = new ReliableFlowControl(1, 1, ctx))
			{
				List<byte[]> buf = new List<byte[]>();
				short id = 1;
				for (int i = 0; i < 50; i++)
				{
					buf.Add(GetRandam(2000 + (int)(10000 * m_Random.NextDouble())));
					control.Send(GetFragments(buf[i], id++, Fragment.Size));
				}
				ctx.ShuffleSentQueue();
				int count = 0;
				while (count < buf.Count && ctx.Receive(control, out var output, 0.3))
				{
					ctx.ShuffleSentQueue();
					var ret = output.ToBytes();
					Assert.IsTrue(Equals(buf[count++], ret), "順番に届く");
					output.RemoveRef(true);
				}
				Assert.AreEqual(buf.Count, count, "同じ数読み取れている");
			}
		}

	}
}
