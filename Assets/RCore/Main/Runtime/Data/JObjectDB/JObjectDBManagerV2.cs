/**
 * Author HNB-RaBear - 2024
 **/

using System;
using System.Reflection;
using RCore.Inspector;
using UnityEngine;

namespace RCore.Data.JObject
{
	/// <summary>
	/// A generic MonoBehaviour that serves as the master orchestrator for the JObjectModel-based data system.
	/// This "Version 2" manager is designed to work with a `JObjectModelCollection`, which aggregates all data models.
	/// It handles the primary application lifecycle events (Update, Pause, Quit) and manages the data saving process.
	/// It should be placed on a persistent GameObject in your main scene.
	/// </summary>
	/// <typeparam name="T">The specific type of JObjectModelCollection this manager will control.</typeparam>
	public abstract class JObjectDBManagerV2<T> : MonoBehaviour where T : JObjectModelCollection
	{
		/// <summary>
		/// An action invoked once the data system has been successfully initialized and loaded.
		/// Other systems can subscribe to this to know when it's safe to access game data.
		/// </summary>
		public Action onInitialized;

		[Tooltip("The root ScriptableObject that contains all game data models. This will be automatically found or created in the editor if not assigned.")]
		[SerializeField, CreateScriptableObject, AutoFill] protected T m_dataCollection;

		[Tooltip("The default delay in seconds before a requested save is actually executed. This helps to batch multiple save requests into one.")]
		[SerializeField, Range(1, 10)] protected int m_saveDelay = 3;

		[Tooltip("If true, the game data will be saved automatically when the application is paused.")]
		[SerializeField] protected bool m_saveOnPause = true;

		[Tooltip("If true, the game data will be saved automatically when the application is quit.")]
		[SerializeField] protected bool m_saveOnQuit = true;

		protected bool m_initialized;
		protected float m_saveCountdown;
		protected float m_saveDelayCustom;
		protected float m_lastSave;
		protected bool m_enableAutoSave = true;
		// -1: initial state, 0: paused, 1: resumed
		private int m_pauseState = -1;

		/// <summary>
		/// Gets a value indicating whether the data system has been initialized.
		/// </summary>
		public bool Initialzied => m_initialized;

		/// <summary>
		/// Provides public access to the root data collection ScriptableObject.
		/// </summary>
		public T DataCollection => m_dataCollection;

		//============================================================================
		// MonoBehaviour
		//============================================================================

		/// <summary>
		/// The Unity Update loop. After initialization, it propagates the update tick to the data collection
		/// and handles the countdown for any pending delayed save operations.
		/// </summary>
		protected virtual void Update()
		{
			if (!m_initialized)
				return;

			// Propagate the update event to all managed data models.
			m_dataCollection.OnUpdate(Time.deltaTime);

			// Handle the delayed save mechanism.
			if (m_saveCountdown > 0)
			{
				m_saveCountdown -= Time.deltaTime;
				if (m_saveCountdown <= 0)
					Save(true); // Save immediately when the countdown finishes.
			}
		}

		/// <summary>
		/// Handles the application pause event. It propagates the pause state to the data collection
		/// and triggers an auto-save if configured to do so.
		/// </summary>
		protected virtual void OnApplicationPause(bool pause)
		{
			// Prevent redundant calls if the pause state hasn't changed.
			if (!m_initialized || m_pauseState == (pause ? 0 : 1))
				return;

			m_pauseState = pause ? 0 : 1;
			m_dataCollection.OnPause(pause);

			if (pause && m_saveOnPause && m_enableAutoSave)
				Save(true);
		}

		/// <summary>
		/// Forwards the application focus event to the pause logic, increasing robustness across platforms.
		/// </summary>
		protected virtual void OnApplicationFocus(bool hasFocus)
		{
			OnApplicationPause(!hasFocus);
		}

		/// <summary>
		/// Handles the application quit event, triggering a final auto-save if configured.
		/// </summary>
		protected virtual void OnApplicationQuit()
		{
			if (m_initialized && m_saveOnQuit && m_enableAutoSave)
				Save(true);
		}

		//============================================================================
		// Public / Internal
		//============================================================================

		/// <summary>
		/// Initializes the data manager. It loads all data collections via the `m_dataCollection`
		/// and then triggers the post-load logic (e.g., calculating offline progress).
		/// </summary>
		public virtual void Init()
		{
			if (m_initialized)
				return;

			m_dataCollection.Load();
			m_dataCollection.PostLoad();
			m_initialized = true;
			onInitialized?.Invoke();

#if FIREBASE_REMOTE_CONFIG
			// We use reflection here to avoid a hard compile-time dependency on the RCore.Service assembly.
			// This allows this data module to be used in projects that do not include the Firebase service package.
			// The code dynamically finds the RFirebaseRemote class and subscribes to its static OnFetched event.
			try
			{
				var remoteConfigType = FindTypeInAssemblies("RCore.Service.RFirebaseRemote");
				if (remoteConfigType != null)
				{
					var onFetchedEvent = remoteConfigType.GetEvent("OnFetched", BindingFlags.Static | BindingFlags.Public);
					if (onFetchedEvent != null)
					{
						// Create a delegate of the correct type (Action) and point it to our local method.
						Action handler = OnRemoteConfigFetched;
						// For static events, the target object for AddEventHandler is null.
						onFetchedEvent.AddEventHandler(null, handler);
						Debug.Log($"[{GetType().Name}] Successfully subscribed to RFirebaseRemote.OnFetched event via reflection.");
					}
					else
					{
						Debug.LogWarning($"[{GetType().Name}] Could not find the static event 'OnFetched' on type 'RCore.Service.RFirebaseRemote'.");
					}
				}
				else
				{
					// This warning is helpful if the FIREBASE_REMOTE_CONFIG flag is defined but the RCore.Service assembly is missing.
					Debug.LogWarning($"[{GetType().Name}] Could not find the type 'RCore.Service.RFirebaseRemote'. Remote config integration will be skipped.");
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"[{GetType().Name}] An error occurred while trying to subscribe to RFirebaseRemote.OnFetched event via reflection. \n{ex}");
			}
#endif
		}

		/// <summary>
		/// Requests a save operation for all managed data.
		/// </summary>
		/// <param name="now">If true, saves the data immediately (with a small debounce). If false, starts a delayed save countdown.</param>
		/// <param name="saveDelayCustom">An optional custom delay to override the default `m_saveDelay` for this specific request.</param>
		/// <returns>True if the data was saved immediately, false if a delayed save was requested.</returns>
		public virtual bool Save(bool now = false, float saveDelayCustom = 0)
		{
			if (!m_initialized)
				return false;

			if (now)
			{
				// Debounce immediate saves to prevent spamming writes to disk.
				if (Time.unscaledTime - m_lastSave < 0.2f)
					return false;

				m_dataCollection.Save();
				m_saveDelayCustom = 0; // Reset any custom delay.
				m_saveCountdown = 0; // Cancel any pending delayed save.
				m_lastSave = Time.unscaledTime;
				return true;
			}

			// Request a delayed save.
			m_saveCountdown = m_saveDelay;
			if (saveDelayCustom > 0)
			{
				// Use the shortest requested delay.
				if (m_saveDelayCustom <= 0 || m_saveDelayCustom > saveDelayCustom)
					m_saveDelayCustom = saveDelayCustom;

				if (m_saveCountdown > m_saveDelayCustom)
					m_saveCountdown = m_saveDelayCustom;
			}
			return false;
		}

		/// <summary>
		/// Enables or disables automatic saving (on pause, on quit).
		/// </summary>
		public void EnableAutoSave(bool pValue) => m_enableAutoSave = pValue;

		/// <summary>
		/// Propagates the remote config fetched event to the data collection.
		/// This should be called by your remote config system after it successfully fetches new values.
		/// </summary>
		public void OnRemoteConfigFetched()
		{
			if (m_initialized)
				m_dataCollection.OnRemoteConfigFetched();
		}

		/// <summary>
		/// A convenience method to get the calculated offline time from the session model.
		/// </summary>
		/// <returns>The duration of the last offline period in seconds.</returns>
		public int GetOfflineSeconds() => m_dataCollection.session.GetOfflineSeconds();

		/// <summary>
		/// Helper method to find a Type by its name in any of the currently loaded assemblies.
		/// This is more robust than Type.GetType() which only checks the calling assembly and mscorlib.
		/// </summary>
		/// <param name="typeName">The full name of the type to find.</param>
		/// <returns>The Type object if found, otherwise null.</returns>
		private static Type FindTypeInAssemblies(string typeName)
		{
			var type = Type.GetType(typeName);
			if (type != null)
				return type;

			foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
			{
				type = a.GetType(typeName);
				if (type != null)
					return type;
			}
			return null;
		}
	}
}