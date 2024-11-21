using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

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
		private static Dictionary<Delegate, EventDelegate> delegateLookup = new Dictionary<Delegate, EventDelegate>();

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
		
		private static Dictionary<Type, CancellationTokenSource> debounceTokens = new Dictionary<Type, CancellationTokenSource>();
		
		public static async void RaiseDeBounce<T>(T e, float pDeBounce = 0) where T : BaseEvent
		{
			var eventType = typeof(T);

			// If there's an existing debounce token for this event type, cancel it
			if (debounceTokens.TryGetValue(eventType, out var existingToken))
			{
				existingToken.Cancel();
				existingToken.Dispose();
			}

			// Create a new cancellation token source for this event type
			var cts = new CancellationTokenSource();
			debounceTokens[eventType] = cts;

			try
			{
				// Wait for the specified debounce period
				await UniTask.Delay(TimeSpan.FromSeconds(pDeBounce), cancellationToken: cts.Token);

				// Raise the event if not canceled
				Raise(e);
			}
			catch (OperationCanceledException)
			{
				// If the task was canceled, do nothing
			}
			finally
			{
				// Clean up the token source
				if (debounceTokens[eventType] == cts)
				{
					debounceTokens.Remove(eventType);
					cts.Dispose();
				}
			}
		}
	}

    public interface BaseEvent { }
}