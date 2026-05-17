using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// On a serialized field, populates its value from the project automatically when the field is null
	/// and the inspector renders it. <see cref="Path"/> narrows the asset search to a folder.
	/// Implemented by an editor-side property drawer.
	/// </summary>
	public sealed class AutoFillAttribute : PropertyAttribute
	{
		/// <summary>Optional asset folder to scope the search to. Empty searches the whole project.</summary>
		public string Path { get; private set; }

		/// <summary>Creates the attribute, optionally restricting the asset search to <paramref name="path"/>.</summary>
		public AutoFillAttribute(string path = "")
		{
			Path = path;
		}
	}
}
