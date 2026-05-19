using System;

namespace RevCore
{
	/// <summary>
	/// Static facade over the process-global <see cref="ITimerScheduler"/>. Each member forwards to
	/// <see cref="Scheduler"/> — replace <see cref="Scheduler"/> at startup to plug in a custom
	/// driver. See <see cref="ITimerScheduler"/> for member semantics.
	/// </summary>
	public static partial class Timers
	{
		private static ITimerScheduler s_scheduler = new TimerScheduler();

		/// <summary>The active scheduler. Setting <c>null</c> reinstates a default <see cref="TimerScheduler"/>.</summary>
		public static ITimerScheduler Scheduler
		{
			get => s_scheduler;
			set => s_scheduler = value ?? new TimerScheduler();
		}

		/// <inheritdoc cref="ITimerScheduler.ActiveCount"/>
		public static int ActiveCount => s_scheduler.ActiveCount;
		/// <inheritdoc cref="ITimerScheduler.WaitForSeconds(float, Action, bool, int)"/>
		public static ITimerHandle WaitForSeconds(float seconds, Action onComplete, bool unscaledTime = false, int id = 0) => s_scheduler.WaitForSeconds(seconds, onComplete, unscaledTime, id);
		/// <inheritdoc cref="ITimerScheduler.WaitForSeconds(float, Action{float}, bool, int)"/>
		public static ITimerHandle WaitForSeconds(float seconds, Action<float> onComplete, bool unscaledTime = false, int id = 0) => s_scheduler.WaitForSeconds(seconds, onComplete, unscaledTime, id);
		/// <inheritdoc cref="ITimerScheduler.WaitForCondition"/>
		public static ITimerHandle WaitForCondition(ConditionalDelegate condition, Action onComplete, int id = 0) => s_scheduler.WaitForCondition(condition, onComplete, id);
		/// <inheritdoc cref="ITimerScheduler.Debounce"/>
		public static ITimerHandle Debounce<T>(T evt, float seconds) where T : IEvent => s_scheduler.Debounce(evt, seconds);
		/// <inheritdoc cref="ITimerScheduler.Enqueue"/>
		public static void Enqueue(Action action) => s_scheduler.Enqueue(action);
		/// <inheritdoc cref="ITimerScheduler.Tick"/>
		public static void Tick(float deltaTime, float unscaledDeltaTime) => s_scheduler.Tick(deltaTime, unscaledDeltaTime);
		/// <inheritdoc cref="ITimerScheduler.Cancel(int)"/>
		public static void Cancel(int id) => s_scheduler.Cancel(id);
		/// <inheritdoc cref="ITimerScheduler.Cancel(ITimerHandle)"/>
		public static void Cancel(ITimerHandle handle) => s_scheduler.Cancel(handle);
		/// <inheritdoc cref="ITimerScheduler.Clear"/>
		public static void Clear() => s_scheduler.Clear();
	}
}
