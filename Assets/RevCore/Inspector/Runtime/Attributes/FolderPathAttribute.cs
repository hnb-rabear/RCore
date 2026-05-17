using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// On a <see cref="string"/> field, renders a folder-picker button that opens the system file
	/// dialog and stores the chosen folder path. Implemented by an editor-side property drawer.
	/// </summary>
	public sealed class FolderPathAttribute : PropertyAttribute { }
}
