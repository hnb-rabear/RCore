/***
 * Author Gemini - 2025
 **/

namespace RCore.ModulePattern
{
	/// <summary>
	/// Defines the contract that all modules must implement to be managed by the ModuleManager.
	/// This interface enforces a standard lifecycle for initialization, per-frame updates, and shutdown.
	/// </summary>
	public interface IModule
	{
		/// <summary>
		/// Gets a unique identifier for the module instance.
		/// It is recommended to return the key defined in the module's [Module] attribute.
		/// </summary>
		string ModuleID { get; }
		
		/// <summary>
		/// Called by the ModuleManager when the module is first created and registered.
		/// Use this method for setup, resource loading, event subscription, and other one-time initialization tasks.
		/// </summary>
		void Initialize();
		
		/// <summary>
		/// Called by the ModuleManager every frame.
		/// Use this method for any logic that needs to run continuously, similar to a MonoBehaviour's Update() method.
		/// </summary>
		void Tick();
		
		/// <summary>
		/// Called by the ModuleManager when the module is being removed or when the application is closing.
		/// Use this method for cleanup, saving data, event unsubscription, and releasing resources.
		/// </summary>
		void Shutdown();
	}
}