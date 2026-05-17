using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// On a <see cref="ScriptableObject"/>-typed field, expands the referenced asset's fields inline
	/// so they can be edited without opening the asset in the project window. Implemented by an
	/// editor-side property drawer.
	/// </summary>
	public sealed class ExposeScriptableObjectAttribute : PropertyAttribute
	{
		/// <summary>Creates the attribute. Behavior is entirely on the drawer side.</summary>
		public ExposeScriptableObjectAttribute() { }
	}
}
