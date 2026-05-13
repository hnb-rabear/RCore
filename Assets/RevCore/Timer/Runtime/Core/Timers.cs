using System;

namespace RevCore
{
	public static class Timers
	{
		private static ITimerScheduler s_scheduler = new TimerScheduler();

		public static ITimerScheduler Scheduler
		{
			get => s_scheduler;
			set => s_scheduler = value ?? new TimerScheduler();
		}

		public static int ActiveCount => s_scheduler.ActiveCount;
		public static ITimerHandle WaitForSeconds(float seconds, Action onComplete, bool unscaledTime = false, int id = 0) => s_scheduler.WaitForSeconds(seconds, onComplete, unscaledTime, id);
		public static ITimerHandle WaitForSeconds(float seconds, Action<float> onComplete, bool unscaledTime = false, int id = 0) => s_scheduler.WaitForSeconds(seconds, onComplete, unscaledTime, id);
		public static ITimerHandle WaitForCondition(ConditionalDelegate condition, Action onComplete, int id = 0) => s_scheduler.WaitForCondition(condition, onComplete, id);
		public static ITimerHandle Debounce<T>(T evt, float seconds) where T : IEvent => s_scheduler.Debounce(evt, seconds);
		public static void Enqueue(Action action) => s_scheduler.Enqueue(action);
		public static void Tick(float deltaTime, float unscaledDeltaTime) => s_scheduler.Tick(deltaTime, unscaledDeltaTime);
		public static void Cancel(int id) => s_scheduler.Cancel(id);
		public static void Cancel(ITimerHandle handle) => s_scheduler.Cancel(handle);
		public static void Clear() => s_scheduler.Clear();
	}
}
