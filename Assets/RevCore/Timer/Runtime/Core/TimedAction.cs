using System;

namespace RevCore
{
	public sealed class TimedAction
	{
		public Action OnFinished;
		public float TimeTarget { get; private set; }

		private bool m_active;
		private bool m_finished = true;
		private float m_elapsedTime;

		public bool IsRunning => m_active && !m_finished;
		public float RemainTime => TimeTarget - m_elapsedTime > 0f ? TimeTarget - m_elapsedTime : 0f;

		public void Update(float deltaTime)
		{
			if (!m_active)
				return;

			m_elapsedTime += deltaTime;
			if (m_elapsedTime >= TimeTarget)
				Finish();
		}

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

		public void Finish()
		{
			if (m_finished)
				return;

			m_elapsedTime = TimeTarget;
			m_active = false;
			m_finished = true;
			OnFinished?.Invoke();
		}

		public void SetElapsedTime(float value)
		{
			m_elapsedTime = value;
		}

		public float GetElapsedTime() => m_elapsedTime;

		public void Stop()
		{
			m_elapsedTime = 0f;
			m_finished = false;
			m_active = false;
		}
	}
}
