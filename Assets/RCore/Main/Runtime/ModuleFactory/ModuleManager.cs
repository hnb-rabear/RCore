namespace RCore.ModulePattern
{
	using Example;
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System;

	public class ModuleManager : MonoBehaviour
	{
		private static ModuleManager m_instance;
		public static ModuleManager Instance
		{
			get
			{
				if (m_instance == null)
				{
					m_instance = FindObjectOfType<ModuleManager>();
					if (m_instance == null)
					{
						var singletonObject = new GameObject(nameof(ModuleManager) + " (Singleton)");
						m_instance = singletonObject.AddComponent<ModuleManager>();
						DontDestroyOnLoad(singletonObject);
					}
				}
				return m_instance;
			}
		}

		private List<IModule> m_activeModules = new();

		// --- Unity Lifecycle Methods ---
		private void Awake()
		{
			if (m_instance == null)
			{
				m_instance = this;
				DontDestroyOnLoad(gameObject);
				Debug.Log($"[ModuleManager] Instance assigned from GameObject '{gameObject.name}' and marked DontDestroyOnLoad.");
			}
			else if (m_instance != this)
			{
				Debug.LogWarning($"[ModuleManager] Duplicate instance detected. GameObject '{gameObject.name}' is being destroyed.");
				Destroy(gameObject);
			}
		}

		private void Start()
		{
			Debug.Log("[ModuleManager] Starting initialization...");
			InitializeAutoModules();
			// RunExamples();
			// StartCoroutine(ExampleRemovalRoutine()); 
		}

		private void InitializeAutoModules()
		{
			Debug.Log("[ModuleManager] Attempting to auto-register modules based on LoadOrder and AutoCreate flags...");
			var modulesToCreate = ModuleFactory.GetModulesForAutoCreation();

			if (modulesToCreate.Count == 0)
			{
				Debug.LogWarning("[ModuleManager] No non-MonoBehaviour modules found for auto-creation.");
			}
			else
			{
				Debug.Log($"[ModuleManager] Found {modulesToCreate.Count} non-MonoBehaviour modules for auto-creation. Processing in load order...");
			}

			foreach (var metadata in modulesToCreate)
			{
				CreateAndRegisterModuleInternal(metadata.Key, metadata.ModuleType);
			}
			Debug.Log($"[ModuleManager] Finished auto-registering modules. {m_activeModules.Count} modules are now active.");
		}

		private void Update()
		{
			foreach (var module in m_activeModules.ToList())
			{
				if (module != null && m_activeModules.Contains(module))
				{
					try
					{
						module.Tick();
					}
					catch (Exception ex)
					{
						Debug.LogError($"[ModuleManager] Error ticking module '{module.ModuleID}' (Type: {module.GetType().FullName}): {ex.Message}\n{ex.StackTrace}");
					}
				}
			}
		}

		private void OnDestroy()
		{
			Debug.Log($"[ModuleManager] Manager OnDestroy called. Shutting down all {m_activeModules.Count} active modules...");

			for (int i = m_activeModules.Count - 1; i >= 0; i--)
			{
				// This internal method calls IModule.ShutdownModule for POCOs
				RemoveModuleInternal(m_activeModules[i]);
			}

			m_activeModules.Clear();
			Debug.Log("[ModuleManager] All active modules shut down and cleared.");

			if (m_instance == this)
			{
				m_instance = null;
				Debug.Log("[ModuleManager] Singleton instance nulled.");
			}
		}

		// --- Public Module Access and Management ---
		public T GetModule<T>() where T : class, IModule
		{
			var foundModule = m_activeModules.FirstOrDefault(m => m is T);
			if (foundModule is UnityEngine.Object unityObject && unityObject == null)
			{
				return null;
			}
			return foundModule as T;
		}

		public T CreateAndRegisterModule<T>(string key) where T : class, IModule
		{
			if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
			{
				Debug.LogError(
					$"[ModuleManager] CreateAndRegisterModule<{typeof(T).Name}> failed. This method cannot be used for MonoBehaviour types. Register them via RegisterExistingModule from their Awake/Start.");
				return null;
			}

			var existingModule = GetModule<T>();
			if (existingModule != null)
			{
				Debug.LogWarning($"[ModuleManager] Module of type '{typeof(T).Name}' is already active. Manual creation skipped.");
				return existingModule;
			}

			if (ModuleFactory.TryGetModuleMetadata(key, out var metaDataForKey))
			{
				if (m_activeModules.Any(m => m.GetType() == metaDataForKey.ModuleType))
				{
					Debug.LogWarning($"[ModuleManager] A module of type '{metaDataForKey.ModuleType.Name}' (associated with key '{key}') is already active. Manual creation skipped.");
					return null;
				}
			}

			var newModule = CreateAndRegisterModuleInternal(key, typeof(T));
			return newModule as T;
		}

		public bool RegisterExistingModule(IModule moduleInstance)
		{
			if (moduleInstance == null)
			{
				Debug.LogError("[ModuleManager] Cannot register null module instance.");
				return false;
			}

			var moduleType = moduleInstance.GetType();
			if (m_activeModules.Any(m => m.GetType() == moduleType))
			{
				Debug.LogWarning($"[ModuleManager] Cannot register module: An instance of type '{moduleType.FullName}' is already active.");
				return false;
			}

			m_activeModules.Add(moduleInstance);
			Debug.Log($"[ModuleManager] Registered existing module instance: Type='{moduleType.FullName}', ID='{moduleInstance.ModuleID}'.");
			// For existing MonoBehaviour modules, we assume their Awake/Start handles their specific initialization.
			// We do NOT call moduleInstance.InitializeModule() here to prevent double calls or calls at wrong times.
			return true;
		}

		public bool RemoveModule<T>() where T : class, IModule
		{
			var moduleInstance = GetModule<T>();
			if (moduleInstance != null)
			{
				return RemoveModuleInternal(moduleInstance);
			}
			Debug.LogWarning($"[ModuleManager] Cannot remove module: No active module of type '{typeof(T).Name}' found.");
			return false;
		}

		public bool RemoveModule(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				Debug.LogError("[ModuleManager] Cannot remove module: Key is null or empty.");
				return false;
			}

			IModule moduleInstance;
			if (ModuleFactory.TryGetModuleMetadata(key, out var metadata))
			{
				moduleInstance = m_activeModules.FirstOrDefault(m => m != null && m.GetType() == metadata.ModuleType);
			}
			else
			{
				Debug.LogWarning($"[ModuleManager] Cannot remove module with key '{key}': No metadata found for this key in the factory.");
				return false;
			}

			if (moduleInstance != null)
			{
				return RemoveModuleInternal(moduleInstance);
			}
			else
			{
				Debug.LogWarning($"[ModuleManager] Cannot remove module: No active module found for key '{key}' (Expected Type: {metadata.ModuleType.Name}).");
				return false;
			}
		}

		public bool UnregisterModule(IModule moduleInstance)
		{
			if (moduleInstance == null)
			{
				return false;
			}
			bool removed = m_activeModules.Remove(moduleInstance);
			if (removed)
			{
				Debug.Log($"[ModuleManager] Unregistered module instance: Type='{moduleInstance.GetType().FullName}', ID='{moduleInstance.ModuleID}'.");
			}
			return removed;
		}

		private IModule CreateAndRegisterModuleInternal(string key, Type expectedType)
		{
			if (m_activeModules.Any(m => m != null && m.GetType() == expectedType))
			{
				Debug.LogWarning($"[ModuleManager] Module of type '{expectedType.FullName}' is already active. Registration skipped.");
				return m_activeModules.First(m => m.GetType() == expectedType);
			}

			var moduleInstance = ModuleFactory.CreateModule(key);

			if (moduleInstance != null)
			{
				try
				{
					Debug.Log($"[ModuleManager] Initializing module '{key}' (Type: {moduleInstance.GetType().FullName})...");
					moduleInstance.Initialize();
					m_activeModules.Add(moduleInstance);
					Debug.Log($"[ModuleManager] Module '{key}' initialized and added to active modules.");
					return moduleInstance;
				}
				catch (Exception ex)
				{
					Debug.LogError($"[ModuleManager] Error initializing module '{key}' (Type: {moduleInstance.GetType().FullName}): {ex.Message}\n{ex.StackTrace}");
					return null;
				}
			}
			else
			{
				return null;
			}
		}

		private bool RemoveModuleInternal(IModule moduleInstance)
		{
			if (moduleInstance == null)
			{
				return false;
			}

			if (moduleInstance is UnityEngine.Object unityObject && unityObject == null)
			{
				Debug.LogWarning($"[ModuleManager] Attempted to remove module (Type: {moduleInstance.GetType().FullName}) that is already destroyed. Removing stale reference.");
				return m_activeModules.Remove(moduleInstance);
			}

			string moduleTypeName = moduleInstance.GetType().FullName;
			string moduleID = "N/A";
			try
			{
				moduleID = moduleInstance.ModuleID;
			}
			catch
			{
				/* Ignore */
			}

			Debug.Log($"[ModuleManager] Shutting down module instance: Type='{moduleTypeName}', ID='{moduleID}'...");

			try
			{
				moduleInstance.Shutdown();
			}
			catch (Exception ex)
			{
				if (!(ex is MissingReferenceException))
				{
					Debug.LogError($"[ModuleManager] Error during ShutdownModule for module '{moduleID}' (Type: {moduleTypeName}): {ex.Message}\n{ex.StackTrace}");
				}
			}

			bool removed = m_activeModules.Remove(moduleInstance);
			if (removed)
			{
				Debug.Log($"[ModuleManager] Module instance '{moduleID}' removed from active list.");
			}
			else
			{
				Debug.LogWarning($"[ModuleManager] Module instance '{moduleID}' (Type: {moduleTypeName}) was not found in the active list during internal removal attempt.");
			}
			return removed;
		}

		private IModule GetModule(Type moduleType)
		{
			return m_activeModules.FirstOrDefault(m => m != null && m.GetType() == moduleType);
		}

		//------------

		private void RunExamples()
		{
			Debug.Log("[ModuleManager] Running examples...");
			var audio = GetModule<AudioModuleExample>();
			audio?.PlaySound("Startup Jingle");

			var inventory = GetModule<InventoryModuleExample>();
			if (inventory != null) Debug.Log($"[ModuleManager] Has Sword: {inventory.HasItem("SwordOfDebugging")}");

			var manualModule = CreateAndRegisterModule<ManualCreationModuleExample>(nameof(ManualCreationModuleExample));
			manualModule?.DoManualThing();

			var remoteConfig = GetModule<IRemoteConfigModuleExample>();
			if (remoteConfig != null)
			{
				Debug.Log(
					$"[RemoteConfig] Welcome: '{remoteConfig.GetString("welcome_message", "Default")}', FeatureX: {remoteConfig.GetBool("feature_x_enabled", false)}, Lives: {remoteConfig.GetInt("max_lives", 3)}");
			}

			var monoModule = GetModule<MonoBehaviourModuleExample>();
			if (monoModule != null)
			{
				Debug.Log($"[ModuleManager] Successfully retrieved manually registered {nameof(MonoBehaviourModuleExample)}.");
				monoModule.DoMonoBehaviourThing();
			}
			else
			{
				Debug.LogWarning($"[ModuleManager] Could not retrieve {nameof(MonoBehaviourModuleExample)}. Ensure it's added to a GameObject in the scene and registered itself in Awake.");
			}
		}

		private IEnumerator ExampleRemovalRoutine()
		{
			yield return new WaitForSeconds(5.0f);

			Debug.Log("[ModuleManager] ExampleRemovalRoutine: Attempting to remove ManualCreationModuleExample...");
			bool removed = RemoveModule<ManualCreationModuleExample>();
			Debug.Log($"[ModuleManager] ExampleRemovalRoutine: ManualCreationModuleExample removal result: {removed}");

			var manualModule = GetModule<ManualCreationModuleExample>();
			if (manualModule == null) Debug.Log("[ModuleManager] ExampleRemovalRoutine: Confirmed ManualCreationModuleExample is no longer active (correct).");

			yield return new WaitForSeconds(2.0f);

			Debug.Log("[ModuleManager] ExampleRemovalRoutine: Attempting to remove AudioModuleExample by key...");
			removed = RemoveModule(nameof(AudioModuleExample));
			Debug.Log($"[ModuleManager] ExampleRemovalRoutine: AudioModuleExample removal result: {removed}");

			var audioModule = GetModule<AudioModuleExample>();
			if (audioModule == null) Debug.Log("[ModuleManager] ExampleRemovalRoutine: Confirmed AudioModuleExample is no longer active (correct).");

			yield return new WaitForSeconds(2.0f);
			Debug.Log("[ModuleManager] ExampleRemovalRoutine: Attempting to destroy GameObject of MonoBehaviourModuleExample (if found)...");
			var monoModule = GetModule<MonoBehaviourModuleExample>();
			if (monoModule != null)
			{
				Destroy(monoModule.gameObject);
				yield return null;
				var monoModuleAfterDestroy = GetModule<MonoBehaviourModuleExample>();
				if (monoModuleAfterDestroy == null) Debug.Log("[ModuleManager] ExampleRemovalRoutine: Confirmed MonoBehaviourModuleExample is no longer active after GameObject destroy (correct).");
				else Debug.LogWarning("[ModuleManager] ExampleRemovalRoutine: MonoBehaviourModuleExample still found after GameObject destroy (INCORRECT).");
			}
			else
			{
				Debug.LogWarning("[ModuleManager] ExampleRemovalRoutine: MonoBehaviourModuleExample not found, cannot test GameObject destruction.");
			}
		}
	}
}