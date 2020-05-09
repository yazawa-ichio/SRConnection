using SRNet.Logging;
using System.Diagnostics;

namespace SRNet
{

	public static class Log
	{
		const string ENABLED_ALL = "SRNET_LOG_ALL";
		const string ENABLED_DEBUG_OR_HIGHER = "SRNET_LOG_DEBUG_OR_HIGHER";
		const string ENABLED_WARNING_OR_HIGHER = "SRNET_LOG_WARNING_OR_HIGHER";

		public enum LogLevel
		{
			Exception,
			Error,
			Warning,
			Info,
			Debug,
			Trace,
			All = Trace,
		}

		static ILogger s_Logger;
		static bool s_Enabled = true;
		static LogLevel s_Level = LogLevel.Warning;

		public static bool Enabled
		{
			get => s_Enabled;
			set => s_Enabled = value;
		}

		public static LogLevel Level
		{
			get => s_Level;
			set => s_Level = value;
		}

		public static ILogger Logger
		{
			get => s_Logger;
			set => s_Logger = value;
		}

		public static void Init()
		{
#if UNITY_5_3_OR_NEWER
			s_Logger = s_Logger ?? new UnityLogger();
#else
			s_Logger = s_Logger ?? new ConsoleLogger();
#endif
		}

		[Conditional(ENABLED_ALL)]
		internal static void Trace(string message)
		{
			if (!s_Enabled || s_Level < LogLevel.Trace) return;
			s_Logger?.Trace(message);
		}

		[Conditional(ENABLED_ALL)]
		internal static void Trace<T1>(string message, T1 arg0)
		{
			if (!s_Enabled || s_Level < LogLevel.Trace) return;
			s_Logger?.Trace(string.Format(message, arg0));
		}

		[Conditional(ENABLED_ALL)]
		internal static void Trace<T1, T2>(string message, T1 arg0, T2 arg1)
		{
			if (!s_Enabled || s_Level < LogLevel.Trace) return;
			s_Logger?.Trace(string.Format(message, arg0, arg1));
		}

		[Conditional(ENABLED_ALL)]
		internal static void Trace<T1, T2, T3>(string message, T1 arg0, T2 arg1, T3 arg2)
		{
			if (!s_Enabled || s_Level < LogLevel.Trace) return;
			s_Logger?.Trace(string.Format(message, arg0, arg1, arg2));
		}

		[Conditional(ENABLED_ALL), Conditional(ENABLED_DEBUG_OR_HIGHER)]
		internal static void Debug(string message)
		{
			if (!s_Enabled || s_Level < LogLevel.Debug) return;
			s_Logger?.Debug(message);
		}

		[Conditional(ENABLED_ALL), Conditional(ENABLED_DEBUG_OR_HIGHER)]
		internal static void Debug<T1>(string message, T1 arg0)
		{
			if (!s_Enabled || s_Level < LogLevel.Debug) return;
			s_Logger?.Debug(string.Format(message, arg0));
		}

		[Conditional(ENABLED_ALL), Conditional(ENABLED_DEBUG_OR_HIGHER)]
		internal static void Debug<T1, T2>(string message, T1 arg0, T2 arg1)
		{
			if (!s_Enabled || s_Level < LogLevel.Debug) return;
			s_Logger?.Debug(string.Format(message, arg0, arg1));
		}

		[Conditional(ENABLED_ALL), Conditional(ENABLED_DEBUG_OR_HIGHER)]
		internal static void Debug<T1, T2, T3>(string message, T1 arg0, T2 arg1, T3 arg2)
		{
			if (!s_Enabled || s_Level < LogLevel.Debug) return;
			s_Logger?.Debug(string.Format(message, arg0, arg1, arg2));
		}

		[Conditional(ENABLED_ALL), Conditional(ENABLED_DEBUG_OR_HIGHER)]
		internal static void Info(string message)
		{
			if (!s_Enabled || s_Level < LogLevel.Info) return;
			s_Logger?.Info(message);
		}

		[Conditional(ENABLED_ALL), Conditional(ENABLED_DEBUG_OR_HIGHER)]
		internal static void Info<T1>(string message, T1 arg0)
		{
			if (!s_Enabled || s_Level < LogLevel.Info) return;
			s_Logger?.Info(string.Format(message, arg0));
		}

		[Conditional(ENABLED_ALL), Conditional(ENABLED_DEBUG_OR_HIGHER)]
		internal static void Info<T1, T2>(string message, T1 arg0, T2 arg1)
		{
			if (!s_Enabled || s_Level < LogLevel.Info) return;
			s_Logger?.Info(string.Format(message, arg0, arg1));
		}

		[Conditional(ENABLED_ALL), Conditional(ENABLED_DEBUG_OR_HIGHER)]
		internal static void Info<T1, T2, T3>(string message, T1 arg0, T2 arg1, T3 arg2)
		{
			if (!s_Enabled || s_Level < LogLevel.Info) return;
			s_Logger?.Info(string.Format(message, arg0, arg1, arg2));
		}

		[Conditional(ENABLED_ALL), Conditional(ENABLED_DEBUG_OR_HIGHER), Conditional(ENABLED_WARNING_OR_HIGHER)]
		internal static void Warning(string message)
		{
			if (!s_Enabled || s_Level < LogLevel.Warning) return;
			s_Logger?.Warning(message);
		}

		[Conditional(ENABLED_ALL), Conditional(ENABLED_DEBUG_OR_HIGHER), Conditional(ENABLED_WARNING_OR_HIGHER)]
		internal static void Warning<T1>(string message, T1 arg0)
		{
			if (!s_Enabled || s_Level < LogLevel.Warning) return;
			s_Logger?.Warning(string.Format(message, arg0));
		}

		[Conditional(ENABLED_ALL), Conditional(ENABLED_DEBUG_OR_HIGHER), Conditional(ENABLED_WARNING_OR_HIGHER)]
		internal static void Warning<T1, T2>(string message, T1 arg0, T2 arg1)
		{
			if (!s_Enabled || s_Level < LogLevel.Warning) return;
			s_Logger?.Warning(string.Format(message, arg0, arg1));
		}

		[Conditional(ENABLED_ALL), Conditional(ENABLED_DEBUG_OR_HIGHER), Conditional(ENABLED_WARNING_OR_HIGHER)]
		internal static void Error(string message)
		{
			if (!s_Enabled || s_Level < LogLevel.Error) return;
			s_Logger?.Error(message);
		}

		[Conditional(ENABLED_ALL), Conditional(ENABLED_DEBUG_OR_HIGHER), Conditional(ENABLED_WARNING_OR_HIGHER)]
		internal static void Error<T1>(string message, T1 arg0)
		{
			if (!s_Enabled || s_Level < LogLevel.Error) return;
			s_Logger?.Error(string.Format(message, arg0));
		}

		[Conditional(ENABLED_ALL), Conditional(ENABLED_DEBUG_OR_HIGHER), Conditional(ENABLED_WARNING_OR_HIGHER)]
		internal static void Error<T1, T2>(string message, T1 arg0, T2 arg1)
		{
			if (!s_Enabled || s_Level < LogLevel.Error) return;
			s_Logger?.Error(string.Format(message, arg0, arg1));
		}

		[Conditional(ENABLED_ALL), Conditional(ENABLED_DEBUG_OR_HIGHER), Conditional(ENABLED_WARNING_OR_HIGHER)]
		internal static void Exception(System.Exception ex)
		{
			if (s_Logger == null) return;
			if (!s_Enabled || s_Level < LogLevel.Exception) return;
			s_Logger?.Exception(ex);
		}


	}
}