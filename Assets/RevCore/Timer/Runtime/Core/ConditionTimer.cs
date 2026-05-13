using System;

namespace RevCore
{
	internal sealed class ConditionTimer
	{
		private readonly ConditionalDelegate m_condition;
		private readonly Action m_onComplete;

		public TimerHandle Handle { get; }

		public ConditionTimer(TimerHandle handle, ConditionalDelegate condition, Action onComplete)
		{
			Handle = handle;
			m_condition = condition;
			m_onComplete = onComplete;
		}

		public bool Tick()
		{
			if (!Handle.IsRunning)
				return true;

			if (m_condition == null || !m_condition())
				return false;

			Handle.Complete();
			m_onComplete?.Invoke();
			return true;
		}
	}
}
