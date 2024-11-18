using UnityEngine;

namespace RCore.Data.JObject
{
	public abstract class JObjectDBManager<T> : MonoBehaviour where T : JObjectsCollection
	{
		[SerializeField] protected T m_jObjectsCollection;
		[SerializeField, Range(1, 10)] protected int m_saveDelay = 3;
		[SerializeField] protected bool m_enabledSave = true;
		[SerializeField] protected bool m_saveOnPause = true;
		[SerializeField] protected bool m_saveOnQuit = true;

		protected bool m_initialized;
		protected float m_saveCountdown;
		protected float m_saveDelayCustom;
		protected float m_lastSave;
		protected int m_pauseState = -1;

		public bool Initialzied => m_initialized;
		public T JObjectsCollection => m_jObjectsCollection;

		protected virtual void Update()
		{
			if (!m_initialized)
				return;

			foreach (var handler in m_jObjectsCollection.handlers)
				handler.OnUpdate(Time.deltaTime);

			//Save with a delay to prevent too many save calls in a short period of time
			if (m_saveCountdown > 0)
			{
				m_saveCountdown -= Time.deltaTime;
				if (m_saveCountdown <= 0)
					Save(true);
			}
		}

		protected virtual void OnApplicationPause(bool pause)
		{
			if (!m_initialized || m_pauseState == (pause ? 0 : 1))
				return;

			m_pauseState = pause ? 0 : 1;
			int utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
			int offlineSeconds = 0;
			if (!pause)
				offlineSeconds = GetOfflineSeconds();
			foreach (var handler in m_jObjectsCollection.handlers)
				handler.OnPause(pause, utcNowTimestamp, offlineSeconds);
			if (pause && m_saveOnPause)
				Save(true);
		}

		protected virtual void OnApplicationFocus(bool hasFocus)
		{
			OnApplicationPause(!hasFocus);
		}

		protected virtual void OnApplicationQuit()
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
		public virtual void Init()
		{
			if (m_initialized)
				return;

			m_jObjectsCollection.Load();
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
				foreach (var handler in m_jObjectsCollection.handlers)
					handler.OnPreSave(utcNowTimestamp);
				foreach (var collection in m_jObjectsCollection.datas)
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

			m_jObjectsCollection.datas.Import(data);
			foreach (var collection in m_jObjectsCollection.datas)
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
			if (m_jObjectsCollection.sessionData.lastActive > 0)
			{
				int utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
				offlineSeconds = utcNowTimestamp - m_jObjectsCollection.sessionData.lastActive;
			}
			return offlineSeconds;
		}

		//============================================================================
		// Private / Protected
		//============================================================================

		protected virtual void PostLoad()
		{
			int offlineSeconds = GetOfflineSeconds();
			var utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
			foreach (var handler in m_jObjectsCollection.handlers)
				handler.OnPostLoad(utcNowTimestamp, offlineSeconds);
		}
	}
}