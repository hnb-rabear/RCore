using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
	[CustomPropertyDrawer(typeof(FolderPathAttribute))]
	public sealed class FolderPathDrawer : PropertyDrawer
	{
		private static string LastOpenedDirectory
		{
			get => EditorPrefs.GetString("RevCore.FolderPath.LastOpened", "Assets");
			set => EditorPrefs.SetString("RevCore.FolderPath.LastOpened", value);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType != SerializedPropertyType.String)
			{
				EditorGUI.HelpBox(position, "FolderPath requires string field.", MessageType.Error);
				return;
			}

			EditorGUI.BeginProperty(position, label, property);
			var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
			EditorGUI.LabelField(labelRect, label);

			float btnWidth = position.width - EditorGUIUtility.labelWidth;
			var btnRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, btnWidth, position.height);
			string display = string.IsNullOrEmpty(property.stringValue) ? "Select Folder" : property.stringValue;

			if (GUI.Button(btnRect, display, EditorStyles.popup))
			{
				string selected = EditorUtility.OpenFolderPanel("Select Folder", LastOpenedDirectory, "");
				if (!string.IsNullOrEmpty(selected))
				{
					string dataPath = Application.dataPath;
					if (selected.StartsWith(dataPath))
						property.stringValue = "Assets" + selected.Substring(dataPath.Length);
					else
						property.stringValue = selected;

					LastOpenedDirectory = selected;
					property.serializedObject.ApplyModifiedProperties();
				}
			}

			EditorGUI.EndProperty();
		}
	}
}
