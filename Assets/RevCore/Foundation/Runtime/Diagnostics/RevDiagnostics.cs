namespace RevCore
{
	/// <summary>
	/// Global access point for the active <see cref="IRevDiagnostics"/> listener. Default is
	/// <c>null</c> — every framework hook site uses the null-conditional operator so an absent
	/// listener costs a single predicted branch.
	/// </summary>
	/// <remarks>
	/// Assign during application startup; clear (set to <c>null</c>) to detach. The framework
	/// does not support multi-listener composition out of the box — wrap multiple listeners in a
	/// composite implementation if needed.
	/// </remarks>
	public static class RevDiagnostics
	{
		/// <summary>The current listener, or <c>null</c> when nothing is observing.</summary>
		public static IRevDiagnostics Listener;
	}
}
