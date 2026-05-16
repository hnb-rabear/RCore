namespace RevCore
{
	/// <summary>
	/// Read-only view of a single pending timer plus a <see cref="Cancel"/> button. Returned from
	/// <see cref="ITimerScheduler.WaitForSeconds(float, System.Action, bool, int)"/> and similar.
	/// </summary>
	public interface ITimerHandle
	{
		/// <summary>Logical identifier supplied at scheduling time; 0 if untracked.</summary>
		int Id { get; }

		/// <summary>True when the timer has not yet completed or been cancelled.</summary>
		bool IsRunning { get; }

		/// <summary>True when the timer's callback has fired.</summary>
		bool IsCompleted { get; }

		/// <summary>True when the timer was cancelled (callback will not fire).</summary>
		bool IsCancelled { get; }

		/// <summary>Seconds elapsed since scheduling.</summary>
		float Elapsed { get; }

		/// <summary>Total seconds the timer will run for. Zero for condition timers.</summary>
		float Duration { get; }

		/// <summary>Seconds left until completion. Zero once completed.</summary>
		float Remaining { get; }

		/// <summary>Cancels the timer. Idempotent — additional calls have no effect.</summary>
		void Cancel();
	}
}
