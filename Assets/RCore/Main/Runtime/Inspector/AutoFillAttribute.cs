using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace RCore.Inspector
{
	/// <summary>
	/// An attribute that marks a field for reference filling through Unity Editor context menus.
	/// It supports single object references and empty arrays/lists of Components or ScriptableObjects.
	/// </summary>
	public class AutoFillAttribute : PropertyAttribute
	{
		/// <summary>
		/// An optional path to specify where to search for the component or asset.
		/// For components, this is a relative path from the MonoBehaviour's Transform (e.g., "Child/GrandChild").
		/// For ScriptableObjects, this is a folder path within the "Assets" directory (e.g., "Assets/Data/Items").
		/// If empty, the search is performed globally (GetComponentsInChildren for components, entire AssetDatabase for ScriptableObjects).
		/// </summary>
		public string Path { get; private set; }

		/// <summary>
		/// Initializes a new instance of the AutoFillAttribute.
		/// </summary>
		/// <param name="path">Optional search path for the asset or component.</param>
		public AutoFillAttribute(string path = "")
		{
			Path = path;
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// The custom property drawer for fields marked with the [AutoFill] attribute.
	/// Use the RCore Auto Fill context menu on the owning Component or ScriptableObject to populate fields.
	/// </summary>
	[CustomPropertyDrawer(typeof(AutoFillAttribute))]
	public class AutoFillDrawer : PropertyDrawer
	{
		/// <summary>
		/// Draws the property without mutating serialized data during an inspector event.
		/// </summary>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.PropertyField(position, property, label, true);
		}

		/// <summary>
		/// Returns height required by expanded array and list fields.
		/// </summary>
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}
	}

	internal static class AutoFillContextMenu
	{
		[MenuItem("CONTEXT/Component/RCore Auto Fill", true)]
		private static bool ValidateComponent(MenuCommand command)
		{
			return HasAutoFillFields(command.context as Component);
		}

		[MenuItem("CONTEXT/Component/RCore Auto Fill")]
		private static void FillComponent(MenuCommand command)
		{
			Fill(command.context as Component);
		}

		[MenuItem("CONTEXT/ScriptableObject/RCore Auto Fill", true)]
		private static bool ValidateScriptableObject(MenuCommand command)
		{
			return HasAutoFillFields(command.context as ScriptableObject);
		}

		[MenuItem("CONTEXT/ScriptableObject/RCore Auto Fill")]
		private static void FillScriptableObject(MenuCommand command)
		{
			Fill(command.context as ScriptableObject);
		}

		private static bool HasAutoFillFields(UnityEngine.Object target)
		{
			return target != null && GetFields(target.GetType()).Any(field => field.GetCustomAttribute<AutoFillAttribute>() != null);
		}

		private static void Fill(UnityEngine.Object target)
		{
			if (target == null) return;

			var serializedObject = new SerializedObject(target);
			var targetComponent = target as Component;
			var changed = false;

			foreach (var field in GetFields(target.GetType()))
			{
				var autoFill = field.GetCustomAttribute<AutoFillAttribute>();
				if (autoFill == null) continue;

				var property = serializedObject.FindProperty(field.Name);
				if (property == null) continue;

				var elementType = GetElementType(field.FieldType);
				if (elementType != null)
				{
					if (property.arraySize != 0) continue;
					var matches = FindMatches(autoFill.Path, elementType, targetComponent);
					if (matches.Length == 0) continue;

					property.arraySize = matches.Length;
					for (int i = 0; i < matches.Length; i++)
						property.GetArrayElementAtIndex(i).objectReferenceValue = matches[i];
					changed = true;
					continue;
				}

				if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue != null) continue;
				var match = FindMatches(autoFill.Path, field.FieldType, targetComponent).FirstOrDefault();
				if (match == null) continue;

				property.objectReferenceValue = match;
				changed = true;
			}

			// ApplyModifiedProperties registers the undo entry and dirties the target.
			if (changed) serializedObject.ApplyModifiedProperties();
		}

		private static Type GetElementType(Type fieldType)
		{
			if (fieldType.IsArray) return fieldType.GetElementType();
			return fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)
				? fieldType.GetGenericArguments()[0]
				: null;
		}

		private static UnityEngine.Object[] FindMatches(string path, Type type, Component targetComponent)
		{
			if (typeof(Component).IsAssignableFrom(type))
			{
				if (targetComponent == null) return new UnityEngine.Object[0];
				if (string.IsNullOrEmpty(path)) return targetComponent.GetComponentsInChildren(type, true);

				var transform = targetComponent.transform.Find(path);
				var component = transform != null ? transform.GetComponent(type) : null;
				return component != null ? new UnityEngine.Object[] { component } : new UnityEngine.Object[0];
			}

			if (!typeof(ScriptableObject).IsAssignableFrom(type)) return new UnityEngine.Object[0];
			if (!string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path)) return new UnityEngine.Object[0];

			var guids = string.IsNullOrEmpty(path)
				? AssetDatabase.FindAssets($"t:{type.Name}")
				: AssetDatabase.FindAssets($"t:{type.Name}", new[] { path });
			return guids.Select(AssetDatabase.GUIDToAssetPath)
				.OrderBy(assetPath => assetPath, StringComparer.Ordinal)
				.Select(assetPath => AssetDatabase.LoadAssetAtPath(assetPath, type))
				.Where(asset => asset != null)
				.ToArray();
		}

		private static IEnumerable<FieldInfo> GetFields(Type type)
		{
			while (type != null && type != typeof(object))
			{
				foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
					yield return field;
				type = type.BaseType;
			}
		}
	}
#endif
}
