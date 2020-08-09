using System;

namespace SRConnection.Logging
{

#if UNITY_5_3_OR_NEWER
	public class UnityLogger : ILogger
	{
		public void Trace(string message)
		{
			UnityEngine.Debug.Log("[SRConnection:Trace] " + message);
		}

		public void Debug(string message)
		{
			UnityEngine.Debug.Log("[SRConnection:Debug] " + message);
		}

		public void Info(string message)
		{
			UnityEngine.Debug.Log("[SRConnection:Info] " + message);
		}

		public void Warning(string message)
		{
			UnityEngine.Debug.LogWarning("[SRConnection:Warning] " + message);
		}

		public void Error(string message)
		{
			UnityEngine.Debug.LogError("[SRConnection:Error] " + message);
		}

		public void Exception(Exception ex)
		{
			UnityEngine.Debug.LogException(ex);
		}
	}
#endif
}