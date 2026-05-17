using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// <see cref="MonoBehaviour"/> that drives an <see cref="ITimerScheduler"/> each frame from
	/// <see cref="LateUpdate"/>. When <c>m_useGlobalScheduler</c> is set, drives <see cref="Timers.Scheduler"/>;
	/// otherwise creates its own scheduler instance for scene-scoped timers.
	/// </summary>
	/// <remarks>
	/// The component disables itself when no timers are pending and re-enables itself on the next
	/// <see cref="WaitForSeconds"/> / <see cref="WaitForCondition"/> call — small CPU win for scenes
	/// with idle drivers.
	/// </remarks>
	public class TimerDriver : MonoBehaviour
	{
		[SerializeField] private bool m_useGlobalScheduler = true;

		/// <summary>The scheduler being driven. Resolved on <see cref="Awake"/>.</summary>
		public ITimerScheduler Scheduler { get; private set; }

		/// <summary>Resolves <see cref="Scheduler"/>. Override to substitute a custom scheduler.</summary>
		protected virtual void Awake()
		{
			Scheduler = m_useGlobalScheduler ? Timers.Scheduler : new TimerScheduler();
		}

		/// <summary>Ticks the scheduler. Disables the component when nothing is pending.</summary>
		protected virtual void LateUpdate()
		{
			Scheduler.Tick(Time.deltaTime, Time.unscaledDeltaTime);
			enabled = Scheduler.ActiveCount > 0;
		}

		/// <summary>Schedules <paramref name="onComplete"/> after <paramref name="seconds"/> and re-enables the driver.</summary>
		public ITimerHandle WaitForSeconds(float seconds, System.Action onComplete, bool unscaledTime = false, int id = 0)
		{
			enabled = true;
			return Scheduler.WaitForSeconds(seconds, onComplete, unscaledTime, id);
		}

		/// <summary>Schedules <paramref name="onComplete"/> for the first tick at which <paramref name="condition"/> returns true.</summary>
		public ITimerHandle WaitForCondition(ConditionalDelegate condition, System.Action onComplete, int id = 0)
		{
			enabled = true;
			return Scheduler.WaitForCondition(condition, onComplete, id);
		}
	}
}
