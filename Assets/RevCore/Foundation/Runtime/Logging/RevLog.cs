using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// Default <see cref="IRevLogger"/> routing to <see cref="UnityEngine.Debug"/>. Tagged messages are
	/// prefixed with <c>[RevCore][tag]</c> for filtering. Set <see cref="MinLevel"/> to suppress noise.
	/// </summary>
	public sealed class RevLog : IRevLogger
	{
		/// <inheritdoc />
		public LogLevel MinLevel { get; set; } = LogLevel.Debug;

		/// <inheritdoc />
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

		/// <inheritdoc />
		public void Log(LogLevel level, string tag, string message, Object context = null)
		{
			if (level < MinLevel) return;
			Log(level, $"[{tag}] {message}", context);
		}
	}

	/// <summary>
	/// Static facade over the active <see cref="IRevLogger"/>. Replace <see cref="Logger"/> at startup
	/// to route RevCore logs elsewhere (analytics, file, remote). Assigning <c>null</c> restores the
	/// default <see cref="RevLog"/>.
	/// </summary>
	public static class Log
	{
		private static IRevLogger s_logger = new RevLog();

		/// <summary>The active logger. Setting <c>null</c> reinstates a default <see cref="RevLog"/>.</summary>
		public static IRevLogger Logger
		{
			get => s_logger;
			set => s_logger = value ?? new RevLog();
		}

		/// <summary>Logs at <see cref="LogLevel.Info"/>.</summary>
		public static void Info(string message, Object context = null)
			=> s_logger.Info(message, context);

		/// <summary>Logs at <see cref="LogLevel.Warning"/>.</summary>
		public static void Warn(string message, Object context = null)
			=> s_logger.Warn(message, context);

		/// <summary>Logs at <see cref="LogLevel.Error"/>.</summary>
		public static void Error(string message, Object context = null)
			=> s_logger.Error(message, context);
	}
}
