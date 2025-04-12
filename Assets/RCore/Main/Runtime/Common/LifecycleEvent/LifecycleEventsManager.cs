using System;
using UnityEngine;
using System.Collections.Generic;

namespace RCore
{
	public interface ILifecycleEvent
	{
		void Start();
		void Update();
		void OnApplicationPause(bool pause);
		void OnApplicationFocus(bool hasFocus);
		void OnApplicationQuit();
	}

	/// <summary>
	/// A Singleton MonoBehaviour that manages the lifecycle (Update, Pause, Focus, Quit, ...)
	/// for registered non-MonoBehaviour objects implementing ILifecycleEvent.
	/// It persists across scene loads.
	/// </summary>
	public class LifecycleEventsManager : MonoBehaviour
	{
		private static LifecycleEventsManager m_Instance;
		public static LifecycleEventsManager Instance
		{
			get
			{
				if (m_Instance != null)
				{
					return m_Instance;
				}

				// Try to find an existing instance in the scene
				// Handles cases like domain reload where static ref might be lost
				m_Instance = FindObjectOfType<LifecycleEventsManager>();

				if (m_Instance == null)
				{
					// No instance found, create a new one
					var obj = new GameObject(nameof(LifecycleEventsManager));
					m_Instance = obj.AddComponent<LifecycleEventsManager>();

					// Hide it from hierarchy, don't save it to scenes,
					// and don't destroy it when loading new scenes.
					obj.hideFlags = HideFlags.HideAndDontSave;
					// Note: DontDestroyOnLoad is called in Awake now for robustness
				}

				// This should ideally not be needed if Awake handles DontDestroyOnLoad correctly,
				// but double-check ensures persistence if accessed before Awake runs somehow.
				if (Application.isPlaying) // DontDestroyOnLoad only works in Play mode
				{
					DontDestroyOnLoad(m_Instance.gameObject);
				}

				return m_Instance;
			}
		}

		// List to hold registered objects
		private readonly List<ILifecycleEvent> m_noMonoBehaviours = new();
		// Buffer for safe removal during iteration
		private readonly List<ILifecycleEvent> m_toRemove = new();
		// Flag to prevent list modification exceptions during loops
		private bool m_isIterating;

		/// <summary>
		/// Registers an object implementing INoMonoBehaviour to receive lifecycle callbacks.
		/// </summary>
		/// <param name="obj">The object to register.</param>
		public void Register(ILifecycleEvent obj)
		{
			if (obj == null)
			{
				Debug.LogWarning($"{nameof(LifecycleEventsManager)}: Attempted to register a null object.");
				return;
			}

			// Prevent adding duplicates
			if (!m_noMonoBehaviours.Contains(obj))
			{
				m_noMonoBehaviours.Add(obj);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				Debug.Log($"{nameof(LifecycleEventsManager)}: Registered {obj.GetType().Name} ({obj.GetHashCode()}). Count: {m_noMonoBehaviours.Count}");
#endif
			}
			else
			{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				Debug.LogWarning($"{nameof(LifecycleEventsManager)}: Attempted to register duplicate object {obj.GetType().Name} ({obj.GetHashCode()}).");
#endif
			}
		}

		/// <summary>
		/// Unregisters an object, preventing it from receiving further lifecycle callbacks.
		/// </summary>
		/// <param name="obj">The object to unregister.</param>
		public void Unregister(ILifecycleEvent obj)
		{
			if (obj == null)
			{
				Debug.LogWarning($"{nameof(LifecycleEventsManager)}: Attempted to unregister a null object.");
				return;
			}

			// Check if the object is actually in the list before proceeding
			bool wasPresent = m_noMonoBehaviours.Contains(obj);

			if (m_isIterating)
			{
				// If currently iterating, buffer removal for later to avoid modifying collection
				if (wasPresent && !m_toRemove.Contains(obj))
				{
					m_toRemove.Add(obj);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
					Debug.Log($"{nameof(LifecycleEventsManager)}: Queued {obj.GetType().Name} ({obj.GetHashCode()}) for removal. Count: {m_noMonoBehaviours.Count - m_toRemove.Count}");
#endif
				}
			}
			else
			{
				// If not iterating, remove directly
				if (m_noMonoBehaviours.Remove(obj))
				{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
					Debug.Log($"{nameof(LifecycleEventsManager)}: Unregistered {obj.GetType().Name} ({obj.GetHashCode()}). Count: {m_noMonoBehaviours.Count}");
#endif
				}
				else if (wasPresent)
				{
					// This case should ideally not happen if Contains was true, but handles edge cases.
					Debug.LogWarning($"{nameof(LifecycleEventsManager)}: Failed to remove {obj.GetType().Name} ({obj.GetHashCode()}) directly even though Contains returned true.");
				}
				else
				{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
					// Don't warn if simply trying to unregister something not present or already unregistered.
					// Debug.Log($"{nameof(LifecycleEventsManager)}: Attempted to unregister {obj.GetType().Name} ({obj.GetHashCode()}) which was not registered.");
#endif
				}
			}
		}

		/// <summary>
		/// Processes the queue of objects marked for removal.
		/// </summary>
		private void ProcessRemovals()
		{
			if (m_toRemove.Count > 0)
			{
				foreach (var item in m_toRemove)
				{
					if (m_noMonoBehaviours.Remove(item))
					{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
						Debug.Log($"{nameof(LifecycleEventsManager)}: Completed removal of {item.GetType().Name} ({item.GetHashCode()}). Count: {m_noMonoBehaviours.Count}");
#endif
					}
				}
				m_toRemove.Clear();
			}
		}

		// --- Unity Lifecycle Methods ---

		private void Start()
		{
			for (int i = 0; i < m_noMonoBehaviours.Count; i++)
				m_noMonoBehaviours[i].Start();
		}

		private void Update()
		{
			ProcessRemovals(); // Process removals queued from previous frames or external calls

			m_isIterating = true;
			try
			{
				for (int i = 0; i < m_noMonoBehaviours.Count; ++i)
				{
					try
					{
						m_noMonoBehaviours[i]?.Update(); // Add null check for safety
					}
					catch (Exception ex)
					{
						Debug.LogError($"Error in {m_noMonoBehaviours[i]?.GetType().Name}.Update: {ex}");
					}
				}
			}
			finally
			{
				m_isIterating = false;
			}
			ProcessRemovals(); // Process any removals queued during this frame's Update calls
		}

		private void OnApplicationPause(bool pause)
		{
			ProcessRemovals();
			m_isIterating = true;
			try
			{
				foreach (var mono in m_noMonoBehaviours)
				{
					try
					{
						mono?.OnApplicationPause(pause);
					}
					catch (Exception ex)
					{
						Debug.LogError($"Error in {mono?.GetType().Name}.OnApplicationPause: {ex}");
					}
				}
			}
			finally
			{
				m_isIterating = false;
			}
			ProcessRemovals();
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			ProcessRemovals();
			m_isIterating = true;
			try
			{
				foreach (var mono in m_noMonoBehaviours)
				{
					try
					{
						mono?.OnApplicationFocus(hasFocus);
					}
					catch (Exception ex)
					{
						Debug.LogError($"Error in {mono?.GetType().Name}.OnApplicationFocus: {ex}");
					}
				}
			}
			finally
			{
				m_isIterating = false;
			}
			ProcessRemovals();
		}

		private void OnApplicationQuit()
		{
			Debug.Log($"{nameof(LifecycleEventsManager)}: Application quitting. Calling OnApplicationQuit on registered objects.");
			// No need to process removals, application is ending.
			m_isIterating = true; // Set flag just in case, though unlikely to matter
			try
			{
				// Can iterate directly as the list won't be modified further meaningfully
				// Iterate backwards as objects might do cleanup that affects others
				for (int i = m_noMonoBehaviours.Count - 1; i >= 0; i--)
				{
					try
					{
						m_noMonoBehaviours[i]?.OnApplicationQuit();
					}
					catch (Exception ex)
					{
						Debug.LogError($"Error in {m_noMonoBehaviours[i]?.GetType().Name}.OnApplicationQuit: {ex}");
					}
				}
			}
			finally
			{
				m_isIterating = false;
			}

			// Clear lists on quit
			m_noMonoBehaviours.Clear();
			m_toRemove.Clear();
			Debug.Log($"{nameof(LifecycleEventsManager)}: Registered objects list cleared.");

			// Optional: Explicitly null the static instance if needed elsewhere
			// though the application ending handles memory cleanup.
			m_Instance = null;
		}

		private void OnDestroy()
		{
			// This might be called on scene change if DontDestroyOnLoad wasn't set correctly,
			// or when stopping the editor, or if a duplicate was destroyed.
			if (m_Instance == this)
			{
				// If this instance *was* the singleton, clear the static reference
				// to allow garbage collection and prevent issues on re-entering play mode
				// without domain reload.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				Debug.Log($"{nameof(LifecycleEventsManager)} OnDestroy: Singleton instance ({this.GetInstanceID()}) is being destroyed.");
#endif
				// Don't call OnApplicationQuit here as the 'quitting' intent is handled by OnApplicationQuit itself.
				// Clear lists just in case.
				m_noMonoBehaviours.Clear();
				m_toRemove.Clear();
				m_Instance = null;
			}
		}
	}
}