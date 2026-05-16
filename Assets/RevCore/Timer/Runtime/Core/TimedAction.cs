using System;

namespace RevCore
{
	/// <summary>
	/// Self-contained countdown that you tick from your own <see cref="UnityEngine.MonoBehaviour.Update"/>.
	/// Useful for short-lived per-instance timers (cooldowns, skill timers) where adding a scheduler
	/// dependency would be overkill. Not driven by <see cref="ITimerScheduler"/> — caller must
	/// invoke <see cref="Update"/> each frame.
	/// </summary>
	public sealed class TimedAction
	{
		/// <summary>Invoked once when <see cref="Finish"/> runs (either by Update completing or explicit call).</summary>
		public Action OnFinished;

		/// <summary>Target duration set by <see cref="Start"/>. Zero when not running.</summary>
		public float TimeTarget { get; private set; }

		private bool m_active;
		private bool m_finished = true;
		private float m_elapsedTime;

		/// <summary>True when started and not yet finished.</summary>
		public bool IsRunning => m_active && !m_finished;

		/// <summary>Seconds remaining until <see cref="Finish"/>. Zero when not running or already finished.</summary>
		public float RemainTime => TimeTarget - m_elapsedTime > 0f ? TimeTarget - m_elapsedTime : 0f;

		/// <summary>Advances the timer by <paramref name="deltaTime"/>. Calls <see cref="Finish"/> when the target is reached.</summary>
		public void Update(float deltaTime)
		{
			if (!m_active)
				return;

			m_elapsedTime += deltaTime;
			if (m_elapsedTime >= TimeTarget)
				Finish();
		}

		/// <summary>
		/// Starts (or restarts) the countdown. Non-positive <paramref name="targetTime"/> finishes
		/// immediately without invoking <see cref="OnFinished"/>.
		/// </summary>
		public void Start(float targetTime)
		{
			if (targetTime <= 0f)
			{
				m_finished = true;
				m_active = false;
				TimeTarget = 0f;
				m_elapsedTime = 0f;
				return;
			}

			m_elapsedTime = 0f;
			TimeTarget = targetTime;
			m_finished = false;
			m_active = true;
		}

		/// <summary>Stops and marks the countdown finished, invoking <see cref="OnFinished"/>. Idempotent.</summary>
		public void Finish()
		{
			if (m_finished)
				return;

			m_elapsedTime = TimeTarget;
			m_active = false;
			m_finished = true;
			OnFinished?.Invoke();
		}

		/// <summary>Sets elapsed time directly — useful for save/load to restore mid-cooldown state.</summary>
		public void SetElapsedTime(float value)
		{
			m_elapsedTime = value;
		}

		/// <summary>Returns the current elapsed time.</summary>
		public float GetElapsedTime() => m_elapsedTime;

		/// <summary>Resets elapsed to zero and marks inactive without invoking <see cref="OnFinished"/>. Use for "cancel".</summary>
		public void Stop()
		{
			m_elapsedTime = 0f;
			m_finished = false;
			m_active = false;
		}
	}
}
