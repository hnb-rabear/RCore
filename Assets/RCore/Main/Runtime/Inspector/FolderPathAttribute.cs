#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace RCore.Inspector
{
	/// <summary>
	/// An attribute for string fields that provides a button in the Inspector to open a folder selection dialog.
	/// The selected folder path is then stored in the string field, relative to the project's "Assets" directory.
	/// </summary>
	public class FolderPathAttribute : PropertyAttribute { }

#if UNITY_EDITOR
	/// <summary>
	/// The custom property drawer for string fields marked with the [FolderPath] attribute.
	/// It replaces the standard text field with a button that shows the current path.
	/// </summary>
	[CustomPropertyDrawer(typeof(FolderPathAttribute))]
	public class FolderPathPropertyDrawer : PropertyDrawer
	{
		/// <summary>
		/// Stores the last used directory path in EditorPrefs to provide a better user experience.
		/// </summary>
		private static string LastOpenedDirectory { get => EditorPrefs.GetString("RCore.FolderPath.LastOpened"); set => EditorPrefs.SetString("RCore.FolderPath.LastOpened", value); }

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// This attribute only works on string properties.
			if (property.propertyType == SerializedPropertyType.String)
			{
				// Draw the property label.
				EditorGUI.PrefixLabel(position, label);

				// Define the rectangle for the button.
				var btnRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);
				
				// Determine the button's text: show the current path or a default message.
				string buttonText = string.IsNullOrEmpty(property.stringValue) ? "Select Folder" : property.stringValue;
				
				if (GUI.Button(btnRect, buttonText))
				{
					// Set the starting directory for the folder panel.
					string startFolder = property.stringValue;
					if (string.IsNullOrEmpty(startFolder) || !System.IO.Directory.Exists(startFolder))
						startFolder = LastOpenedDirectory; // Use the last opened directory if the current one is invalid.
					if (string.IsNullOrEmpty(startFolder) || !System.IO.Directory.Exists(startFolder))
						startFolder = Application.dataPath; // Default to the Assets folder.

					// Open the folder selection dialog.
					string selectedPath = EditorUtility.OpenFolderPanel("Select Folder", startFolder, "");
					
					// If a path was selected, update the property.
					if (!string.IsNullOrEmpty(selectedPath))
					{
						// Convert the absolute path to a relative path starting with "Assets".
						property.stringValue = "Assets" + selectedPath.Substring(Application.dataPath.Length);
						LastOpenedDirectory = property.stringValue; // Save for next time.
					}
				}
			}
			else
			{
				// Show an error message if the attribute is used on a non-string field.
				EditorGUI.LabelField(position, label.text, "Use [FolderPath] with string fields only.");
			}
		}
	}
#endif
}