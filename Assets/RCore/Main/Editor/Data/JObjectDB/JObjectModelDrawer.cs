using System.IO;
using RCore.Data.JObject;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Data.JObject
{
	[CustomPropertyDrawer(typeof(JObjectModel<>), true)]
	public class JObjectModelDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			// Calculate rects
			Rect propertyRect = new Rect(position.x, position.y, position.width - 70, position.height);
			Rect buttonRect = new Rect(position.x + position.width - 65, position.y, 60, position.height);

			// Draw the property field
			EditorGUI.PropertyField(propertyRect, property, label, true);

			// Check if the object is null
			bool isObjectNull = property.objectReferenceValue == null;
			GUI.enabled = isObjectNull;

			// Draw the Create button
			if (GUI.Button(buttonRect, "Create"))
			{
				System.Type objectType = null;

				if (fieldInfo.FieldType.IsGenericType)
				{
					objectType = fieldInfo.FieldType.GetGenericArguments()[0];
				}
				else
				{
					objectType = fieldInfo.FieldType;
				}

				// Create an instance of the type
				var newObjectData = ScriptableObject.CreateInstance(objectType);

				if (newObjectData != null)
				{
					// Get the path of the selected object
					string assetPath = AssetDatabase.GetAssetPath(property.serializedObject.targetObject);
					string directoryPath = !string.IsNullOrEmpty(assetPath) ? Path.GetDirectoryName(assetPath) : null;

					// If the directory path is null, prompt the user to select a folder
					if (string.IsNullOrEmpty(directoryPath))
					{
						directoryPath = EditorUtility.OpenFolderPanel("Select Folder to Save New Asset", "Assets", "");
						if (string.IsNullOrEmpty(directoryPath))
						{
							Debug.LogWarning("No folder selected. Creation canceled.");
							return;
						}

						directoryPath = "Assets" + directoryPath.Substring(Application.dataPath.Length);
					}

					string newAssetPath = AssetDatabase.GenerateUniqueAssetPath(directoryPath + "/New" + objectType.Name + ".asset");

					// Create the asset
					AssetDatabase.CreateAsset(newObjectData, newAssetPath);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();

					// Assign the created object to the field
					property.objectReferenceValue = newObjectData;
					property.serializedObject.ApplyModifiedProperties();

					Debug.Log("New " + objectType.Name + " asset created and assigned to the field at " + newAssetPath);
				}
				else
				{
					Debug.LogError("Failed to create a new instance of " + objectType.Name);
				}
			}

			// Re-enable GUI for other elements
			GUI.enabled = true;

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}
	}
}