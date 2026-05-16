namespace RevCore
{
	/// <summary>
	/// Severity level for <see cref="IRevLogger"/>. Higher values are more severe; setting
	/// <see cref="IRevLogger.MinLevel"/> filters out everything below.
	/// </summary>
	public enum LogLevel
	{
		/// <summary>Most verbose. Per-frame or per-call diagnostics.</summary>
		Trace = 0,
		/// <summary>Developer-only diagnostics. Stripped from release builds by convention.</summary>
		Debug = 1,
		/// <summary>Significant lifecycle events; safe to ship.</summary>
		Info = 2,
		/// <summary>Suspicious but recoverable state.</summary>
		Warning = 3,
		/// <summary>Failed operation that the caller could not recover from.</summary>
		Error = 4,
		/// <summary>Sentinel — set <see cref="IRevLogger.MinLevel"/> to this to silence all output.</summary>
		Off = 5
	}
}
