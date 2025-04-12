using System;

namespace RCore
{
	/// <summary>
	/// Add this attribute to classes implementing ILifecycleEvent
	/// that should be automatically instantiated and registered by LifecycleEventsManager's auto-loader.
	/// The class MUST have a parameterless constructor.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class LifecycleEventAttribute : Attribute { }
}