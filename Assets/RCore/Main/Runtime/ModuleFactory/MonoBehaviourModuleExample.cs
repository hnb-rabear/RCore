namespace RCore.ModulePattern.Example
{
	using UnityEngine;

	public class MonoBehaviourModuleExample : MonoBehaviour, IModule
	{
		public string ModuleID => $"MonoBehaviourModule_{gameObject.GetInstanceID()}";

		private bool m_isRegistered;

		// --- IModule Implementation ---
		public void Initialize()
		{
			// This might be called by ModuleManager if registered manually via CreateAndRegister...
			// Or you might choose to put initialization logic directly in Awake/Start.
			Debug.Log($"[{nameof(MonoBehaviourModuleExample)}] InitializeModule called. ID: {ModuleID}");
		}

		public void Tick()
		{
			// ModuleManager will call this if the module is registered.
			// You could put logic here, or directly in this component's Update().
			// Debug.Log($"[{nameof(MonoBehaviourModuleExample)}] TickModule called. ID: {ModuleID}");
		}

		public void Shutdown()
		{
			// This is the IModule.Shutdown, called by ModuleManager during its shutdown
			// or explicit removal via RemoveModule.
			Debug.Log($"[{nameof(MonoBehaviourModuleExample)}] IModule.ShutdownModule called. ID: {ModuleID}");
			// Perform cleanup specific to the module interface contract if necessary.
		}

		// --- MonoBehaviour Lifecycle Methods ---
		private void Awake()
		{
			Debug.Log($"[{nameof(MonoBehaviourModuleExample)}] MonoBehaviour.Awake. Attempting self-registration. ID: {ModuleID}");
			m_isRegistered = ModuleManager.Instance.RegisterExistingModule(this);
			if (!m_isRegistered)
			{
				Debug.LogWarning($"[{nameof(MonoBehaviourModuleExample)}] Failed to register with ModuleManager (maybe a duplicate?). ID: {ModuleID}");
			}
		}
		
		private void OnDestroy()
		{
			Debug.Log($"[{nameof(MonoBehaviourModuleExample)}] MonoBehaviour.OnDestroy. Attempting self-unregistration. ID: {ModuleID}");
			if (m_isRegistered && ModuleManager.Instance != null)
			{
				ModuleManager.Instance.UnregisterModule(this);
			}
		}

		// --- Example Method ---
		public void DoMonoBehaviourThing()
		{
			Debug.Log($"[{nameof(MonoBehaviourModuleExample)}] Doing a MonoBehaviour-specific thing! ID: {ModuleID}");
		}
	}
}