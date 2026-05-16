namespace RevCore
{
	/// <summary>
	/// Logging abstraction. The default implementation routes to <see cref="UnityEngine.Debug"/>;
	/// consumers may register a custom instance via <see cref="IServiceLocator"/> to route to
	/// analytics, a file, or a remote service.
	/// </summary>
	public interface IRevLogger
	{
		/// <summary>Messages with severity below this are dropped.</summary>
		LogLevel MinLevel { get; set; }

		/// <summary>Logs a message at the given level. <paramref name="context"/> is the Unity object whose Inspector "ping" the log entry should target.</summary>
		void Log(LogLevel level, string message, UnityEngine.Object context = null);

		/// <summary>Logs a tagged message. The tag is conventionally rendered as <c>[tag]</c> prefix and useful for filtering log streams.</summary>
		void Log(LogLevel level, string tag, string message, UnityEngine.Object context = null);
	}

	/// <summary>Sugar methods that route to <see cref="IRevLogger.Log(LogLevel, string, UnityEngine.Object)"/>.</summary>
	public static class RevLoggerExtensions
	{
		/// <summary>Logs at <see cref="LogLevel.Info"/>.</summary>
		public static void Info(this IRevLogger logger, string message, UnityEngine.Object context = null)
			=> logger.Log(LogLevel.Info, message, context);

		/// <summary>Logs at <see cref="LogLevel.Warning"/>.</summary>
		public static void Warn(this IRevLogger logger, string message, UnityEngine.Object context = null)
			=> logger.Log(LogLevel.Warning, message, context);

		/// <summary>Logs at <see cref="LogLevel.Error"/>.</summary>
		public static void Error(this IRevLogger logger, string message, UnityEngine.Object context = null)
			=> logger.Log(LogLevel.Error, message, context);
	}
}
