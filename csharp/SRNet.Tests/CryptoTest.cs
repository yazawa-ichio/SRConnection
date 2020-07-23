using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;

namespace SRNet.Tests
{
	[TestClass]
	public class CryptoTest
	{

		StringBuilder m_SB = new StringBuilder();
		string To(byte[] buf)
		{
			m_SB.Length = 0;
			foreach (var b in buf)
			{
				m_SB.Append(b.ToString("X2") + ",");
			}
			m_SB.Length--;
			Console.WriteLine(m_SB.ToString());
			return m_SB.ToString();
		}


		[TestMethod]
		public void SHA256Test()
		{
			var sha1 = System.Security.Cryptography.SHA256.Create();
			var sha2 = new Crypto.SHA256();

			byte[] hash3 = new byte[32];

			for (int i = 0; i < 2048; i++)
			{
				var buf = Random.GenBytes(i);
				var hash1 = sha1.ComputeHash(buf);


				var hash2 = sha2.ComputeHash(buf);
				sha2.ComputeHash(buf, hash3);

				Assert.AreEqual(To(hash1), To(hash2));
				Assert.AreEqual(To(hash1), To(hash3));

				foreach (var _split in new int[] { 10, 34, 57, 128, 300, 500, 1000, 1500 })
				{
					var split = 0;
					if (i > 0)
					{
						split = _split % i;
					}
					sha2.Initialize();
					var tmp = sha2.TransformBlock(buf, 0, split, null, 0);
					var sha3 = new Crypto.SHA256(sha2);
					sha2.TransformFinalBlock(buf, split, i - split);
					sha3.TransformFinalBlock(buf, split, i - split);
					Assert.AreEqual(To(hash1), To(sha2.Hash));
					Assert.AreEqual(To(hash1), To(sha3.Hash));
				}
			}
		}

		[TestMethod]
		public void SHA256UpdateTest()
		{
			var sha1 = System.Security.Cryptography.SHA256.Create();
			var sha2 = new Crypto.SHA256();
			for (int i = 0; i < 2048; i++)
			{
				var buf = Random.GenBytes(i);
				var hash1 = sha1.ComputeHash(buf);
				foreach (var b in buf)
				{
					sha2.TransformBlock(new byte[] { b }, 0, 1, null, 0);
				}
				sha2.TransformFinalBlock(System.Array.Empty<byte>(), 0, 0);
				Assert.AreEqual(To(hash1), To(sha2.Hash));
			}
		}

		[TestMethod]
		public void HMACSHA256Test()
		{
			var hmac1 = new System.Security.Cryptography.HMACSHA256();
			var hmac2 = new Crypto.HMACSHA256(hmac1.Key);

			Assert.AreEqual(To(hmac1.Key), To(hmac2.Key));

			byte[] hash3 = new byte[32];

			for (int i = 0; i < 2048; i++)
			{
				var buf = Random.GenBytes(i);
				var hash1 = hmac1.ComputeHash(buf);
				var hash2 = hmac2.ComputeHash(buf);
				hmac2.ComputeHash(buf, hash3);

				Assert.AreEqual(To(hash1), To(hash2));
				Assert.AreEqual(To(hash1), To(hash3));

			}

		}

	}
}