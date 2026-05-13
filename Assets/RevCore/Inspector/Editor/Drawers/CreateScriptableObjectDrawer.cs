using System.IO;
using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
	[CustomPropertyDrawer(typeof(CreateScriptableObjectAttribute))]
	public sealed class CreateScriptableObjectDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			=> EditorGUI.GetPropertyHeight(property, label, true);

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var fieldRect = new Rect(position.x, position.y, position.width - 66f, position.height);
			var buttonRect = new Rect(position.x + position.width - 60f, position.y, 60f, EditorGUIUtility.singleLineHeight);

			EditorGUI.PropertyField(fieldRect, property, label, true);

			bool enabled = GUI.enabled;
			GUI.enabled = enabled && property.objectReferenceValue == null;
			if (GUI.Button(buttonRect, "Create"))
				CreateAsset(property);
			GUI.enabled = enabled;
		}

		private void CreateAsset(SerializedProperty property)
		{
			if (!typeof(ScriptableObject).IsAssignableFrom(fieldInfo.FieldType)) return;

			var asset = ScriptableObject.CreateInstance(fieldInfo.FieldType);
			string targetPath = AssetDatabase.GetAssetPath(property.serializedObject.targetObject);
			string directory = string.IsNullOrEmpty(targetPath) ? "Assets" : Path.GetDirectoryName(targetPath)?.Replace("\\", "/");
			if (string.IsNullOrEmpty(directory)) directory = "Assets";

			string path = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{fieldInfo.FieldType.Name}.asset");
			AssetDatabase.CreateAsset(asset, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			property.objectReferenceValue = asset;
			property.serializedObject.ApplyModifiedProperties();
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = asset;
		}
	}
}
