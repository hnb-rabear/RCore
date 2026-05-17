using System;
using RevCore;
using UnityEngine;

namespace RevCore.Samples.Diagnostics
{
    /// <summary>
    /// Simplest possible listener — logs every framework event to the Unity Console with a
    /// <c>[Rev]</c> prefix. Useful for an ad-hoc "I want to see everything RevCore is doing"
    /// debug session. Verbose; do not ship to production builds.
    /// </summary>
    public sealed class UnityLogDiagnostics : IRevDiagnostics
    {
        public void OnTimerScheduled(int id, float duration, bool unscaled) => Debug.Log($"[Rev] Timer[{id}] sched dur={duration:F2} u={unscaled}");
        public void OnTimerCancelled(int id) => Debug.Log($"[Rev] Timer[{id}] cancel");
        public void OnTimerCompleted(int id, float overtime) => Debug.Log($"[Rev] Timer[{id}] done over={overtime:F3}");
        public void OnEventPublished(Type eventType, int listenerCount) => Debug.Log($"[Rev] Pub {eventType.Name} (->{listenerCount})");
        public void OnEventSubscribed(Type eventType, int newCount) => Debug.Log($"[Rev] Sub {eventType.Name} (now {newCount})");
        public void OnEventUnsubscribed(Type eventType, int newCount) => Debug.Log($"[Rev] Unsub {eventType.Name} (now {newCount})");
        public void OnPoolSpawn(string poolName, bool reused) => Debug.Log($"[Rev] Pool {poolName} {(reused ? "reuse" : "NEW")}");
        public void OnPoolRelease(string poolName) => Debug.Log($"[Rev] Pool {poolName} release");
        public void OnAudioPlaySFX(string clipName) => Debug.Log($"[Rev] SFX {clipName}");
        public void OnAudioPlayMusic(string clipName, bool looping) => Debug.Log($"[Rev] Music {clipName} loop={looping}");
    }
}
