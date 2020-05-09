using System;

namespace SRNet.Logging
{
	public class ConsoleLogger : ILogger
	{
		public void Trace(string message)
		{
			Console.Write("[SRNet:Trace] ");
			Console.WriteLine(message);
		}

		public void Debug(string message)
		{
			Console.Write("[SRNet:Debug] ");
			Console.WriteLine(message);
		}

		public void Info(string message)
		{
			Console.Write("[SRNet:Info] ");
			Console.WriteLine(message);
		}

		public void Warning(string message)
		{
			Console.Write("[SRNet:Warning] ");
			Console.WriteLine(message);
		}

		public void Error(string message)
		{
			Console.Write("[SRNet:Error] ");
			Console.WriteLine(message);
		}

		public void Exception(Exception ex)
		{
			Console.Write("[SRNet:Exception] ");
			Console.WriteLine(ex);
		}

	}

}