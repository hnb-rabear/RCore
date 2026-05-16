using System;
using System.Collections.Generic;

namespace RevCore
{
	public sealed class EventBus : IEventBus
	{
		private readonly Dictionary<Type, Delegate> m_listeners = new();

		public int ListenerCount
		{
			get
			{
				int count = 0;
				foreach (var del in m_listeners.Values)
					if (del != null)
						count += del.GetInvocationList().Length;
				return count;
			}
		}

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
		}

		public void Unsubscribe<T>(Action<T> listener) where T : IEvent
		{
			var key = typeof(T);
			if (m_listeners.TryGetValue(key, out var existing))
			{
				var updated = (Action<T>)existing - listener;
				if (updated == null)
					m_listeners.Remove(key);
				else
					m_listeners[key] = updated;
			}
		}

		public void Publish<T>(T evt) where T : IEvent
		{
			if (m_listeners.TryGetValue(typeof(T), out var del))
				((Action<T>)del).Invoke(evt);
		}

		public void Clear()
		{
			m_listeners.Clear();
		}

		public void Clear<T>() where T : IEvent
		{
			m_listeners.Remove(typeof(T));
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
