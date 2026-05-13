using System;
using System.Collections.Generic;

namespace RevCore
{
	public sealed class TimerScheduler : ITimerScheduler
	{
		private readonly List<CountdownTimer> m_countdowns = new();
		private readonly List<ConditionTimer> m_conditions = new();
		private readonly Queue<Action> m_queue = new();
		private readonly object m_queueLock = new();

		public int ActiveCount
		{
			get
			{
				lock (m_queueLock)
				{
					return m_countdowns.Count + m_conditions.Count + m_queue.Count;
				}
			}
		}

		public ITimerHandle WaitForSeconds(float seconds, Action onComplete, bool unscaledTime = false, int id = 0)
		{
			return WaitForSeconds(seconds, _ => onComplete?.Invoke(), unscaledTime, id);
		}

		public ITimerHandle WaitForSeconds(float seconds, Action<float> onComplete, bool unscaledTime = false, int id = 0)
		{
			var handle = new TimerHandle(id, seconds > 0f ? seconds : 0f, RemoveHandle);
			if (seconds <= 0f)
			{
				handle.Complete();
				onComplete?.Invoke(0f);
				return handle;
			}

			var timer = new CountdownTimer(handle, unscaledTime, onComplete);
			ReplaceCountdownIfNeeded(timer);
			return handle;
		}

		public ITimerHandle WaitForCondition(ConditionalDelegate condition, Action onComplete, int id = 0)
		{
			var handle = new TimerHandle(id, 0f, RemoveHandle);
			var timer = new ConditionTimer(handle, condition, onComplete);
			ReplaceConditionIfNeeded(timer);
			return handle;
		}

		public ITimerHandle Debounce<T>(T evt, float seconds) where T : IEvent
		{
			return WaitForSeconds(seconds, () => Events.Publish(evt), false, typeof(T).GetHashCode());
		}

		public void Enqueue(Action action)
		{
			if (action == null)
				return;

			lock (m_queueLock)
			{
				m_queue.Enqueue(action);
			}
		}

		public void Tick(float deltaTime, float unscaledDeltaTime)
		{
			FlushQueue();

			for (int i = m_countdowns.Count - 1; i >= 0; i--)
			{
				var timer = m_countdowns[i];
				float delta = timer.UnscaledTime ? unscaledDeltaTime : deltaTime;
				if (timer.Tick(delta))
					m_countdowns.RemoveAt(i);
			}

			for (int i = m_conditions.Count - 1; i >= 0; i--)
			{
				if (m_conditions[i].Tick())
					m_conditions.RemoveAt(i);
			}
		}

		public void Cancel(int id)
		{
			for (int i = m_countdowns.Count - 1; i >= 0; i--)
				if (m_countdowns[i].Handle.Id == id)
					m_countdowns[i].Handle.Cancel();

			for (int i = m_conditions.Count - 1; i >= 0; i--)
				if (m_conditions[i].Handle.Id == id)
					m_conditions[i].Handle.Cancel();
		}

		public void Cancel(ITimerHandle handle)
		{
			handle?.Cancel();
		}

		public void Clear()
		{
			m_countdowns.Clear();
			m_conditions.Clear();
			lock (m_queueLock)
			{
				m_queue.Clear();
			}
		}

		private void FlushQueue()
		{
			while (true)
			{
				Action action;
				lock (m_queueLock)
				{
					if (m_queue.Count == 0)
						return;

					action = m_queue.Dequeue();
				}

				action.Invoke();
			}
		}

		private void ReplaceCountdownIfNeeded(CountdownTimer timer)
		{
			if (timer.Handle.Id != 0)
			{
				for (int i = 0; i < m_countdowns.Count; i++)
				{
					if (m_countdowns[i].Handle.Id == timer.Handle.Id)
					{
						m_countdowns[i].Handle.Cancel();
						m_countdowns[i] = timer;
						return;
					}
				}
			}

			m_countdowns.Add(timer);
		}

		private void ReplaceConditionIfNeeded(ConditionTimer timer)
		{
			if (timer.Handle.Id != 0)
			{
				for (int i = 0; i < m_conditions.Count; i++)
				{
					if (m_conditions[i].Handle.Id == timer.Handle.Id)
					{
						m_conditions[i].Handle.Cancel();
						m_conditions[i] = timer;
						return;
					}
				}
			}

			m_conditions.Add(timer);
		}

		private void RemoveHandle(TimerHandle handle)
		{
			for (int i = m_countdowns.Count - 1; i >= 0; i--)
				if (m_countdowns[i].Handle == handle)
					m_countdowns.RemoveAt(i);

			for (int i = m_conditions.Count - 1; i >= 0; i--)
				if (m_conditions[i].Handle == handle)
					m_conditions.RemoveAt(i);
		}
	}
}
