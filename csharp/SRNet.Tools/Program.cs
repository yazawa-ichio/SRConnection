using Cocona;
using OpenSSL.PrivateKeyDecoder;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SRNet.Tools
{
	class Program
	{
		static void Main(string[] args)
		{
			CoconaApp.Run<Program>(args);
		}

		[Command(Description = "RSA  Key To XML")]
		public void KeyToXML([Argument]string path, [Option("private")] bool privateKey, [Option("public")] bool publicKey)
		{
			if (!privateKey && !publicKey)
			{
				privateKey = true;
				publicKey = true;
			}
			string privateKeyText = File.ReadAllText(path);
			IOpenSSLPrivateKeyDecoder decoder = new OpenSSLPrivateKeyDecoder();
			RSAParameters parameters = decoder.DecodeParameters(privateKeyText);
			using (var rsa = RSA.Create(parameters))
			{
				var dir = Path.GetDirectoryName(path);
				var name = Path.GetFileNameWithoutExtension(path);
				if (privateKey)
				{
					File.WriteAllText(Path.Combine(dir, name + "_private.xml"), rsa.ToXmlString(true));
				}
				if (publicKey)
				{
					File.WriteAllText(Path.Combine(dir, name + "_public.xml"), rsa.ToXmlString(false));
				}
			}
		}

		[Command(Description = "RSA Key Generate")]
		public void KeyGenerate([Argument]string path = ".", [Option("size")] int keySize = 4096)
		{
			string privateKeyText = File.ReadAllText("../../cert/private.key");
			IOpenSSLPrivateKeyDecoder decoder = new OpenSSLPrivateKeyDecoder();
			RSAParameters parameters = decoder.DecodeParameters(privateKeyText);
			using (var rsa = RSA.Create(parameters))
			{
				File.WriteAllText(Path.Combine(path, "private.key"), ExportPrivateKey(rsa));
				File.WriteAllText(Path.Combine(path, "private.xml"), rsa.ToXmlString(true));
				File.WriteAllText(Path.Combine(path, "public.xml"), rsa.ToXmlString(false));
			}
		}

		string ExportPrivateKey(RSA rsa)
		{
			var privateKeyBytes = rsa.ExportRSAPrivateKey();
			var builder = new StringBuilder();
			builder.AppendLine("-----BEGIN RSA PRIVATE KEY-----");
			var base64PrivateKeyString = Convert.ToBase64String(privateKeyBytes);
			var offset = 0;
			const int LINE_LENGTH = 64;
			while (offset < base64PrivateKeyString.Length)
			{
				var lineEnd = Math.Min(offset + LINE_LENGTH, base64PrivateKeyString.Length);
				builder.AppendLine(base64PrivateKeyString.Substring(offset, lineEnd - offset));
				offset = lineEnd;
			}
			builder.AppendLine("-----END RSA PRIVATE KEY-----");
			return builder.ToString();
		}

	}
}