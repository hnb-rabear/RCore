using RCore.Inspector;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Inspector
{
	[CustomPropertyDrawer(typeof(FolderPathAttribute))]
	public class FolderPathPropertyDrawer : PropertyDrawer
	{
		private static string LastOpenedDirectory
		{
			get => EditorPrefs.GetString("LastOpenedDirectory");
			set => EditorPrefs.SetString("LastOpenedDirectory", value);
		}
		
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType == SerializedPropertyType.String)
			{
				EditorGUI.PrefixLabel(position, label);
				var btnRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);
				if (GUI.Button(btnRect, string.IsNullOrEmpty(property.stringValue) ? "Select Folder" : property.stringValue))
				{
					string folder = property.stringValue;
					if (string.IsNullOrEmpty(folder))
						folder = LastOpenedDirectory;
					if (string.IsNullOrEmpty(folder))
						folder = Application.dataPath;
					string path = EditorUtility.OpenFolderPanel("Select Folder", folder, "");
					if (!string.IsNullOrEmpty(path))
					{
						property.stringValue = path.Replace(Application.dataPath, "Assets");
						LastOpenedDirectory = property.stringValue;
					}
				}
			}
			else
				EditorGUI.LabelField(position, label.text, "Use [FolderPath] with strings.");
		}
	}
}