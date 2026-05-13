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
	}
}
