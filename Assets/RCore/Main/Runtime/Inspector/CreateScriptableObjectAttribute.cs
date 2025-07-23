using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.IO;

namespace RCore.Inspector
{
	/// <summary>
	/// An attribute that, when applied to a ScriptableObject field in the Inspector,
	/// displays a "Create" button next to the field. This allows for the quick creation
	/// and assignment of new ScriptableObject assets directly from the Inspector.
	/// </summary>
	public class CreateScriptableObjectAttribute : PropertyAttribute
	{
		public CreateScriptableObjectAttribute() { }
	}

#if UNITY_EDITOR
	/// <summary>
	/// The custom property drawer for fields marked with the [CreateScriptableObject] attribute.
	/// This class contains the editor logic for drawing the "Create" button and handling the asset creation process.
	/// </summary>
	[CustomPropertyDrawer(typeof(CreateScriptableObjectAttribute))]
	public class CreateScriptableObjectDrawer : PropertyDrawer
	{
		/// <summary>
		/// The main GUI drawing method for the property. It draws the object field and a conditional "Create" button.
		/// </summary>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			EditorGUILayout.BeginHorizontal();

			// Draw the property field for the ScriptableObject reference.
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width - 70, position.height), property, label, true);

			// Enable the "Create" button only if the property field is currently null (empty).
			GUI.enabled = property.objectReferenceValue == null;

			// Draw the "Create" button.
			if (GUI.Button(new Rect(position.x + position.width - 65, position.y, 60, position.height), "Create"))
			{
				Type objectType = fieldInfo.FieldType;

				// Create an in-memory instance of the ScriptableObject.
				ScriptableObject newObject = ScriptableObject.CreateInstance(objectType);
				if (newObject != null)
				{
					// Determine a default save path. Try to save next to the asset being inspected.
					string assetPath = AssetDatabase.GetAssetPath(property.serializedObject.targetObject);
					string directoryPath = !string.IsNullOrEmpty(assetPath) ? Path.GetDirectoryName(assetPath) : null;

					// If no path can be determined (e.g., inspecting a scene object), open a folder panel.
					if (string.IsNullOrEmpty(directoryPath))
					{
						directoryPath = EditorUtility.OpenFolderPanel("Select Folder to Save New Asset", "Assets", "");
						if (string.IsNullOrEmpty(directoryPath))
						{
							Debug.LogWarning("No folder selected. Creation canceled.");
							return; // Exit if the user cancels the folder selection.
						}

						// Convert the absolute path back to a relative "Assets/..." path.
						directoryPath = "Assets" + directoryPath.Substring(Application.dataPath.Length);
					}

					// Create the new asset on disk with a unique name.
					string newAssetPath = AssetDatabase.GenerateUniqueAssetPath(directoryPath + "/New" + objectType.Name + ".asset");
					AssetDatabase.CreateAsset(newObject, newAssetPath);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();

					// Assign the newly created asset to the property field.
					property.objectReferenceValue = newObject;
					property.serializedObject.ApplyModifiedProperties();

					Debug.Log("New " + objectType.Name + " asset created and assigned to the field at " + newAssetPath);
				}
				else
				{
					Debug.LogError("Failed to create a new instance of " + objectType.Name);
				}
			}

			// Restore the GUI's enabled state.
			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();
			EditorGUI.EndProperty();
		}
		
		/// <summary>
		/// Ensures the property drawer has the correct height, especially for multi-line properties.
		/// </summary>
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}
	}
#endif
}