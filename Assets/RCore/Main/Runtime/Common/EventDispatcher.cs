using System.Collections.Generic;

namespace RCore
{
	/// <summary>
	/// Event Manager manages publishing raised events to subscribing/listening classes.
	///
	/// @example subscribe
	///     EventManager.AddListener<SomethingHappenedEvent>(OnSomethingHappened);
	///
	/// @example unsubscribe
	///     EventManager.RemoveListener<SomethingHappenedEvent>(OnSomethingHappened);
	///
	/// @example publish an event
	///     EventManager.Raise(new SomethingHappenedEvent());
	///
	/// This class is a minor variation on <http://www.willrmiller.com/?p=87>
	/// </summary>
	public static class EventDispatcher
	{
		public delegate void EventDelegate<T>(T e) where T : BaseEvent;
		private delegate void EventDelegate(BaseEvent e);

		/// <summary>
		/// The actual delegate, there is one delegate per unique event. Each
		/// delegate has multiple invocation list items.
		/// </summary>
		private static Dictionary<int, EventDelegate> delegates = new Dictionary<int, EventDelegate>();

		/// <summary>
		/// Lookups only, there is one delegate lookup per listener
		/// </summary>
		private static Dictionary<System.Delegate, EventDelegate> delegateLookup = new Dictionary<System.Delegate, EventDelegate>();

		/// <summary>
		/// Add the delegate.
		/// </summary>
		public static void AddListener<T>(EventDelegate<T> del) where T : BaseEvent
		{
			if (delegateLookup.ContainsKey(del))
				return;

			// Create a new non-generic delegate which calls our generic one.  This
			// is the delegate we actually invoke.
			EventDelegate internalDelegate = e => del((T)e);
			delegateLookup[del] = internalDelegate;

			int id = RUtil.GetStableHashCode(typeof(T).Name);
			if (delegates.TryGetValue(id, out EventDelegate tempDel))
			{
				delegates[id] = tempDel += internalDelegate;
			}
			else
			{
				delegates[id] = internalDelegate;
			}
		}

		/// <summary>
		/// Remove the delegate. Can be called multiple times on same delegate.
		/// </summary>
		public static void RemoveListener<T>(EventDelegate<T> del) where T : BaseEvent
		{
			if (delegateLookup.TryGetValue(del, out EventDelegate internalDelegate))
			{
				int id = RUtil.GetStableHashCode(typeof(T).Name);
				if (delegates.TryGetValue(id, out EventDelegate tempDel))
				{
					tempDel -= internalDelegate;
					if (tempDel == null)
					{
						delegates.Remove(id);
					}
					else
					{
						delegates[id] = tempDel;
					}
				}

				delegateLookup.Remove(del);
			}
		}

		/// <summary>
		/// The count of delegate lookups. The delegate lookups will increase by
		/// one for each unique AddListener. Useful for debugging and not much else.
		/// </summary>
		public static int DelegateLookupCount => delegateLookup.Count;

		/// <summary>
		/// Raise the event to all the listeners
		/// </summary>
		public static void Raise(BaseEvent e)
		{
			int id = RUtil.GetStableHashCode(e.GetType().Name);
#if UNITY_EDITOR
			Debug.Log("Raise event " + e.GetType().Name);
#endif
			if (delegates.TryGetValue(id, out EventDelegate del))
			{
				del.Invoke(e);
			}
		}
	}

    public interface BaseEvent { }
}