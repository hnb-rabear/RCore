using System;
using System.Collections.Generic;
using RevCore;
using UnityEngine;

namespace RevCore.Samples.Diagnostics
{
    /// <summary>
    /// Records the last N framework events as a ring buffer of strings. Drop into a singleton
    /// MonoBehaviour at startup and assign to <see cref="RevDiagnostics.Listener"/>; in your
    /// crash handler call <see cref="Dump"/> and attach the trace to Crashlytics / Sentry
    /// custom keys. Capacity is configurable; default is 100 frames of events.
    /// </summary>
    /// <remarks>
    /// Each entry is prefixed with <see cref="Time.frameCount"/> so you can reconstruct the
    /// approximate timing without a real clock. The queue is unsynchronized — call only on
    /// the main thread.
    /// </remarks>
    public sealed class CrashBufferDiagnostics : IRevDiagnostics
    {
        private readonly Queue<string> m_buffer = new();
        private readonly int m_capacity;

        public CrashBufferDiagnostics(int capacity = 100) => m_capacity = capacity;

        public string Dump() => string.Join("\n", m_buffer);

        public void OnTimerScheduled(int id, float duration, bool unscaled) => Push($"Timer[{id}] sched dur={duration:F2} u={unscaled}");
        public void OnTimerCancelled(int id) => Push($"Timer[{id}] cancel");
        public void OnTimerCompleted(int id, float overtime) => Push($"Timer[{id}] done over={overtime:F3}");
        public void OnEventPublished(Type eventType, int listenerCount) => Push($"Pub {eventType.Name} (->{listenerCount})");
        public void OnEventSubscribed(Type eventType, int newCount) => Push($"Sub {eventType.Name} (now {newCount})");
        public void OnEventUnsubscribed(Type eventType, int newCount) => Push($"Unsub {eventType.Name} (now {newCount})");
        public void OnPoolSpawn(string poolName, bool reused) => Push($"Pool {poolName} {(reused ? "reuse" : "NEW")}");
        public void OnPoolRelease(string poolName) => Push($"Pool {poolName} release");
        public void OnAudioPlaySFX(string clipName) => Push($"SFX {clipName}");
        public void OnAudioPlayMusic(string clipName, bool looping) => Push($"Music {clipName} loop={looping}");

        private void Push(string msg)
        {
            m_buffer.Enqueue($"{Time.frameCount}: {msg}");
            while (m_buffer.Count > m_capacity)
                m_buffer.Dequeue();
        }
    }
}
