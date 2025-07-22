/**
 * Author HNB-RaBear - 2024
 **/

namespace RCore.Data.JObject
{
	/// <summary>
	/// An abstract base class that provides a structured way to handle the logic associated with a JObjectDataCollection.
	/// This class acts as a "Controller" or "Handler" for the data, responding to application lifecycle events.
	/// It is designed to be inherited by specific handler classes that implement the actual game logic.
	/// </summary>
	/// <typeparam name="T">The specific type of JObjectDataCollection this handler will manage.</typeparam>
	[System.Serializable]
	public abstract class JObjectHandler<T> : IJObjectHandler where T : JObjectDataCollection
	{
		/// <summary>
		/// A reference to the main data collection. This provides the handler with access to the entire
		/// data ecosystem, including all other data models and their states.
		/// </summary>
		public T dataCollection;

		/// <summary>
		/// Called when the application is paused or resumed. Inheriting classes must implement this
		/// to define logic for handling these state changes (e.g., stopping timers).
		/// </summary>
		public abstract void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds);

		/// <summary>
		/// Called after all data has been loaded. Inheriting classes must implement this to define logic
		/// that runs once at the start of a session, such as calculating offline progress.
		/// </summary>
		public abstract void OnPostLoad(int utcNowTimestamp, int offlineSeconds);

		/// <summary>
		/// Called every frame. Inheriting classes must implement this to define any continuous,
		/// time-based logic (e.g., regenerating energy over time).
		/// </summary>
		public abstract void OnUpdate(float deltaTime);

		/// <summary>
		/// Called immediately before the data is saved. Inheriting classes must implement this to perform
		/// any final calculations or state updates before the data is written to storage.
		/// </summary>
		public abstract void OnPreSave(int utcNowTimestamp);

		/// <summary>
		/// A virtual method called when new remote configuration values are fetched.
		/// Child classes can override this to react to remote config changes, but it is not required.
		/// </summary>
		public virtual void OnRemoteConfigFetched() { }

		/// <summary>
		/// Throws a NotImplementedException. This method is not intended to be called on a handler.
		/// The saving process is managed centrally by the `JObjectDataCollection`, which calls `OnPreSave` on handlers
		/// before persisting the data.
		/// </summary>
		public void Save()
		{
			throw new System.NotImplementedException("JObjectHandlers are not responsible for saving. The JObjectDataCollection orchestrates the save process after calling OnPreSave on all handlers.");
		}

		/// <summary>
		/// A convenience helper to dispatch an event, typically when a significant data change occurs.
		/// This allows other parts of the application (like the UI) to react to the change without being tightly coupled.
		/// </summary>
		/// <typeparam name="TEvent">The type of the event, which must inherit from `BaseEvent`.</typeparam>
		/// <param name="e">The event instance to be raised.</param>
		/// <param name="pDeBounce">An optional delay in seconds. If provided, multiple calls within this duration will only result in a single event being dispatched after the delay, preventing event spam.</param>
		protected void DispatchEvent<TEvent>(TEvent e, float pDeBounce = 0) where TEvent : BaseEvent
		{
			if (pDeBounce > 0)
			{
				// Use a debouncing mechanism to prevent rapid-fire events.
				// A CountdownEvent with a stable ID based on the event type is used for this.
				TimerEventsGlobal.Instance.WaitForSeconds(new CountdownEvent()
				{
					id = RUtil.GetStableHashCode(typeof(TEvent).Name),
					waitTime = pDeBounce,
					onTimeOut = f => EventDispatcher.Raise(e),
					unscaledTime = true,
				});
			}
			else
			{
				// Dispatch the event immediately.
				EventDispatcher.Raise(e);
			}
		}
	}
}