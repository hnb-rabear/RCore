using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.Data.JObject
{
	public abstract class JObjectDBManager : MonoBehaviour
	{
		[SerializeField] private JObjectCollectionSO m_jObjectCollection;
		[SerializeField, Range(1, 10)] private int m_saveDelay = 3;
		[SerializeField] private bool m_enabledSave = true;
		[SerializeField] private bool m_saveOnPause = true;
		[SerializeField] private bool m_saveOnQuit = true;
		
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
			
			foreach (var handler in m_jObjectCollection.handlers)
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
			int utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
			int offlineSeconds = 0;
			if (!pause)
				offlineSeconds = GetOfflineSeconds();
			foreach (var handler in m_jObjectCollection.handlers)
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

			m_jObjectCollection.Load();
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
				int utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
				foreach (var handler in m_jObjectCollection.handlers)
					handler.OnPreSave(utcNowTimestamp);
				foreach (var collection in m_jObjectCollection.collections)
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

			m_jObjectCollection.collections.Import(data);
			foreach (var collection in m_jObjectCollection.collections)
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
			if (m_jObjectCollection.sessionData.lastActive > 0)
			{
				int utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
				offlineSeconds = utcNowTimestamp - m_jObjectCollection.sessionData.lastActive;
			}
			return offlineSeconds;
		}

		//============================================================================
		// Private / Protected
		//============================================================================
		
		protected void PostLoad()
		{
			int offlineSeconds = GetOfflineSeconds();
			var utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
			foreach (var handler in m_jObjectCollection.handlers)
				handler.OnPostLoad(utcNowTimestamp, offlineSeconds);
		}
	}
}