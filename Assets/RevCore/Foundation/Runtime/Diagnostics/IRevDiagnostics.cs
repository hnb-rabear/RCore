using System;

namespace RevCore
{
	/// <summary>
	/// Observability hooks that the RevCore framework fires on its hot paths. Consumer projects
	/// can wire a single implementation through <see cref="RevDiagnostics.Listener"/> to record
	/// crash traces, drive a debug overlay, forward to analytics, or assert framework activity
	/// in tests. Default state (no listener assigned) costs one null check per hook site.
	/// </summary>
	/// <remarks>
	/// Every hook fires <b>after</b> the underlying operation succeeds. Operations that early-return
	/// (no-op cancels, dedup'd subscribes, validation failures) skip the hook entirely. All hooks
	/// run on the main thread; an implementation that forwards to a background queue is responsible
	/// for its own marshalling.
	/// </remarks>
	public interface IRevDiagnostics
	{
		/// <summary>Fires after a timer with <paramref name="id"/> is added to the scheduler. Not fired for instant-complete schedules (<c>seconds &lt;= 0</c>).</summary>
		void OnTimerScheduled(int id, float duration, bool unscaled);

		/// <summary>Fires after <see cref="ITimerHandle.Cancel"/> flips <c>IsCancelled</c>. Not fired when the handle was already cancelled or completed.</summary>
		void OnTimerCancelled(int id);

		/// <summary>Fires after a timer's user callback runs and before it is removed from the scheduler. Not fired for timers removed via cancellation.</summary>
		void OnTimerCompleted(int id, float overtime);

		/// <summary>Fires once per <c>Publish&lt;T&gt;</c> call. <paramref name="listenerCount"/> is zero when the event had no subscribers — the hook still fires so observers can record fire-and-forget publishes.</summary>
		void OnEventPublished(Type eventType, int listenerCount);

		/// <summary>Fires after a new listener is appended. Skipped on dedup hit (listener already in the invocation list).</summary>
		void OnEventSubscribed(Type eventType, int newCount);

		/// <summary>Fires after a listener is removed. Skipped when the listener wasn't registered.</summary>
		void OnEventUnsubscribed(Type eventType, int newCount);

		/// <summary>Fires after a pool moves an item to the active bucket. <paramref name="reused"/> is <c>false</c> for fresh instantiations.</summary>
		void OnPoolSpawn(string poolName, bool reused);

		/// <summary>Fires after a pool moves an item to the inactive bucket. Skipped when the item was null or not currently active.</summary>
		void OnPoolRelease(string poolName);

		/// <summary>Fires after a sound effect actually plays. Skipped when SFX is disabled, the clip is null, or no audio collection is bound.</summary>
		void OnAudioPlaySFX(string clipName);

		/// <summary>Fires after the music source starts playing <paramref name="clipName"/>. Skipped when the clip is null.</summary>
		void OnAudioPlayMusic(string clipName, bool looping);
	}
}
