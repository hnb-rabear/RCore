using System;
using UnityEngine;

namespace RevCore
{
    /// <summary>
    /// MonoBehaviour that drives a <see cref="JObjectModelCollection"/> through its full lifecycle:
    /// initialize on first ready, autosave on a delay, force-save on application pause / quit, and
    /// per-frame tick. Subclass with a concrete collection type and place once in your bootstrap scene.
    /// </summary>
    /// <typeparam name="T">A concrete <see cref="JObjectModelCollection"/> subclass.</typeparam>
    public abstract class JObjectDBManager<T> : MonoBehaviour where T : JObjectModelCollection
    {
        /// <summary>Invoked once after <see cref="Init"/> succeeds.</summary>
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

        /// <summary>True once <see cref="Init"/> has completed.</summary>
        public bool Initialized => m_initialized;

        /// <summary>The managed <see cref="JObjectModelCollection"/> instance.</summary>
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

        /// <summary>Loads, injects dependencies into, and post-loads the managed collection. Idempotent.</summary>
        public virtual void Init()
        {
            if (m_initialized) return;
            m_dataCollection.Load();
            m_dataCollection.InjectDependencies();
            m_dataCollection.PostLoad();
            m_initialized = true;
            onInitialized?.Invoke();
        }

        /// <summary>
        /// Schedules or executes a save. When <paramref name="now"/> is <c>false</c>, queues a save for
        /// <see cref="m_saveDelay"/> seconds out (or sooner if <paramref name="saveDelayCustom"/> is shorter).
        /// When <paramref name="now"/> is <c>true</c>, saves immediately — but returns <c>false</c> if called
        /// within 200 ms of the last successful save (rate-limit). Use <see cref="SaveForced"/> to bypass.
        /// </summary>
        /// <returns><c>true</c> when a write actually happened; <c>false</c> for queued / throttled / not-initialized.</returns>
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

        /// <summary>Enables / disables the autosave queue. The pause / quit force-save lifecycle still runs when this is <c>false</c>.</summary>
        public void EnableAutoSave(bool value) => m_enableAutoSave = value;

        /// <summary>Forwards a remote-config refresh to the managed collection.</summary>
        public void OnRemoteConfigFetched()
        {
            if (m_initialized)
                m_dataCollection.OnRemoteConfigFetched();
        }

        /// <summary>Convenience pass-through to <see cref="SessionModel.GetOfflineSeconds"/>.</summary>
        public int GetOfflineSeconds() => m_dataCollection.session.GetOfflineSeconds();
    }
}
