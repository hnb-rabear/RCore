using System;
using System.Collections.Generic;
using System.Threading;

namespace RCore
{
	/// <summary>
	/// Provides a static, global event management system. It allows for decoupled communication
	/// between different parts of the application using a publish-subscribe pattern.
	/// Classes can subscribe (listen) to specific event types and other classes can publish (raise)
	/// those events without either having direct references to the other.
	///
	/// @example subscribe
	///     EventDispatcher.AddListener<SomethingHappenedEvent>(OnSomethingHappened);
	///
	/// @example unsubscribe
	///     EventDispatcher.RemoveListener<SomethingHappenedEvent>(OnSomethingHappened);
	///
	/// @example publish an event
	///     EventDispatcher.Raise(new SomethingHappenedEvent());
	///
	/// </summary>
	public static class EventDispatcher
	{
		/// <summary>
		/// A generic delegate type for event listeners.
		/// </summary>
		/// <typeparam name="T">The type of event this delegate handles, must implement BaseEvent.</typeparam>
		/// <param name="e">The event object passed to the listener.</param>
		public delegate void EventDelegate<T>(T e) where T : BaseEvent;
		/// <summary>A non-generic delegate used internally to store all listeners in a single dictionary.</summary>
		private delegate void EventDelegate(BaseEvent e);

		/// <summary>
		/// The primary dictionary storing all event delegates.
		/// The key is a hash of the event type's name, and the value is a multicast delegate
		/// containing all listeners for that event type.
		/// </summary>
		private static Dictionary<int, EventDelegate> delegates = new Dictionary<int, EventDelegate>();

		/// <summary>
		/// A lookup dictionary to map a listener's generic delegate to its internal, non-generic counterpart.
		/// This is crucial for correctly removing listeners.
		/// </summary>
		private static Dictionary<Delegate, EventDelegate> delegateLookup = new Dictionary<Delegate, EventDelegate>();

		/// <summary>
		/// Subscribes a listener to a specific event type.
		/// </summary>
		/// <typeparam name="T">The type of event to listen for.</typeparam>
		/// <param name="del">The method (delegate) that will be called when the event is raised.</param>
		public static void AddListener<T>(EventDelegate<T> del) where T : BaseEvent
		{
			// Prevent double-subscribing the same delegate instance.
			if (delegateLookup.ContainsKey(del))
				return;

			// Create a new non-generic delegate which calls our generic one.  This
			// is the delegate we actually invoke.
			EventDelegate internalDelegate = e => del((T)e);
			delegateLookup[del] = internalDelegate;

			int id = RUtil.GetStableHashCode(typeof(T).Name);
			if (delegates.TryGetValue(id, out EventDelegate tempDel))
			{
				// If a delegate for this event type already exists, add the new listener to its invocation list.
				delegates[id] = tempDel += internalDelegate;
			}
			else
			{
				// Otherwise, create a new entry for this event type.
				delegates[id] = internalDelegate;
			}
		}

		/// <summary>
		/// Unsubscribes a listener from a specific event type. It is safe to call this
		/// even if the listener was not previously subscribed.
		/// </summary>
		/// <typeparam name="T">The type of event to unsubscribe from.</typeparam>
		/// <param name="del">The method (delegate) to remove.</param>
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
						// If there are no listeners left for this event type, remove the entry to save memory.
						delegates.Remove(id);
					}
					else
					{
						delegates[id] = tempDel;
					}
				}

				// Always remove from the lookup table.
				delegateLookup.Remove(del);
			}
		}

		/// <summary>
		/// Gets the total number of unique listeners currently subscribed to any event.
		/// Useful for debugging memory leaks or subscription issues.
		/// </summary>
		public static int DelegateLookupCount => delegateLookup.Count;

		/// <summary>
		/// Publishes an event, notifying all subscribed listeners.
		/// </summary>
		/// <param name="e">The event object to be raised.</param>
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
		
		/// <summary>A dictionary to store cancellation tokens for debounced events.</summary>
		private static Dictionary<Type, CancellationTokenSource> debounceTokens = new Dictionary<Type, CancellationTokenSource>();
		
		/// <summary>
		/// Raises an event after a specified delay, but cancels any previously scheduled
		/// event of the same type that hasn't fired yet. This is useful for preventing
		/// an event from firing too rapidly (e.g., from rapid button clicks).
		/// Requires UniTask.
		/// </summary>
		/// <typeparam name="T">The type of event to raise.</typeparam>
		/// <param name="e">The event object to raise.</param>
		/// <param name="pDeBounce">The delay in seconds before the event is raised.</param>
		public static async void RaiseDeBounce<T>(T e, float pDeBounce = 0) where T : BaseEvent
		{
			var eventType = typeof(T);

			// If there's a pending debounced event of this type, cancel it.
			if (debounceTokens.TryGetValue(eventType, out var existingToken))
			{
				existingToken.Cancel();
				existingToken.Dispose();
			}

			// Create and store a new cancellation token for this new debounce request.
			var cts = new CancellationTokenSource();
			debounceTokens[eventType] = cts;

			try
			{
				// Wait for the specified delay.
				await Cysharp.Threading.Tasks.UniTask.Delay(TimeSpan.FromSeconds(pDeBounce), cancellationToken: cts.Token);

				// If the delay completed without being canceled, raise the event.
				Raise(e);
			}
			catch (OperationCanceledException)
			{
				// This is expected if another RaiseDeBounce call for the same event type was made. Do nothing.
			}
			finally
			{
				// Clean up the token source if it's the one we created.
				// This check prevents a race condition where a new token is created before this one is disposed.
				if (debounceTokens.TryGetValue(eventType, out var currentToken) && currentToken == cts)
				{
					debounceTokens.Remove(eventType);
				}
				cts.Dispose();
			}
		}
	}

    /// <summary>
    /// A marker interface that all event structs/classes must implement
    /// to be used with the EventDispatcher.
    /// </summary>
    public interface BaseEvent { }
}