using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.Data.JObject
{
	public abstract class JObjectDBManager : MonoBehaviour
	{
		[SerializeField, Range(1, 10)] private int m_saveDelay = 3;
		[SerializeField] private bool m_enabledSave = true;
		[SerializeField] private bool m_saveOnPause = true;
		[SerializeField] private bool m_saveOnQuit = true;

		protected List<JObjectCollection> m_collections = new List<JObjectCollection>();
		protected List<IJObjectHandler> m_handlers = new List<IJObjectHandler>();
		
		public SessionData sessionData;
		public SessionDataHandler sessionDataHandler;
		
		protected bool m_initialized;
		private float m_saveCountdown;
		private float m_saveDelayCustom;
		private float m_lastSave;
		private int m_pauseState = -1;

		public bool Initialzied => m_initialized;

		private void Update()
		{
			if (!m_initialized)
				return;
			
			foreach (var handler in m_handlers)
				handler.OnUpdate(Time.deltaTime);

			//Save with a delay to prevent too many save calls in a short period of time
			if (m_saveCountdown > 0)
			{
				m_saveCountdown -= Time.deltaTime;
				if (m_saveCountdown <= 0)
					Save(true);
			}
		}

		private void OnApplicationPause(bool pause)
		{
			if (!m_initialized || m_pauseState == (pause ? 0 : 1))
				return;

			m_pauseState = pause ? 0 : 1;
			int utcNowTimestamp = TimeHelper.GetUtcNowTimestamp();
			int offlineSeconds = 0;
			if (!pause)
				offlineSeconds = GetOfflineSeconds();
			foreach (var handler in m_handlers)
				handler.OnPause(pause, utcNowTimestamp, offlineSeconds);
			if (pause && m_saveOnPause)
				Save(true);
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			OnApplicationPause(!hasFocus);
		}

		private void OnApplicationQuit()
		{
			if (m_initialized && m_saveOnQuit)
				Save(true);
		}

		//============================================================================
		// Public / Internal
		//============================================================================
		
		/// <summary>
		/// Initialize DB Manager
		/// </summary>
		public void Init()
		{
			if (m_initialized)
				return;

			(sessionData, sessionDataHandler) = CreateModule<SessionData, SessionDataHandler, JObjectDBManager>("UserSession");
			Load();
			PostLoad();
			m_initialized = true;
		}
		
		public virtual void Save(bool now = false, float saveDelayCustom = 0)
		{
			if (!m_enabledSave || !m_initialized)
				return;
			
			if (now)
			{
				// Do not allow multiple Save calls within a short period of time.
				if (Time.unscaledTime - m_lastSave < 0.2f)
					return;
				int utcNowTimestamp = TimeHelper.GetUtcNowTimestamp();
				foreach (var handler in m_handlers)
					handler.OnPreSave(utcNowTimestamp);
				foreach (var collection in m_collections)
					collection.Save();
				m_saveDelayCustom = 0; // Reset save delay custom
				m_lastSave = Time.unscaledTime;
				return;
			}
			
			m_saveCountdown = m_saveDelay;
			if (saveDelayCustom > 0)
			{
				if (m_saveDelayCustom <= 0)
					m_saveDelayCustom = saveDelayCustom;
				else if (m_saveDelayCustom > saveDelayCustom)
					m_saveDelayCustom = saveDelayCustom;
				if (m_saveCountdown > m_saveDelayCustom)
					m_saveCountdown = m_saveDelayCustom;
			}
		}

		public virtual void Import(string data)
		{
			if (!m_enabledSave)
				return;

			m_collections.Import(data);
			foreach (var collection in m_collections)
				collection.Load();
			PostLoad();
		}

		public virtual void EnableSave(bool value)
		{
			m_enabledSave = value;
		}

		public virtual int GetOfflineSeconds()
		{
			int offlineSeconds = 0;
			if (sessionData.lastActive > 0)
			{
				int utcNowTimestamp = TimeHelper.GetUtcNowTimestamp();
				offlineSeconds = utcNowTimestamp - sessionData.lastActive;
			}
			return offlineSeconds;
		}

		//============================================================================
		// Private / Protected
		//============================================================================

		/// <summary>
		/// Override this method then create DB Collections and Controller in here
		/// </summary>
		protected abstract void Load();
		
		protected TCollection CreateCollection<TCollection>(string key, TCollection defaultVal = null)
			where TCollection : JObjectCollection, new()
		{
			if (string.IsNullOrEmpty(key))
				key = typeof(TCollection).Name;
			var newCollection = JObjectDB.CreateCollection(key, defaultVal);
			if (newCollection != null)
				m_collections.Add(newCollection);
			return newCollection;
		}
		
		protected THandler CreateController<THandler, TManager>()
			where THandler : JObjectHandler<TManager>
			where TManager : JObjectDBManager
		{
			var newController = gameObject.AddComponent<THandler>();
			newController.dbManager = this as TManager;
			
			m_handlers.Add(newController);
			return newController;
		}

		protected (TCollection, THandler) CreateModule<TCollection, THandler, TManager>(string key, TCollection defaultVal = null) 
			where TCollection : JObjectCollection, new()
			where THandler : JObjectHandler<TManager>
			where TManager : JObjectDBManager
		{
			var collection = CreateCollection(key, defaultVal);
			var controller = CreateController<THandler, TManager>();
			return (collection, controller);
		}
		
		protected void PostLoad()
		{
			int offlineSeconds = GetOfflineSeconds();
			var utcNowTimestamp = TimeHelper.GetUtcNowTimestamp();
			foreach (var handler in m_handlers)
				handler.OnPostLoad(utcNowTimestamp, offlineSeconds);
		}
	}
}