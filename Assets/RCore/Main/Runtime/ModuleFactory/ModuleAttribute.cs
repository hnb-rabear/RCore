/***
 * Author Gemini - 2025
 **/

namespace RCore.ModulePattern
{
	/// <summary>
	/// An attribute used to mark a class as a discoverable module.
	/// This allows the ModuleFactory to identify and manage the class through reflection.
	/// Applying this attribute to a class that implements IModule makes it eligible for
	/// automatic or manual creation and registration with the ModuleManager.
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class ModuleAttribute : System.Attribute
	{
		/// <summary>
		/// Gets the unique string identifier for this module. This key is used by the
		/// ModuleFactory and ModuleManager to create and retrieve the module.
		/// </summary>
		public string Key { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the ModuleFactory should automatically
		/// create an instance of this module during its auto-registration phase.
		/// Note: This is ignored for modules that inherit from MonoBehaviour.
		/// Defaults to true.
		/// </summary>
		public bool AutoCreate { get; set; } = true;

		/// <summary>
		/// Gets or sets the loading order for this module. Modules with lower numbers are
		/// initialized first by the ModuleManager during the auto-creation process.
		/// This is useful for managing dependencies between modules.
		/// Defaults to 0.
		/// </summary>
		public int LoadOrder { get; set; } = 0;

		/// <summary>
		/// Marks a class as a module that can be created and managed by the module system.
		/// </summary>
		/// <param name="key">A unique key to identify this module type. It is recommended to use `nameof(ClassName)` to ensure uniqueness and prevent typos.</param>
		public ModuleAttribute(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new System.ArgumentNullException(nameof(key), "Module key cannot be null or empty.");
			}
			Key = key;
		}
	}
}