using System;
using RCore.Inspector;
using UnityEngine;

namespace RCore.Data.JObject
{
	public abstract class JObjectDBManagerV2<T> : MonoBehaviour where T : JObjectModelCollection
	{
		public Action onInitialized;
		[SerializeField, CreateScriptableObject, AutoFill] protected T m_dataCollection;
		[SerializeField, Range(1, 10)] protected int m_saveDelay = 3;
		[SerializeField] protected bool m_saveOnPause = true;
		[SerializeField] protected bool m_saveOnQuit = true;

		protected bool m_initialized;
		protected float m_saveCountdown;
		protected float m_saveDelayCustom;
		protected float m_lastSave;
		protected bool m_enableAutoSave = true;
		private int m_pauseState = -1;

		public bool Initialzied => m_initialized;

		public T DataCollection => m_dataCollection;

		//============================================================================
		// MonoBehaviour
		//============================================================================

		protected virtual void Update()
		{
			if (!m_initialized)
				return;

			m_dataCollection.OnUpdate(Time.deltaTime);

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
			m_dataCollection.OnPause(pause);

			if (pause && m_saveOnPause && m_enableAutoSave)
				Save(true);
		}

		protected virtual void OnApplicationFocus(bool hasFocus)
		{
			OnApplicationPause(!hasFocus);
		}

		protected virtual void OnApplicationQuit()
		{
			if (m_initialized && m_saveOnQuit && m_enableAutoSave)
				Save(true);
		}

		//============================================================================
		// Public / Internal
		//============================================================================

		public virtual void Init()
		{
			if (m_initialized)
				return;

			m_dataCollection.Load();
			m_dataCollection.PostLoad();
			m_initialized = true;
			onInitialized?.Invoke();
		}

		public virtual bool Save(bool now = false, float saveDelayCustom = 0)
		{
			if (!m_initialized)
				return false;

			if (now)
			{
				// Do not allow multiple Save calls within a short period of time.
				if (Time.unscaledTime - m_lastSave < 0.2f)
					return false;
				m_dataCollection.Save();
				m_saveDelayCustom = 0; // Reset save delay custom
				m_lastSave = Time.unscaledTime;
				return true;
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
			return false;
		}

		public void EnableAutoSave(bool pValue) => m_enableAutoSave = pValue;

		public int GetOfflineSeconds() => m_dataCollection.session.GetOfflineSeconds();
	}
}