using System;
using System.Collections.Generic;

namespace RevCore
{
	/// <summary>
	/// Default in-memory implementation of <see cref="IEventBus"/>. Stores subscribers per event type
	/// in a <see cref="Dictionary{TKey,TValue}"/> of multicast delegates. Main-thread only — synchronization
	/// is the caller's responsibility if used from multiple threads.
	/// </summary>
	public sealed class EventBus : IEventBus
	{
		// Per-type entry storing the multicast delegate plus a maintained listener count.
		// The count avoids `GetInvocationList()` on the Publish hot path — that call returns
		// a freshly-allocated `Delegate[]` every invocation, which is the single largest
		// per-Publish allocation in steady-state usage.
		private struct Entry
		{
			public Delegate Del;
			public int Count;
		}

		private readonly Dictionary<Type, Entry> m_listeners = new();
		private int m_totalListenerCount;

		/// <inheritdoc />
		public int ListenerCount => m_totalListenerCount;

		/// <inheritdoc />
		public void Subscribe<T>(Action<T> listener) where T : IEvent
		{
			var key = typeof(T);
			int newCount;
			if (m_listeners.TryGetValue(key, out var entry))
			{
				var typed = (Action<T>)entry.Del;
				// Dedup walk is unavoidable here, but Subscribe is not a hot path —
				// callers wire listeners at startup, not per-frame.
				if (Array.IndexOf(typed.GetInvocationList(), listener) >= 0)
					return;
				entry.Del = typed + listener;
				entry.Count++;
				newCount = entry.Count;
				m_listeners[key] = entry;
			}
			else
			{
				m_listeners[key] = new Entry { Del = listener, Count = 1 };
				newCount = 1;
			}
			m_totalListenerCount++;
			RevDiagnostics.Listener?.OnEventSubscribed(key, newCount);
		}

		/// <inheritdoc />
		public void Unsubscribe<T>(Action<T> listener) where T : IEvent
		{
			var key = typeof(T);
			if (!m_listeners.TryGetValue(key, out var entry))
				return;
			var typed = (Action<T>)entry.Del;
			var updated = typed - listener;
			if (ReferenceEquals(updated, typed))
				return; // listener wasn't in the invocation list; no-op
			int newCount;
			if (updated == null)
			{
				m_listeners.Remove(key);
				newCount = 0;
			}
			else
			{
				entry.Del = updated;
				entry.Count--;
				newCount = entry.Count;
				m_listeners[key] = entry;
			}
			m_totalListenerCount--;
			RevDiagnostics.Listener?.OnEventUnsubscribed(key, newCount);
		}

		/// <inheritdoc />
		public void Publish<T>(T evt) where T : IEvent
		{
			int listenerCount = 0;
			if (m_listeners.TryGetValue(typeof(T), out var entry))
			{
				listenerCount = entry.Count;
				((Action<T>)entry.Del).Invoke(evt);
			}
			RevDiagnostics.Listener?.OnEventPublished(typeof(T), listenerCount);
		}

		/// <inheritdoc />
		public void Clear()
		{
			m_listeners.Clear();
			m_totalListenerCount = 0;
		}

		/// <inheritdoc />
		public void Clear<T>() where T : IEvent
		{
			if (m_listeners.TryGetValue(typeof(T), out var entry))
			{
				m_totalListenerCount -= entry.Count;
				m_listeners.Remove(typeof(T));
			}
		}

		/// <summary>
		/// Returns the number of subscribers for event type <typeparamref name="T"/>. O(1) dictionary
		/// lookup plus a single integer read — no allocation. Prefer this over
		/// <see cref="ListenerCount"/> on hot paths that only care about one event type.
		/// </summary>
		public int ListenerCountFor<T>() where T : IEvent
		{
			return m_listeners.TryGetValue(typeof(T), out var entry)
				? entry.Count
				: 0;
		}
	}
}
