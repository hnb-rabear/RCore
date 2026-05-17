using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// On a <see cref="string"/> field, renders a dropdown listing the project's tags so the user
	/// picks rather than free-types. Implemented by an editor-side property drawer.
	/// </summary>
	public sealed class TagSelectorAttribute : PropertyAttribute
	{
		/// <summary>When <c>true</c>, falls back to Unity's built-in tag dropdown instead of the custom one.</summary>
		public bool UseDefaultTagFieldDrawer = false;
	}
}
