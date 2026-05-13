using System;
using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
	[CustomPropertyDrawer(typeof(AutoFillAttribute))]
	public sealed class AutoFillDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.PropertyField(position, property, label, true);

			if (property.isArray)
				HandleArrayProperty(property);
			else
				HandleObjectReferenceProperty(property);
		}

		private void HandleObjectReferenceProperty(SerializedProperty property)
		{
			if (property.propertyType != SerializedPropertyType.ObjectReference) return;
			if (property.objectReferenceValue != null) return;

			var go = (property.serializedObject.targetObject as Component)?.gameObject;
			if (go == null) return;

			var attr = (AutoFillAttribute)attribute;
			var fieldType = fieldInfo.FieldType;

			if (typeof(Component).IsAssignableFrom(fieldType))
			{
				Component found = string.IsNullOrEmpty(attr.Path)
					? go.GetComponentInChildren(fieldType, true)
					: FindComponentAtPath(go, attr.Path, fieldType);
				if (found != null)
				{
					property.objectReferenceValue = found;
					property.serializedObject.ApplyModifiedProperties();
				}
			}
			else if (typeof(ScriptableObject).IsAssignableFrom(fieldType))
			{
				string[] guids = AssetDatabase.FindAssets($"t:{fieldType.Name}");
				if (guids.Length > 0)
				{
					string path = AssetDatabase.GUIDToAssetPath(guids[0]);
					property.objectReferenceValue = AssetDatabase.LoadAssetAtPath(path, fieldType);
					property.serializedObject.ApplyModifiedProperties();
				}
			}
		}

		private void HandleArrayProperty(SerializedProperty property)
		{
			if (property.arraySize > 0) return;

			var go = (property.serializedObject.targetObject as Component)?.gameObject;
			if (go == null) return;

			var elementType = fieldInfo.FieldType.IsArray
				? fieldInfo.FieldType.GetElementType()
				: fieldInfo.FieldType.GetGenericArguments().Length > 0
					? fieldInfo.FieldType.GetGenericArguments()[0]
					: null;

			if (elementType == null) return;

			if (typeof(Component).IsAssignableFrom(elementType))
			{
				var components = go.GetComponentsInChildren(elementType, true);
				if (components.Length == 0) return;
				property.arraySize = components.Length;
				for (int i = 0; i < components.Length; i++)
					property.GetArrayElementAtIndex(i).objectReferenceValue = components[i];
				property.serializedObject.ApplyModifiedProperties();
			}
			else if (typeof(ScriptableObject).IsAssignableFrom(elementType))
			{
				string[] guids = AssetDatabase.FindAssets($"t:{elementType.Name}");
				if (guids.Length == 0) return;
				property.arraySize = guids.Length;
				for (int i = 0; i < guids.Length; i++)
				{
					string path = AssetDatabase.GUIDToAssetPath(guids[i]);
					property.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath(path, elementType);
				}
				property.serializedObject.ApplyModifiedProperties();
			}
		}

		private static Component FindComponentAtPath(GameObject root, string path, Type type)
		{
			var child = root.transform.Find(path);
			return child != null ? child.GetComponent(type) : null;
		}
	}
}
