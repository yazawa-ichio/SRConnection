using System;

namespace SRNet.Logging
{

#if UNITY_5_3_OR_NEWER
	public class UnityLogger : ILogger
	{
		public void Trace(string message)
		{
			UnityEngine.Debug.Log("[SRNet:Trace] " + message);
		}

		public void Debug(string message)
		{
			UnityEngine.Debug.Log("[SRNet:Debug] " + message);
		}

		public void Info(string message)
		{
			UnityEngine.Debug.Log("[SRNet:Info] " + message);
		}

		public void Warning(string message)
		{
			UnityEngine.Debug.LogWarning("[SRNet:Warning] " + message);
		}

		public void Error(string message)
		{
			UnityEngine.Debug.LogError("[SRNet:Error] " + message);
		}

		public void Exception(Exception ex)
		{
			UnityEngine.Debug.LogException(ex);
		}
	}
#endif
}