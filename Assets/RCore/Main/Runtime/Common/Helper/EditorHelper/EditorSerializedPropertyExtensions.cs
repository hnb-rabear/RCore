#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
    /// <summary>
    /// Provides a collection of static extension methods for Unity's SerializedObject and SerializedProperty classes.
    /// These methods simplify common operations performed in custom inspectors, such as drawing multiple properties
    /// and retrieving the underlying object of a SerializedProperty.
    /// </summary>
    public static class EditorSerializedPropertyExtensions
    {
		/// <summary>
		/// Draws multiple child properties of a given SerializedProperty using EditorGUILayout.PropertyField.
		/// </summary>
		/// <param name="pProperty">The parent SerializedProperty.</param>
		/// <param name="properties">An array of relative property names to draw.</param>
		public static void SerializeFields(this SerializedProperty pProperty, params string[] properties)
		{
			foreach (var p in properties)
			{
				var item = pProperty.FindPropertyRelative(p);
				if (item != null)
					EditorGUILayout.PropertyField(item, true);
				else
					Debug.LogWarning($"Property '{p}' not found relative to '{pProperty.propertyPath}'.");
			}
		}

		/// <summary>
		/// Draws multiple properties of a given SerializedObject using EditorGUILayout.PropertyField.
		/// </summary>
		/// <param name="pObj">The SerializedObject.</param>
		/// <param name="properties">An array of property names to draw.</param>
		public static void SerializeFields(this SerializedObject pObj, params string[] properties)
		{
			foreach (var p in properties)
				pObj.SerializeField(p);
		}

		/// <summary>
		/// Finds and draws a single property of a SerializedObject using EditorGUILayout.PropertyField.
		/// This provides a concise way to draw a property with an optional custom display name.
		/// </summary>
		/// <param name="pObj">The SerializedObject.</param>
		/// <param name="pPropertyName">The name of the property to find and draw.</param>
		/// <param name="pDisplayName">An optional display name to use instead of the default. If null, the default is used.</param>
		/// <param name="options">Optional GUILayout options.</param>
		/// <returns>The found SerializedProperty, or null if it was not found.</returns>
		public static SerializedProperty SerializeField(this SerializedObject pObj, string pPropertyName, string pDisplayName = null, params GUILayoutOption[] options)
		{
			var property = pObj.FindProperty(pPropertyName);
			if (property == null)
			{
				UnityEngine.Debug.LogError($"Not found property {pPropertyName} in {pObj.targetObject.name}");
				return null;
			}

			// Use the appropriate PropertyField overload for arrays/lists vs. single fields.
			if (!property.isArray)
			{
				EditorGUILayout.PropertyField(property, new GUIContent(string.IsNullOrEmpty(pDisplayName) ? property.displayName : pDisplayName), true, options);
				return property;
			}

			if (property.isExpanded)
				EditorGUILayout.PropertyField(property, true, options);
			else
				EditorGUILayout.PropertyField(property, new GUIContent(property.displayName), false, options); // Pass 'false' for children when collapsed
			return property;
		}

		/// <summary>
		/// Retrieves the actual C# object that a SerializedProperty represents.
		/// This is useful when you need to access the raw object instance from its serialized representation,
		/// especially for properties nested within classes or lists.
		/// </summary>
		/// <param name="prop">The SerializedProperty.</param>
		/// <returns>The target object instance, or null if it cannot be found.</returns>
		public static object GetTargetObjectOfProperty(SerializedProperty prop)
		{
			if (prop == null) return null;

			var path = prop.propertyPath.Replace(".Array.data[", "[");
			object obj = prop.serializedObject.targetObject;
			var elements = path.Split('.');
			foreach (var element in elements)
			{
				if (element.Contains("["))
				{
					var elementName = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
					var index = Convert.ToInt32(element.Substring(element.IndexOf("[", StringComparison.Ordinal))
						.Replace("[", "")
						.Replace("]", ""));
					obj = GetValue(obj, elementName, index);
				}
				else
				{
					obj = GetValue(obj, element);
				}
			}

			return obj;
		}

		/// <summary>
		/// Checks if a SerializedProperty represents the first element (index 0) of an array or list.
		/// </summary>
		/// <param name="property">The property to check.</param>
		/// <returns>True if the property is the first element of a collection, otherwise false.</returns>
		public static bool IsFirstElementOfList(this SerializedProperty property)
		{
			string path = property.propertyPath;
			int index = path.LastIndexOf('[');
			if (index < 0)
				return false;

			int endIndex = path.IndexOf(']', index);
			if (endIndex < 0)
				return false;

			int elementIndex = int.Parse(path.Substring(index + 1, endIndex - index - 1));
			return elementIndex == 0;
		}

		/// <summary>
		/// Checks if a SerializedProperty is an element within an array or list.
		/// This is a simple check based on whether the property's display name contains "Element".
		/// </summary>
		/// <param name="property">The property to check.</param>
		/// <returns>True if the property is likely an element in a collection.</returns>
		public static bool IsInList(this SerializedProperty property)
		{
			return property.displayName.Contains("Element");
		}
		
		/// <summary>
		/// A helper method that uses reflection to get the value of a field or property from a source object.
		/// It searches the entire inheritance hierarchy.
		/// </summary>
		/// <param name="source">The object containing the field or property.</param>
		/// <param name="name">The name of the field or property.</param>
		/// <returns>The value of the member, or null if not found.</returns>
		private static object GetValue(object source, string name)
		{
			if (source == null)
				return null;
			var type = source.GetType();

			while (type != null)
			{
				var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				if (f != null)
					return f.GetValue(source);

				var p = type.GetProperty(name,
					BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
				if (p != null)
					return p.GetValue(source, null);

				type = type.BaseType;
			}

			return null;
		}

		/// <summary>
		/// A helper method that gets an element at a specific index from a collection (field or property) on a source object.
		/// </summary>
		/// <param name="source">The object containing the collection.</param>
		/// <param name="name">The name of the collection field or property.</param>
		/// <param name="index">The index of the element to retrieve.</param>
		/// <returns>The element at the specified index, or null if not found.</returns>
		private static object GetValue(object source, string name, int index)
		{
			var enumerable = GetValue(source, name) as System.Collections.IEnumerable;
			if (enumerable == null) return null;
			var enm = enumerable.GetEnumerator();

			for (int i = 0; i <= index; i++)
			{
				if (!enm.MoveNext()) return null;
			}

			return enm.Current;
		}
    }
}
#endif