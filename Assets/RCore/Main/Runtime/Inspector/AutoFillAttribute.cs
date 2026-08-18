using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace RCore.Inspector
{
	/// <summary>
	/// An attribute that marks a field to be filled automatically while its Inspector is drawn.
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
	/// Empty references fill automatically for each inspected object.
	/// </summary>
	[CustomPropertyDrawer(typeof(AutoFillAttribute))]
	public class AutoFillDrawer : PropertyDrawer
	{
		private static readonly HashSet<string> s_checked = new HashSet<string>();

		static AutoFillDrawer()
		{
			Selection.selectionChanged += ClearChecks;
			EditorApplication.hierarchyChanged += ClearChecks;
			EditorApplication.projectChanged += ClearChecks;
			Undo.undoRedoPerformed += ClearChecks;
		}

		/// <summary>
		/// Draws the property and schedules automatic filling after the current Inspector event.
		/// </summary>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField(position, property, label, true);
			if (EditorGUI.EndChangeCheck())
				ClearChecks(property.serializedObject.targetObjects, property.propertyPath);

			if (Event.current.type == EventType.Repaint && fieldInfo != null
				&& AutoFillResolver.NeedsFill(property, fieldInfo.FieldType))
				ScheduleFill(property, fieldInfo.FieldType, ((AutoFillAttribute)attribute).Path);
		}

		/// <summary>
		/// Returns height required by expanded array and list fields.
		/// </summary>
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}

		private static void ScheduleFill(SerializedProperty property, Type fieldType, string path)
		{
			var propertyPath = property.propertyPath;
			var targets = new List<UnityEngine.Object>();
			var keys = new List<string>();

			foreach (var target in property.serializedObject.targetObjects)
			{
				if (target == null) continue;
				var key = GetKey(target, propertyPath);
				if (!s_checked.Add(key)) continue;
				targets.Add(target);
				keys.Add(key);
			}

			if (targets.Count == 0) return;
			EditorApplication.delayCall += () => Fill(targets, keys, propertyPath, fieldType, path);
		}

		private static void Fill(IReadOnlyList<UnityEngine.Object> targets, IReadOnlyList<string> keys,
			string propertyPath, Type fieldType, string path)
		{
			for (int i = 0; i < targets.Count; i++)
			{
				if (!s_checked.Contains(keys[i])) continue;
				var target = targets[i];
				if (target == null) continue;

				var serializedObject = new SerializedObject(target);
				serializedObject.UpdateIfRequiredOrScript();
				var property = serializedObject.FindProperty(propertyPath);
				if (property == null || !AutoFillResolver.NeedsFill(property, fieldType))
				{
					s_checked.Remove(keys[i]);
					continue;
				}

				if (AutoFillResolver.TryFill(property, fieldType, path, target as Component))
				{
					serializedObject.ApplyModifiedProperties();
					s_checked.Remove(keys[i]);
				}
			}
		}

		private static string GetKey(UnityEngine.Object target, string propertyPath)
		{
			return target.GetInstanceID() + "/" + propertyPath;
		}

		private static void ClearChecks()
		{
			s_checked.Clear();
		}

		private static void ClearChecks(IEnumerable<UnityEngine.Object> targets, string propertyPath)
		{
			foreach (var target in targets)
				if (target != null)
					s_checked.Remove(GetKey(target, propertyPath));
		}
	}

	/// <summary>
	/// Reference-resolution logic used by automatic Inspector filling.
	/// </summary>
	internal static class AutoFillResolver
	{
		/// <summary>
		/// Returns whether the property is still empty, so a fill is worth attempting.
		/// Cheap enough to call per repaint: it never touches the AssetDatabase or the scene.
		/// </summary>
		public static bool NeedsFill(SerializedProperty property, Type fieldType)
		{
			if (property.hasMultipleDifferentValues) return true;
			if (GetElementType(fieldType) != null)
				return property.isArray && property.arraySize == 0;
			return property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null;
		}

		/// <summary>
		/// Fills the property if it is still empty. Returns whether anything changed.
		/// </summary>
		public static bool TryFill(SerializedProperty property, Type fieldType, string path, Component owner)
		{
			var elementType = GetElementType(fieldType);
			if (elementType != null)
			{
				// A collection field can still resolve to a non-array property (e.g. a serialized wrapper); skip it.
				if (!property.isArray) return false;
				if (property.arraySize != 0) return false;
				var matches = FindMatches(path, elementType, owner);
				if (matches.Length == 0) return false;

				property.arraySize = matches.Length;
				for (int i = 0; i < matches.Length; i++)
					property.GetArrayElementAtIndex(i).objectReferenceValue = matches[i];
				return true;
			}

			if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue != null) return false;
			var match = FindMatches(path, fieldType, owner).FirstOrDefault();
			if (match == null) return false;

			property.objectReferenceValue = match;
			return true;
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
	}
#endif
}
