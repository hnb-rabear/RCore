/**
 * Author HNB-RaBear - 2024
 **/

namespace RCore.Data.JObject
{
	/// <summary>
	/// Defines a contract for data objects that need to react to application lifecycle events.
	/// This interface provides a standardized set of callbacks for handling pausing, loading, saving,
	/// and other critical moments, allowing for consistent data management logic.
	/// </summary>
	public interface IJObjectHandler
	{
		/// <summary>
		/// Called when the application is paused or resumed.
		/// </summary>
		/// <param name="pause">True if the application is being paused, false if it is being resumed.</param>
		/// <param name="utcNowTimestamp">The current UTC time as a Unix timestamp.</param>
		/// <param name="offlineSeconds">The duration in seconds that the application was paused (if resuming).</param>
		public void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds);
		
		/// <summary>
		/// Called after the object's data has been successfully loaded from storage.
		/// This is the ideal place to calculate any progress made while the user was offline (e.g., idle resource generation).
		/// </summary>
		/// <param name="utcNowTimestamp">The current UTC time as a Unix timestamp.</param>
		/// <param name="offlineSeconds">The duration in seconds since the last session ended.</param>
		public void OnPostLoad(int utcNowTimestamp, int offlineSeconds);
		
		/// <summary>
		/// Called every frame to allow for continuous updates and time-based logic.
		/// </summary>
		/// <param name="deltaTime">The time in seconds that has passed since the last frame.</param>
		public void OnUpdate(float deltaTime);
		
		/// <summary>
		/// A hook that is called immediately before the object's data is saved.
		/// This allows for final calculations or state updates to be performed before serialization.
		/// </summary>
		/// <param name="utcNowTimestamp">The current UTC time as a Unix timestamp.</param>
		public void OnPreSave(int utcNowTimestamp);
		
		/// <summary>
		/// A callback method that is invoked when new remote configuration values have been fetched.
		/// Implementing classes must provide logic to handle and apply these new settings.
		/// </summary>
		public abstract void OnRemoteConfigFetched();
		
		/// <summary>
		/// Triggers the save mechanism for the object, persisting its current state.
		/// </summary>
		public void Save();
	}
}