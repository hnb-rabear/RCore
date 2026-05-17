using System;

namespace RevCore
{
	/// <summary>
	/// Schedules deferred work. The default implementation is <see cref="TimerScheduler"/>; instances
	/// are driven each frame by a <see cref="TimerDriver"/> MonoBehaviour or a scene-scoped equivalent.
	/// Two flavors of "wait":
	/// <list type="bullet">
	///   <item><see cref="WaitForSeconds(float, Action, bool, int)"/> — fires after a wall-clock delay.</item>
	///   <item><see cref="WaitForCondition"/> — polls each tick until a predicate returns <c>true</c>.</item>
	/// </list>
	/// </summary>
	public interface ITimerScheduler
	{
		/// <summary>Total number of pending timers across countdown + condition + enqueued action lists.</summary>
		int ActiveCount { get; }

		/// <summary>
		/// Schedules <paramref name="onComplete"/> after <paramref name="seconds"/>. Returns a handle for
		/// inspection or cancellation. Non-positive delays complete on the next tick.
		/// </summary>
		/// <param name="seconds">Delay in seconds.</param>
		/// <param name="onComplete">Callback invoked when the timer elapses.</param>
		/// <param name="unscaledTime">When <c>true</c>, advances by <see cref="UnityEngine.Time.unscaledDeltaTime"/> instead of <see cref="UnityEngine.Time.deltaTime"/>.</param>
		/// <param name="id">Optional logical identifier — pass a non-zero value to enable <see cref="Cancel(int)"/> and to replace any existing timer with the same id.</param>
		ITimerHandle WaitForSeconds(float seconds, Action onComplete, bool unscaledTime = false, int id = 0);

		/// <summary>Same as <see cref="WaitForSeconds(float, Action, bool, int)"/> but the callback receives the actual elapsed seconds.</summary>
		ITimerHandle WaitForSeconds(float seconds, Action<float> onComplete, bool unscaledTime = false, int id = 0);

		/// <summary>
		/// Schedules <paramref name="onComplete"/> to fire the first tick at which <paramref name="condition"/>
		/// returns <c>true</c>. The poll runs every tick — keep the predicate cheap.
		/// </summary>
		ITimerHandle WaitForCondition(ConditionalDelegate condition, Action onComplete, int id = 0);

		/// <summary>
		/// Schedules a global-bus publish of <paramref name="evt"/> after <paramref name="seconds"/>. A
		/// second call before the timer fires replaces the pending one (debounce semantics keyed on event type).
		/// </summary>
		ITimerHandle Debounce<T>(T evt, float seconds) where T : IEvent;

		/// <summary>Queues <paramref name="action"/> to run on the next <see cref="Tick"/>. Thread-safe — used to marshal background work back to the main thread.</summary>
		void Enqueue(Action action);

		/// <summary>
		/// Drives every timer forward by one frame. Called by the scheduler's driver MonoBehaviour;
		/// consumers normally do not call this directly.
		/// </summary>
		void Tick(float deltaTime, float unscaledDeltaTime);

		/// <summary>
		/// Cancels every timer with the given <paramref name="id"/>. Note: id 0 is the default for
		/// <see cref="WaitForSeconds(float, Action, bool, int)"/> when the caller did not supply one — so
		/// <c>Cancel(0)</c> cancels every untracked timer. See
		/// <c>Characterization_TimerSchedulerTests.cs</c> for the pinned semantics.
		/// </summary>
		void Cancel(int id);

		/// <summary>Cancels a single timer by handle. Tolerates null handle.</summary>
		void Cancel(ITimerHandle handle);

		/// <summary>Cancels every pending timer, condition, and enqueued action.</summary>
		void Clear();
	}
}
