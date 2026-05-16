using System;
using System.Collections.Generic;

namespace RevCore
{
	/// <summary>
	/// Default <see cref="ITimerScheduler"/> implementation. <see cref="Enqueue"/> is thread-safe;
	/// every other entry point runs on the main thread only. Timers with the same non-zero id replace
	/// each other on schedule (handy for "reset the timeout on each input").
	/// </summary>
	/// <remarks>
	/// Phase 4 will rework <see cref="Cancel(int)"/> from O(n) linear scan to O(1) dictionary lookup;
	/// the observable behavior (see <c>Characterization_TimerSchedulerTests.cs</c>) stays the same.
	/// </remarks>
	public sealed class TimerScheduler : ITimerScheduler
	{
		private readonly List<CountdownTimer> m_countdowns = new();
		private readonly List<ConditionTimer> m_conditions = new();
		private readonly Queue<Action> m_queue = new();
		private readonly object m_queueLock = new();

		/// <inheritdoc />
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

		/// <inheritdoc />
		public ITimerHandle WaitForSeconds(float seconds, Action onComplete, bool unscaledTime = false, int id = 0)
		{
			return WaitForSeconds(seconds, _ => onComplete?.Invoke(), unscaledTime, id);
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
		public ITimerHandle WaitForCondition(ConditionalDelegate condition, Action onComplete, int id = 0)
		{
			var handle = new TimerHandle(id, 0f, RemoveHandle);
			var timer = new ConditionTimer(handle, condition, onComplete);
			ReplaceConditionIfNeeded(timer);
			return handle;
		}

		/// <inheritdoc />
		public ITimerHandle Debounce<T>(T evt, float seconds) where T : IEvent
		{
			return WaitForSeconds(seconds, () => Events.Publish(evt), false, typeof(T).GetHashCode());
		}

		/// <inheritdoc />
		public void Enqueue(Action action)
		{
			if (action == null)
				return;

			lock (m_queueLock)
			{
				m_queue.Enqueue(action);
			}
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
		public void Cancel(int id)
		{
			for (int i = m_countdowns.Count - 1; i >= 0; i--)
				if (m_countdowns[i].Handle.Id == id)
					m_countdowns[i].Handle.Cancel();

			for (int i = m_conditions.Count - 1; i >= 0; i--)
				if (m_conditions[i].Handle.Id == id)
					m_conditions[i].Handle.Cancel();
		}

		/// <inheritdoc />
		public void Cancel(ITimerHandle handle)
		{
			handle?.Cancel();
		}

		/// <inheritdoc />
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
						// Replace the slot BEFORE cancelling the old handle. Cancel() fires the
						// RemoveHandle callback, which scans m_countdowns by handle reference. If
						// we cancelled first, that callback would RemoveAt(i) and the next line's
						// indexer assignment would throw ArgumentOutOfRangeException.
						var oldHandle = m_countdowns[i].Handle;
						m_countdowns[i] = timer;
						oldHandle.Cancel();
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
						// Same re-entrancy guard as ReplaceCountdownIfNeeded above.
						var oldHandle = m_conditions[i].Handle;
						m_conditions[i] = timer;
						oldHandle.Cancel();
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
