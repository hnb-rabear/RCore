using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// On a <see cref="ScriptableObject"/>-typed field, renders an inspector button that creates a new
	/// asset of the field type and assigns it. Implemented by an editor-side property drawer.
	/// </summary>
	public sealed class CreateScriptableObjectAttribute : PropertyAttribute
	{
		/// <summary>Creates the attribute. Has no parameters; behavior is entirely on the drawer side.</summary>
		public CreateScriptableObjectAttribute() { }
	}
}
