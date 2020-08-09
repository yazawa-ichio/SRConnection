using System;

namespace SRConnection.Logging
{
	public class ConsoleLogger : ILogger
	{
		public void Trace(string message)
		{
			Console.Write("[SRConnection:Trace] ");
			Console.WriteLine(message);
		}

		public void Debug(string message)
		{
			Console.Write("[SRConnection:Debug] ");
			Console.WriteLine(message);
		}

		public void Info(string message)
		{
			Console.Write("[SRConnection:Info] ");
			Console.WriteLine(message);
		}

		public void Warning(string message)
		{
			Console.Write("[SRConnection:Warning] ");
			Console.WriteLine(message);
		}

		public void Error(string message)
		{
			Console.Write("[SRConnection:Error] ");
			Console.WriteLine(message);
		}

		public void Exception(Exception ex)
		{
			Console.Write("[SRConnection:Exception] ");
			Console.WriteLine(ex);
		}

	}

}