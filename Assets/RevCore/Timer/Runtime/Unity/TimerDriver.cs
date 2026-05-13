using UnityEngine;

namespace RevCore
{
	public class TimerDriver : MonoBehaviour
	{
		[SerializeField] private bool m_useGlobalScheduler = true;

		public ITimerScheduler Scheduler { get; private set; }

		protected virtual void Awake()
		{
			Scheduler = m_useGlobalScheduler ? Timers.Scheduler : new TimerScheduler();
		}

		protected virtual void LateUpdate()
		{
			Scheduler.Tick(Time.deltaTime, Time.unscaledDeltaTime);
			enabled = Scheduler.ActiveCount > 0;
		}

		public ITimerHandle WaitForSeconds(float seconds, System.Action onComplete, bool unscaledTime = false, int id = 0)
		{
			enabled = true;
			return Scheduler.WaitForSeconds(seconds, onComplete, unscaledTime, id);
		}

		public ITimerHandle WaitForCondition(ConditionalDelegate condition, System.Action onComplete, int id = 0)
		{
			enabled = true;
			return Scheduler.WaitForCondition(condition, onComplete, id);
		}
	}
}
