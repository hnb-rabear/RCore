namespace RevCore
{
	public interface IRevLogger
	{
		LogLevel MinLevel { get; set; }
		void Log(LogLevel level, string message, UnityEngine.Object context = null);
		void Log(LogLevel level, string tag, string message, UnityEngine.Object context = null);
	}

	public static class RevLoggerExtensions
	{
		public static void Info(this IRevLogger logger, string message, UnityEngine.Object context = null)
			=> logger.Log(LogLevel.Info, message, context);

		public static void Warn(this IRevLogger logger, string message, UnityEngine.Object context = null)
			=> logger.Log(LogLevel.Warning, message, context);

		public static void Error(this IRevLogger logger, string message, UnityEngine.Object context = null)
			=> logger.Log(LogLevel.Error, message, context);
	}
}
