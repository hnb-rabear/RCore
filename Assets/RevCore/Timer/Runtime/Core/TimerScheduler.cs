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
	/// <see cref="Cancel(int)"/>, <see cref="WaitForSeconds(float,System.Action,bool,int)"/>, and the
	/// internal handle-cancellation callback are amortized O(1) via two parallel indices on top of
	/// the main list: a per-id map (<c>m_countdownsById</c>) and a per-handle index map
	/// (<c>m_countdownIndexByHandle</c>). The main list still drives <see cref="Tick"/> iteration
	/// in reverse-registration order. Cancelled timers are dropped from the indices immediately and
	/// reaped from the main list on the next <see cref="Tick"/> (lazy cleanup).
	/// </remarks>
	public sealed class TimerScheduler : ITimerScheduler
	{
		private readonly List<CountdownTimer> m_countdowns = new();
		private readonly Dictionary<int, List<CountdownTimer>> m_countdownsById = new();
		private readonly Dictionary<TimerHandle, int> m_countdownIndexByHandle = new();

		private readonly List<ConditionTimer> m_conditions = new();
		private readonly Dictionary<int, List<ConditionTimer>> m_conditionsById = new();
		private readonly Dictionary<TimerHandle, int> m_conditionIndexByHandle = new();

		private readonly Queue<Action> m_queue = new();
		private readonly object m_queueLock = new();

		/// <inheritdoc />
		public int ActiveCount
		{
			get
			{
				// The index maps track ALIVE handles only — cancelled timers are dropped from the
				// indices immediately and reaped from the main list on the next Tick. Counting via
				// the indices gives the live count without waiting for Tick.
				lock (m_queueLock)
				{
					return m_countdownIndexByHandle.Count + m_conditionIndexByHandle.Count + m_queue.Count;
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

			// CountdownTimer.Tick and ConditionTimer.Tick both return true when the handle is
			// no longer running (cancelled OR completed), so a single Tick call collapses both
			// the "expired naturally" and "cancelled lazily" cases. The remove helpers are
			// idempotent for handles already cleared from the indices.
			for (int i = m_countdowns.Count - 1; i >= 0; i--)
			{
				var timer = m_countdowns[i];
				float delta = timer.UnscaledTime ? unscaledDeltaTime : deltaTime;
				if (timer.Tick(delta))
					RemoveCountdownAtPreservingOrder(i);
			}

			for (int i = m_conditions.Count - 1; i >= 0; i--)
			{
				if (m_conditions[i].Tick())
					RemoveConditionAtPreservingOrder(i);
			}
		}

		/// <inheritdoc />
		public void Cancel(int id)
		{
			if (m_countdownsById.TryGetValue(id, out var clist) && clist.Count > 0)
			{
				// Cancel() triggers RemoveHandle which mutates clist; snapshot before iterating.
				var snap = clist.ToArray();
				for (int i = 0; i < snap.Length; i++)
					snap[i].Handle.Cancel();
			}
			if (m_conditionsById.TryGetValue(id, out var dlist) && dlist.Count > 0)
			{
				var snap = dlist.ToArray();
				for (int i = 0; i < snap.Length; i++)
					snap[i].Handle.Cancel();
			}
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
			m_countdownsById.Clear();
			m_countdownIndexByHandle.Clear();
			m_conditions.Clear();
			m_conditionsById.Clear();
			m_conditionIndexByHandle.Clear();
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
			int id = timer.Handle.Id;
			if (id != 0 && m_countdownsById.TryGetValue(id, out var list) && list.Count > 0)
			{
				// For non-zero ids the per-id list always holds exactly one entry — the previous
				// schedule that this new timer replaces. Overwrite the slot in place so the
				// indices stay valid, then cancel the old handle. RemoveHandle on the old handle
				// is a no-op now because we already removed it from m_countdownIndexByHandle.
				var old = list[0];
				var oldHandle = old.Handle;
				int idx = m_countdownIndexByHandle[oldHandle];

				m_countdowns[idx] = timer;
				m_countdownIndexByHandle.Remove(oldHandle);
				m_countdownIndexByHandle[timer.Handle] = idx;
				list[0] = timer;

				oldHandle.Cancel();
				return;
			}

			AddCountdown(timer);
		}

		private void ReplaceConditionIfNeeded(ConditionTimer timer)
		{
			int id = timer.Handle.Id;
			if (id != 0 && m_conditionsById.TryGetValue(id, out var list) && list.Count > 0)
			{
				// Same in-place replace as ReplaceCountdownIfNeeded above.
				var old = list[0];
				var oldHandle = old.Handle;
				int idx = m_conditionIndexByHandle[oldHandle];

				m_conditions[idx] = timer;
				m_conditionIndexByHandle.Remove(oldHandle);
				m_conditionIndexByHandle[timer.Handle] = idx;
				list[0] = timer;

				oldHandle.Cancel();
				return;
			}

			AddCondition(timer);
		}

		private void AddCountdown(CountdownTimer timer)
		{
			int idx = m_countdowns.Count;
			m_countdowns.Add(timer);
			m_countdownIndexByHandle[timer.Handle] = idx;
			int id = timer.Handle.Id;
			if (!m_countdownsById.TryGetValue(id, out var list))
				m_countdownsById[id] = list = new List<CountdownTimer>(1);
			list.Add(timer);
		}

		private void AddCondition(ConditionTimer timer)
		{
			int idx = m_conditions.Count;
			m_conditions.Add(timer);
			m_conditionIndexByHandle[timer.Handle] = idx;
			int id = timer.Handle.Id;
			if (!m_conditionsById.TryGetValue(id, out var list))
				m_conditionsById[id] = list = new List<ConditionTimer>(1);
			list.Add(timer);
		}

		private void RemoveHandle(TimerHandle handle)
		{
			// Drop the handle from the indices immediately; the main list keeps the timer until
			// the next Tick reaps it (lazy cleanup). Subsequent dict lookups treat the handle as
			// already gone, which preserves the contract for Cancel(id) and Replace*IfNeeded.
			if (m_countdownIndexByHandle.Remove(handle, out _))
			{
				if (m_countdownsById.TryGetValue(handle.Id, out var list))
				{
					for (int i = 0; i < list.Count; i++)
						if (ReferenceEquals(list[i].Handle, handle))
						{
							list.RemoveAt(i);
							break;
						}
					if (list.Count == 0)
						m_countdownsById.Remove(handle.Id);
				}
				return;
			}

			if (m_conditionIndexByHandle.Remove(handle, out _))
			{
				if (m_conditionsById.TryGetValue(handle.Id, out var list))
				{
					for (int i = 0; i < list.Count; i++)
						if (ReferenceEquals(list[i].Handle, handle))
						{
							list.RemoveAt(i);
							break;
						}
					if (list.Count == 0)
						m_conditionsById.Remove(handle.Id);
				}
			}
		}

		/// <summary>
		/// Removes <c>m_countdowns[idx]</c>, preserving order. Items after <c>idx</c> shift down
		/// by one; their entries in <c>m_countdownIndexByHandle</c> are decremented to match.
		/// Idempotent on handles that were already cleared from the indices (e.g. by an earlier
		/// <see cref="RemoveHandle"/> call during a lazy cancel) — the dict ops degrade to no-ops.
		/// </summary>
		private void RemoveCountdownAtPreservingOrder(int idx)
		{
			var removed = m_countdowns[idx];
			m_countdowns.RemoveAt(idx);

			m_countdownIndexByHandle.Remove(removed.Handle);
			int id = removed.Handle.Id;
			if (m_countdownsById.TryGetValue(id, out var list))
			{
				for (int i = 0; i < list.Count; i++)
					if (ReferenceEquals(list[i], removed))
					{
						list.RemoveAt(i);
						break;
					}
				if (list.Count == 0)
					m_countdownsById.Remove(id);
			}

			for (int j = idx; j < m_countdowns.Count; j++)
				m_countdownIndexByHandle[m_countdowns[j].Handle] = j;
		}

		private void RemoveConditionAtPreservingOrder(int idx)
		{
			var removed = m_conditions[idx];
			m_conditions.RemoveAt(idx);

			m_conditionIndexByHandle.Remove(removed.Handle);
			int id = removed.Handle.Id;
			if (m_conditionsById.TryGetValue(id, out var list))
			{
				for (int i = 0; i < list.Count; i++)
					if (ReferenceEquals(list[i], removed))
					{
						list.RemoveAt(i);
						break;
					}
				if (list.Count == 0)
					m_conditionsById.Remove(id);
			}

			for (int j = idx; j < m_conditions.Count; j++)
				m_conditionIndexByHandle[m_conditions[j].Handle] = j;
		}
	}
}
