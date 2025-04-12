#if UNITY_EDITOR
namespace RCore.Examples
{
	[LifecycleEvent] // Add this attribute!
	public class AutoDetectedService : ILifecycleEvent
	{
		// MUST have a parameterless constructor for Activator.CreateInstance
		public AutoDetectedService()
		{
			UnityEngine.Debug.Log("AutoDetectedService created.");
		}
		public void Start() { }
		public void Update() { }
		public void OnApplicationPause(bool pause) { }
		public void OnApplicationFocus(bool hasFocus) { }
		public void OnApplicationQuit()
		{
			Debug.Log("AutoDetectedService quitting.");
			// Note: Automatic unregistration isn't built-in here.
			// LifecycleEventsManager clears its list OnApplicationQuit anyway.
		}
	}

	// This class will NOT be auto-registered because it lacks the attribute
	public class ManualService : ILifecycleEvent
	{
		public void Start() { }
		public void Update() { }
		public void OnApplicationPause(bool pause) { }
		public void OnApplicationFocus(bool hasFocus) { }
		public void OnApplicationQuit() { }
	}
}
#endif