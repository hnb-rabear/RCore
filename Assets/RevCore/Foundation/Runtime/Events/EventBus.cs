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
		private readonly Dictionary<Type, Delegate> m_listeners = new();
		private int m_totalListenerCount;

		/// <inheritdoc />
		public int ListenerCount => m_totalListenerCount;

		/// <inheritdoc />
		public void Subscribe<T>(Action<T> listener) where T : IEvent
		{
			var key = typeof(T);
			if (m_listeners.TryGetValue(key, out var existing))
			{
				var typed = (Action<T>)existing;
				if (Array.IndexOf(typed.GetInvocationList(), listener) >= 0)
					return;
				m_listeners[key] = typed + listener;
			}
			else
			{
				m_listeners[key] = listener;
			}
			m_totalListenerCount++;
		}

		/// <inheritdoc />
		public void Unsubscribe<T>(Action<T> listener) where T : IEvent
		{
			var key = typeof(T);
			if (!m_listeners.TryGetValue(key, out var existing))
				return;
			var typed = (Action<T>)existing;
			var updated = typed - listener;
			if (ReferenceEquals(updated, typed))
				return; // listener wasn't in the invocation list; no-op
			if (updated == null)
				m_listeners.Remove(key);
			else
				m_listeners[key] = updated;
			m_totalListenerCount--;
		}

		/// <inheritdoc />
		public void Publish<T>(T evt) where T : IEvent
		{
			if (m_listeners.TryGetValue(typeof(T), out var del))
				((Action<T>)del).Invoke(evt);
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
			if (m_listeners.TryGetValue(typeof(T), out var existing))
			{
				m_totalListenerCount -= existing.GetInvocationList().Length;
				m_listeners.Remove(typeof(T));
			}
		}

		/// <summary>
		/// Returns the number of subscribers for event type <typeparamref name="T"/>. O(1) plus
		/// the cost of <see cref="Delegate.GetInvocationList"/> when there is at least one
		/// listener. Prefer this over <see cref="ListenerCount"/> on hot paths that only care
		/// about one event type.
		/// </summary>
		public int ListenerCountFor<T>() where T : IEvent
		{
			return m_listeners.TryGetValue(typeof(T), out var del) && del != null
				? del.GetInvocationList().Length
				: 0;
		}
	}
}
