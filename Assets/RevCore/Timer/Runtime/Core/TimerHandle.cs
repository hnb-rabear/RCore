using System;

namespace RevCore
{
	internal sealed class TimerHandle : ITimerHandle
	{
		private readonly Action<TimerHandle> m_cancel;

		public int Id { get; }
		public bool IsRunning => !IsCompleted && !IsCancelled;
		public bool IsCompleted { get; private set; }
		public bool IsCancelled { get; private set; }
		public float Elapsed { get; private set; }
		public float Duration { get; }
		public float Remaining => Duration - Elapsed > 0f ? Duration - Elapsed : 0f;

		public TimerHandle(int id, float duration, Action<TimerHandle> cancel)
		{
			Id = id;
			Duration = duration;
			m_cancel = cancel;
		}

		public void SetElapsed(float elapsed)
		{
			Elapsed = elapsed;
		}

		public void Complete()
		{
			if (IsCancelled)
				return;

			Elapsed = Duration;
			IsCompleted = true;
		}

		public void Cancel()
		{
			if (IsCompleted || IsCancelled)
				return;

			IsCancelled = true;
			m_cancel?.Invoke(this);
		}
	}
}
