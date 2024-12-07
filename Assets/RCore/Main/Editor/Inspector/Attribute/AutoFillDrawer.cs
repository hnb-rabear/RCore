#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using RCore.Inspector;

namespace RCore.Editor.Inspector
{
	[CustomPropertyDrawer(typeof(AutoFillAttribute))]
	public class AutoFillDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var autoFill = (AutoFillAttribute)attribute;
			if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
			{
				HandleArrayProperty(position, property, label, autoFill);
			}
			else
			{
				HandleObjectReferenceProperty(position, property, label, autoFill);
			}
		}

		private void HandleObjectReferenceProperty(Rect position, SerializedProperty property, GUIContent label, AutoFillAttribute autoFill)
		{
			if (property.objectReferenceValue == null)
			{
				var fieldType = fieldInfo.FieldType;

				if (typeof(MonoBehaviour).IsAssignableFrom(fieldType) || typeof(Component).IsAssignableFrom(fieldType))
				{
					var targetMonoBehaviour = property.serializedObject.targetObject as MonoBehaviour;
					if (targetMonoBehaviour != null)
					{
						FindComponentInChildren(autoFill, property, fieldType, targetMonoBehaviour);
					}
				}
				else if (typeof(ScriptableObject).IsAssignableFrom(fieldType))
				{
					FindScriptableObject(autoFill, property, fieldType);
				}
			}

			EditorGUI.PropertyField(position, property, label);
		}

		private void HandleArrayProperty(Rect position, SerializedProperty property, GUIContent label, AutoFillAttribute autoFill)
		{
			var elementType = fieldInfo.FieldType.GetElementType() ?? fieldInfo.FieldType.GetGenericArguments().FirstOrDefault();
			if (elementType == null) return;

			if (typeof(MonoBehaviour).IsAssignableFrom(elementType) || typeof(Component).IsAssignableFrom(elementType))
			{
				var targetMonoBehaviour = property.serializedObject.targetObject as MonoBehaviour;
				if (targetMonoBehaviour != null)
				{
					var components = targetMonoBehaviour.GetComponentsInChildren(elementType);
					AssignComponentsToArrayProperty(property, components);
				}
			}
			else if (typeof(ScriptableObject).IsAssignableFrom(elementType))
			{
				var guids = string.IsNullOrEmpty(autoFill.Path) ? AssetDatabase.FindAssets($"t:{elementType.Name}") : AssetDatabase.FindAssets($"t:{elementType.Name}", new[] { autoFill.Path });
				var assets = guids.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>).ToArray();
				AssignScriptableObjectsToArrayProperty(property, assets);
			}

			EditorGUI.PropertyField(position, property, label, true);
		}

		private void FindComponentInChildren(AutoFillAttribute autoFill, SerializedProperty property, System.Type fieldType, MonoBehaviour targetMonoBehaviour)
		{
			if (string.IsNullOrEmpty(autoFill.Path))
			{
				var component = targetMonoBehaviour.GetComponentsInChildren(fieldType).FirstOrDefault();
				if (component != null)
				{
					property.objectReferenceValue = component;
				}
			}
			else
			{
				var target = targetMonoBehaviour.transform.Find(autoFill.Path);
				if (target != null)
				{
					var component = target.GetComponent(fieldType);
					if (component != null)
					{
						property.objectReferenceValue = component;
					}
				}
			}
		}

		private void FindScriptableObject(AutoFillAttribute autoFill, SerializedProperty property, System.Type fieldType)
		{
			string[] guids = string.IsNullOrEmpty(autoFill.Path) ? AssetDatabase.FindAssets($"t:{fieldType.Name}") : AssetDatabase.FindAssets($"t:{fieldType.Name}", new[] { autoFill.Path });
			var assets = guids.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>).ToArray();

			if (assets.Length > 0)
			{
				property.objectReferenceValue = assets[0];
			}
		}

		private void AssignComponentsToArrayProperty(SerializedProperty property, Component[] components)
		{
			property.arraySize = components.Length;
			for (int i = 0; i < components.Length; i++)
			{
				property.GetArrayElementAtIndex(i).objectReferenceValue = components[i];
			}
		}

		private void AssignScriptableObjectsToArrayProperty(SerializedProperty property, ScriptableObject[] assets)
		{
			property.arraySize = assets.Length;
			for (int i = 0; i < assets.Length; i++)
			{
				property.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
			}
		}
	}
}
#endif