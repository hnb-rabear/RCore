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
			int newCount;
			if (m_listeners.TryGetValue(key, out var existing))
			{
				var typed = (Action<T>)existing;
				if (Array.IndexOf(typed.GetInvocationList(), listener) >= 0)
					return;
				var combined = typed + listener;
				m_listeners[key] = combined;
				newCount = combined.GetInvocationList().Length;
			}
			else
			{
				m_listeners[key] = listener;
				newCount = 1;
			}
			m_totalListenerCount++;
			RevDiagnostics.Listener?.OnEventSubscribed(key, newCount);
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
			int newCount;
			if (updated == null)
			{
				m_listeners.Remove(key);
				newCount = 0;
			}
			else
			{
				m_listeners[key] = updated;
				newCount = updated.GetInvocationList().Length;
			}
			m_totalListenerCount--;
			RevDiagnostics.Listener?.OnEventUnsubscribed(key, newCount);
		}

		/// <inheritdoc />
		public void Publish<T>(T evt) where T : IEvent
		{
			int listenerCount = 0;
			if (m_listeners.TryGetValue(typeof(T), out var del))
			{
				var typed = (Action<T>)del;
				listenerCount = typed.GetInvocationList().Length;
				typed.Invoke(evt);
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
