/**
 * Author HNB-RaBear - 2024
 **/

using UnityEngine;
using System;
using RCore.Inspector;

namespace RCore.Data.JObject
{
	/// <summary>
	/// A generic MonoBehaviour that acts as the main orchestrator for the JObject data system.
	/// It manages the lifecycle of a `JObjectDataCollection`, handling initialization, saving, and pausing.
	/// This class should be placed on a persistent GameObject in your scene (often a singleton manager).
	/// </summary>
	/// <typeparam name="T">The specific type of JObjectDataCollection this manager will control.</typeparam>
	public abstract class JObjectDBManager<T> : MonoBehaviour where T : JObjectDataCollection
	{
		/// <summary>
		/// An action invoked once the data system has been successfully initialized.
		/// Other systems can subscribe to this to know when it's safe to access game data.
		/// </summary>
		public Action onInitialized;
		
		[Tooltip("The main ScriptableObject that contains all game data collections. This will be automatically found if not assigned.")]
		[SerializeField, AutoFill] protected T m_dataCollection;
		
		[Tooltip("The default delay in seconds before a save request is actually executed. This prevents frequent, unnecessary saves.")]
		[SerializeField, Range(1, 10)] protected int m_saveDelay = 3;
		
		[Tooltip("A master switch to enable or disable all saving functionality.")]
		[SerializeField] protected bool m_enabledSave = true;
		
		[Tooltip("If true, the game data will be saved automatically when the application is paused.")]
		[SerializeField] protected bool m_saveOnPause = true;
		
		[Tooltip("If true, the game data will be saved automatically when the application is quit.")]
		[SerializeField] protected bool m_saveOnQuit = true;
		
		protected bool m_initialized;
		protected float m_saveCountdown;
		protected float m_saveDelayCustom;
		protected float m_lastSave;
		// -1: initial state, 0: paused, 1: resumed
		protected int m_pauseState = -1; 
		protected bool m_enableAutoSave = true;
		
		/// <summary>
		/// Gets a value indicating whether the data system has been initialized.
		/// </summary>
		public bool Initialzied => m_initialized;

		/// <summary>
		/// Provides public access to the root data collection.
		/// </summary>
		public T DataCollection => m_dataCollection;

		//============================================================================
		// MonoBehaviour
		//============================================================================
		
		/// <summary>
		/// The Unity Update loop. It propagates the update event to the data collection
		/// and handles the countdown for delayed saves.
		/// </summary>
		protected virtual void Update()
		{
			if (!m_initialized)
				return;

			// Propagate the update tick to all data handlers.
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
		/// Handles the application pause event, propagating it to the data collection
		/// and triggering an auto-save if configured.
		/// </summary>
		protected virtual void OnApplicationPause(bool pause)
		{
			// Prevent redundant calls.
			if (!m_initialized || m_pauseState == (pause ? 0 : 1))
				return;

			m_pauseState = pause ? 0 : 1;
			int utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
			int offlineSeconds = pause ? 0 : GetOfflineSeconds();

			m_dataCollection.OnPause(pause, utcNowTimestamp, offlineSeconds);

			if (pause && m_saveOnPause && m_enableAutoSave)
				Save(true);
		}
		
		/// <summary>
		/// On some platforms, OnApplicationFocus is more reliable than OnApplicationPause.
		/// This forwards the focus event to the pause logic.
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
		/// Initializes the data manager. It loads all data collections and triggers the post-load logic.
		/// </summary>
		public virtual void Init()
		{
			if (m_initialized)
				return;

			m_dataCollection.Load();
			PostLoad();
			m_initialized = true;
			onInitialized?.Invoke();
		}

		/// <summary>
		/// Requests a save operation.
		/// </summary>
		/// <param name="now">If true, saves the data immediately (with a small debounce). If false, starts a countdown based on `m_saveDelay`.</param>
		/// <param name="saveDelayCustom">An optional custom delay to override the default for this specific save request.</param>
		public virtual void Save(bool now = false, float saveDelayCustom = 0)
		{
			if (!m_enabledSave || !m_initialized)
				return;

			if (now)
			{
				// Debounce immediate saves to prevent spamming PlayerPrefs.
				if (Time.unscaledTime - m_lastSave < 0.2f)
					return;
				
				m_dataCollection.Save();
				m_saveDelayCustom = 0; // Reset any custom delay.
				m_saveCountdown = 0;   // Cancel any pending delayed save.
				m_lastSave = Time.unscaledTime;
				return;
			}

			// Request a delayed save.
			m_saveCountdown = m_saveDelay;
			// Allow for a custom, potentially shorter, delay.
			if (saveDelayCustom > 0)
			{
				if (m_saveDelayCustom <= 0 || m_saveDelayCustom > saveDelayCustom)
					m_saveDelayCustom = saveDelayCustom;
				
				if (m_saveCountdown > m_saveDelayCustom)
					m_saveCountdown = m_saveDelayCustom;
			}
		}
		
		/// <summary>
		/// Imports data from a JSON string, then re-initializes the post-load logic.
		/// </summary>
		/// <param name="data">The JSON data string to import.</param>
		public virtual void Import(string data)
		{
			if (!m_enabledSave)
				return;
				
			m_dataCollection.Import(data);
			PostLoad(); // Re-run post-load logic to apply imported data.
		}

		/// <summary>
		/// Globally enables or disables the saving functionality.
		/// </summary>
		public virtual void EnableSave(bool value)
		{
			m_enabledSave = value;
		}

		/// <summary>
		/// Enables or disables automatic saving (on pause, on quit).
		/// </summary>
		protected void EnableAutoSave(bool pValue)
		{
			m_enableAutoSave = pValue;
		}

		/// <summary>
		/// Calculates the time in seconds that has passed since the last active session.
		/// </summary>
		/// <returns>The duration of the offline period in seconds.</returns>
		public virtual int GetOfflineSeconds()
		{
			int offlineSeconds = 0;
			if (m_dataCollection.sessionData.lastActive > 0)
			{
				int utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
				offlineSeconds = utcNowTimestamp - m_dataCollection.sessionData.lastActive;
			}
			// Ensure offline time is not negative.
			return Mathf.Max(0, offlineSeconds);
		}

		//============================================================================
		// Private / Protected
		//============================================================================
		
		/// <summary>
		/// A hook that is called after the initial data load. It calculates offline time
		/// and propagates the `OnPostLoad` event to all data handlers.
		/// </summary>
		protected virtual void PostLoad()
		{
			int offlineSeconds = GetOfflineSeconds();
			var utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
			m_dataCollection.OnPostLoad(utcNowTimestamp, offlineSeconds);
		}
	}
}