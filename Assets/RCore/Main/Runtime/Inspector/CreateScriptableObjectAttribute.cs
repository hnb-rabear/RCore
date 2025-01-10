using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;

namespace RCore.Inspector
{
	public class CreateScriptableObjectAttribute : PropertyAttribute
	{
		public CreateScriptableObjectAttribute() { }
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(CreateScriptableObjectAttribute))]
	public class CreateScriptableObjectDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			EditorGUILayout.BeginHorizontal();

			// Draw the property field
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width - 70, position.height), property, label, true);

			// Check if the object is null
			GUI.enabled = property.objectReferenceValue == null;

			// Draw the Create button
			if (GUI.Button(new Rect(position.x + position.width - 65, position.y, 60, position.height), "Create"))
			{
				Type objectType = fieldInfo.FieldType;

				ScriptableObject newObject = ScriptableObject.CreateInstance(objectType);
				if (newObject != null)
				{
					string assetPath = AssetDatabase.GetAssetPath(property.serializedObject.targetObject);
					string directoryPath = !string.IsNullOrEmpty(assetPath) ? System.IO.Path.GetDirectoryName(assetPath) : null;

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

					AssetDatabase.CreateAsset(newObject, newAssetPath);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();

					property.objectReferenceValue = newObject;
					property.serializedObject.ApplyModifiedProperties();

					Debug.Log("New " + objectType.Name + " asset created and assigned to the field at " + newAssetPath);
				}
				else
				{
					Debug.LogError("Failed to create a new instance of " + objectType.Name);
				}
			}

			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}
	}
#endif
}