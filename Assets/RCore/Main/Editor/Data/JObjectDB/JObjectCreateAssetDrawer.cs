using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Data.JObject
{
	/// <summary>
	/// Shared base for property drawers that render an object-reference field next to a "Create" button.
	/// When the reference is null, the button creates a ScriptableObject asset of the type returned by
	/// <see cref="GetAssetType"/>, saves it beside the owning asset (or in a folder chosen by the user),
	/// and assigns it back to the field. Subclasses only need to specify which type to instantiate.
	/// </summary>
	public abstract class JObjectCreateAssetDrawer : PropertyDrawer
	{
		/// <summary>
		/// Returns the concrete <see cref="ScriptableObject"/> type to instantiate for the drawn field.
		/// Implementations typically derive this from <see cref="PropertyDrawer.fieldInfo"/>.
		/// </summary>
		protected abstract Type GetAssetType();

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			var propertyRect = new Rect(position.x, position.y, position.width - 70, position.height);
			var buttonRect = new Rect(position.x + position.width - 65, position.y, 60, position.height);

			EditorGUI.PropertyField(propertyRect, property, label, true);

			// The button is only actionable while the reference is empty.
			using (new EditorGUI.DisabledScope(property.objectReferenceValue != null))
			{
				if (GUI.Button(buttonRect, "Create"))
					CreateAndAssign(property);
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}

		private void CreateAndAssign(SerializedProperty property)
		{
			var objectType = GetAssetType();
			var newObject = ScriptableObject.CreateInstance(objectType);
			if (newObject == null)
			{
				Debug.LogError("Failed to create a new instance of " + objectType.Name);
				return;
			}

			// Save next to the owning asset; fall back to a user-picked folder when the target is not an asset.
			string assetPath = AssetDatabase.GetAssetPath(property.serializedObject.targetObject);
			string directoryPath = !string.IsNullOrEmpty(assetPath) ? Path.GetDirectoryName(assetPath) : null;
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
	}
}
