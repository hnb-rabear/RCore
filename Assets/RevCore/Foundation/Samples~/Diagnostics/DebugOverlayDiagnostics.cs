using System;
using RevCore;
using UnityEngine;

namespace RevCore.Samples.Diagnostics
{
    /// <summary>
    /// MonoBehaviour that wires itself in as the active <see cref="RevDiagnostics.Listener"/>
    /// on <c>Awake</c> and draws a small on-screen overlay with active-timer count,
    /// events/second, and spawns/second. Useful for spotting per-frame activity spikes during
    /// development without opening the profiler.
    /// </summary>
    public sealed class DebugOverlayDiagnostics : MonoBehaviour, IRevDiagnostics
    {
        private int m_activeTimers;
        private int m_eventsThisSec;
        private int m_spawnsThisSec;
        private float m_resetAt;

        private void Awake() => RevDiagnostics.Listener = this;

        private void OnDestroy()
        {
            if (ReferenceEquals(RevDiagnostics.Listener, this))
                RevDiagnostics.Listener = null;
        }

        public void OnTimerScheduled(int id, float duration, bool unscaled) => m_activeTimers++;
        public void OnTimerCancelled(int id) => m_activeTimers--;
        public void OnTimerCompleted(int id, float overtime) => m_activeTimers--;
        public void OnEventPublished(Type eventType, int listenerCount) => m_eventsThisSec++;
        public void OnEventSubscribed(Type eventType, int newCount) { }
        public void OnEventUnsubscribed(Type eventType, int newCount) { }
        public void OnPoolSpawn(string poolName, bool reused) => m_spawnsThisSec++;
        public void OnPoolRelease(string poolName) { }
        public void OnAudioPlaySFX(string clipName) { }
        public void OnAudioPlayMusic(string clipName, bool looping) { }

        private void OnGUI()
        {
            if (Time.unscaledTime >= m_resetAt)
            {
                m_eventsThisSec = 0;
                m_spawnsThisSec = 0;
                m_resetAt = Time.unscaledTime + 1f;
            }
            GUI.Label(new Rect(10, 10, 300, 20), $"Active timers: {m_activeTimers}");
            GUI.Label(new Rect(10, 30, 300, 20), $"Events/sec: {m_eventsThisSec}");
            GUI.Label(new Rect(10, 50, 300, 20), $"Spawns/sec: {m_spawnsThisSec}");
        }
    }
}
