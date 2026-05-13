using System;

namespace RevCore
{
	internal sealed class CountdownTimer
	{
		private readonly Action<float> m_onComplete;

		public TimerHandle Handle { get; }
		public bool UnscaledTime { get; }

		public CountdownTimer(TimerHandle handle, bool unscaledTime, Action<float> onComplete)
		{
			Handle = handle;
			UnscaledTime = unscaledTime;
			m_onComplete = onComplete;
		}

		public bool Tick(float deltaTime)
		{
			if (!Handle.IsRunning)
				return true;

			Handle.SetElapsed(Handle.Elapsed + deltaTime);
			if (Handle.Elapsed < Handle.Duration)
				return false;

			float overtime = Handle.Elapsed - Handle.Duration;
			Handle.Complete();
			m_onComplete?.Invoke(overtime);
			return true;
		}
	}
}
