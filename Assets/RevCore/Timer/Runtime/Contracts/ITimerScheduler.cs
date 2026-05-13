using System;

namespace RevCore
{
	public interface ITimerScheduler
	{
		int ActiveCount { get; }
		ITimerHandle WaitForSeconds(float seconds, Action onComplete, bool unscaledTime = false, int id = 0);
		ITimerHandle WaitForSeconds(float seconds, Action<float> onComplete, bool unscaledTime = false, int id = 0);
		ITimerHandle WaitForCondition(ConditionalDelegate condition, Action onComplete, int id = 0);
		ITimerHandle Debounce<T>(T evt, float seconds) where T : IEvent;
		void Enqueue(Action action);
		void Tick(float deltaTime, float unscaledDeltaTime);
		void Cancel(int id);
		void Cancel(ITimerHandle handle);
		void Clear();
	}
}
