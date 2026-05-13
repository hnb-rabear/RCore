using UnityEngine;

namespace RevCore
{
	public sealed class RevLog : IRevLogger
	{
		public LogLevel MinLevel { get; set; } = LogLevel.Debug;

		public void Log(LogLevel level, string message, Object context = null)
		{
			if (level < MinLevel) return;

			switch (level)
			{
				case LogLevel.Trace:
				case LogLevel.Debug:
				case LogLevel.Info:
					UnityEngine.Debug.Log($"[RevCore] {message}", context);
					break;
				case LogLevel.Warning:
					UnityEngine.Debug.LogWarning($"[RevCore] {message}", context);
					break;
				case LogLevel.Error:
					UnityEngine.Debug.LogError($"[RevCore] {message}", context);
					break;
			}
		}

		public void Log(LogLevel level, string tag, string message, Object context = null)
		{
			if (level < MinLevel) return;
			Log(level, $"[{tag}] {message}", context);
		}
	}

	public static class Log
	{
		private static IRevLogger s_logger = new RevLog();

		public static IRevLogger Logger
		{
			get => s_logger;
			set => s_logger = value ?? new RevLog();
		}

		public static void Info(string message, Object context = null)
			=> s_logger.Info(message, context);

		public static void Warn(string message, Object context = null)
			=> s_logger.Warn(message, context);

		public static void Error(string message, Object context = null)
			=> s_logger.Error(message, context);
	}
}
