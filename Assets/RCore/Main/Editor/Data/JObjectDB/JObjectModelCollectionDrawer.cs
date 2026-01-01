using System.IO;
using RCore.Data.JObject;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Data.JObject
{
	/// <summary>
	/// Custom property drawer for JObjectModelCollection, allowing creation of new asset instances directly from the inspector.
	/// </summary>
	[CustomPropertyDrawer(typeof(JObjectModelCollection), true)]
	public class JObjectModelCollectionDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Start the horizontal group
			EditorGUI.BeginProperty(position, label, property);
			EditorGUILayout.BeginHorizontal();

			// Draw the property field
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width - 70, position.height), property, label, true);

			// Check if the object is null
			GUI.enabled = property.objectReferenceValue == null;

			// Draw the Create button
			if (GUI.Button(new Rect(position.x + position.width - 65, position.y, 60, position.height), "Create"))
			{
				// Get the type of the property
				var objectType = fieldInfo.FieldType;

				// Create an instance of the type
				var newObjectData = ScriptableObject.CreateInstance(objectType) as JObjectModelCollection;

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

			// End the horizontal group
			EditorGUILayout.EndHorizontal();
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}
	}
}