using System;
using UnityEngine;

namespace RevCore
{
    public abstract class JObjectDBManager<T> : MonoBehaviour where T : JObjectModelCollection
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

        public bool Initialized => m_initialized;
        public T DataCollection => m_dataCollection;

        protected virtual void Update()
        {
            if (!m_initialized) return;
            m_dataCollection.OnUpdate(Time.deltaTime);
            if (m_saveCountdown > 0)
            {
                m_saveCountdown -= Time.deltaTime;
                if (m_saveCountdown <= 0)
                    Save(true);
            }
        }

        protected virtual void OnApplicationPause(bool pause)
        {
            if (!m_initialized || m_pauseState == (pause ? 0 : 1)) return;
            m_pauseState = pause ? 0 : 1;
            m_dataCollection.OnPause(pause);
            if (pause && m_saveOnPause && m_enableAutoSave)
                SaveForced();
        }

        protected virtual void OnApplicationFocus(bool hasFocus) => OnApplicationPause(!hasFocus);

        protected virtual void OnApplicationQuit()
        {
            if (m_initialized && m_saveOnQuit && m_enableAutoSave)
                SaveForced();
        }

        public virtual void Init()
        {
            if (m_initialized) return;
            m_dataCollection.Load();
            m_dataCollection.InjectDependencies();
            m_dataCollection.PostLoad();
            m_initialized = true;
            onInitialized?.Invoke();
        }

        public virtual bool Save(bool now = false, float saveDelayCustom = 0)
        {
            if (!m_initialized) return false;
            if (now)
            {
                if (Time.unscaledTime - m_lastSave < 0.2f) return false;
                m_dataCollection.Save();
                m_saveDelayCustom = 0;
                m_saveCountdown = 0;
                m_lastSave = Time.unscaledTime;
                return true;
            }
            m_saveCountdown = m_saveDelay;
            if (saveDelayCustom > 0)
            {
                if (m_saveDelayCustom <= 0 || m_saveDelayCustom > saveDelayCustom)
                    m_saveDelayCustom = saveDelayCustom;
                if (m_saveCountdown > m_saveDelayCustom)
                    m_saveCountdown = m_saveDelayCustom;
            }
            return false;
        }

        /// <summary>
        /// Saves immediately, bypassing the 200 ms throttle that <see cref="Save"/>(now: true)
        /// applies. Intended for end-of-life events (application pause, quit) and any
        /// caller that needs a guaranteed write before yielding control. No-op when
        /// the manager has not been initialized.
        /// </summary>
        /// <returns><c>true</c> when the write was issued; <c>false</c> when the manager
        /// was not yet initialized.</returns>
        public virtual bool SaveForced()
        {
            if (!m_initialized) return false;
            m_dataCollection.Save();
            m_saveDelayCustom = 0;
            m_saveCountdown = 0;
            m_lastSave = Time.unscaledTime;
            return true;
        }

        public void EnableAutoSave(bool value) => m_enableAutoSave = value;

        public void OnRemoteConfigFetched()
        {
            if (m_initialized)
                m_dataCollection.OnRemoteConfigFetched();
        }

        public int GetOfflineSeconds() => m_dataCollection.session.GetOfflineSeconds();
    }
}
