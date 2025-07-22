/**
 * Author HNB-RaBear - 2024
 **/

using UnityEngine;

namespace RCore.Data.JObject
{
	/// <summary>
	/// An interface that combines the data handling logic of `IJObjectHandler` with a standardized way
	/// to access the underlying serializable data object.
	/// This is typically implemented by a higher-level "Model" class (like a ScriptableObject) that manages the data.
	/// </summary>
	public interface IJObjectModel : IJObjectHandler
	{
		/// <summary>
		/// Gets the raw, serializable data object associated with this model.
		/// </summary>
		public JObjectData Data { get; }
	}

	/// <summary>
	/// An abstract ScriptableObject base class that serves as the "Model" in a data management architecture.
	/// It encapsulates a specific type of serializable data (`T`, which must be a `JObjectData`) and provides
	/// the Unity-centric logic and lifecycle methods to manipulate that data.
	/// </summary>
	/// <typeparam name="T">The type of the serializable data object this model will manage.</typeparam>
	public abstract class JObjectModel<T> : ScriptableObject, IJObjectModel where T : JObjectData
	{
		[Tooltip("The unique key used to identify and save the associated data object.")]
		public string key;
		[Tooltip("The actual data object that holds the serializable state.")]
		public T data;
		
		/// <summary>
		/// Provides access to the underlying data object through the IJObjectModel interface.
		/// </summary>
		public JObjectData Data => data;
		
		/// <summary>
		/// Called to initialize the model and its data. This is typically where you would create or load the data object.
		/// </summary>
		public abstract void Init();
		
		/// <summary>
		/// Called when the application is paused or resumed. Implement this to handle game state changes.
		/// </summary>
		public abstract void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds);
		
		/// <summary>
		/// Called after the data has been loaded. Implement this to calculate offline progress.
		/// </summary>
		public abstract void OnPostLoad(int utcNowTimestamp, int offlineSeconds);
		
		/// <summary>
		/// Called every frame. Implement this for time-based updates to the data.
		/// </summary>
		public abstract void OnUpdate(float deltaTime);
		
		/// <summary>
		/// Called just before the data is saved. Implement this to perform any final calculations.
		/// </summary>
		public abstract void OnPreSave(int utcNowTimestamp);
		
		/// <summary>
		/// Called when new remote configuration values have been fetched.
		/// </summary>
		public abstract void OnRemoteConfigFetched();
		
		/// <summary>
		/// Saves the current state of the associated `data` object to its persistent storage (e.g., PlayerPrefs).
		/// </summary>
		public void Save()
		{
			data.Save();
		}
		
		/// <summary>
		/// Replaces the current data object with a new instance, while preserving the original key.
		/// This is useful for importing or restoring data.
		/// </summary>
		/// <param name="pData">The new data object to use.</param>
		public void Import(T pData)
		{
			if (data != null)
				pData.key = data.key;
			data = pData;
		}
		
		/// <summary>
		/// A convenience helper to dispatch an event, typically when a significant data change occurs.
		/// This allows other parts of the application to react to the change.
		/// </summary>
		/// <param name="e">The event instance to be raised.</param>
		/// <param name="pDeBounce">An optional delay in seconds. If provided, multiple calls within this duration will only result in a single event being dispatched after the delay, preventing event spam.</param>
		/// <typeparam name="TEvent">The type of the event, which must inherit from `BaseEvent`.</typeparam>
		protected void DispatchEvent<TEvent>(TEvent e, float pDeBounce = 0) where TEvent : BaseEvent
		{
			if (pDeBounce > 0)
			{
				// Use a debouncing mechanism to prevent rapid-fire events.
				// A CountdownEvent with a stable ID based on the event type is used.
				TimerEventsGlobal.Instance.WaitForSeconds(new CountdownEvent
				{
					id = RUtil.GetStableHashCode(typeof(TEvent).Name),
					waitTime = pDeBounce,
					onTimeOut = f => EventDispatcher.Raise(e),
					unscaledTime = true,
				});
			}
			else
			{
				EventDispatcher.Raise(e);
			}
		}
	}
}