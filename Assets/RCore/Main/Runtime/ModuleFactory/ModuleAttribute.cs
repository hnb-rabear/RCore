/***
 * Author Gemini - 2025
 **/

namespace RCore.ModulePattern
{
	[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class ModuleAttribute : System.Attribute
	{
		public string Key { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the ModuleFactory should automatically
		/// create an instance of this module during its auto-registration phase.
		/// Defaults to true.
		/// </summary>
		public bool AutoCreate { get; set; } = true;

		/// <summary>
		/// Gets or sets the loading order for this module. Modules with lower numbers are
		/// initialized first by the ModuleManager during auto-registration.
		/// Defaults to 0.
		/// </summary>
		public int LoadOrder { get; set; } = 0;

		/// <summary>
		/// Marks a class as a module that can be created by the ModuleFactory.
		/// </summary>
		/// <param name="key">A unique key to identify this module type.</param>
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