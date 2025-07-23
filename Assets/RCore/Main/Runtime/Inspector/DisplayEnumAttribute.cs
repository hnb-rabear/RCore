using System;
using System.Reflection;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace RCore.Inspector
{
	/// <summary>
	/// An attribute used to display an integer field as a dropdown of enum values in the Inspector.
	/// This is particularly useful when you need to serialize an enum's value as an integer but still
	/// want a user-friendly dropdown in the editor. The enum type can be provided directly or
	/// determined dynamically at runtime by calling a specified method.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class DisplayEnumAttribute : PropertyAttribute
	{
		/// <summary>
		/// Gets the hard-coded enum type to display. This is null if a method name is used instead.
		/// </summary>
		public Type EnumType { get; private set; }
		
		/// <summary>
		/// Gets the name of the method to call to dynamically get the enum type. This is null if a type is provided directly.
		/// </summary>
		public string MethodName { get; private set; }

		/// <summary>
		/// Initializes the attribute with a specific enum type.
		/// </summary>
		/// <param name="enumType">The System.Type of the enum to display in the dropdown.</param>
		public DisplayEnumAttribute(Type enumType)
		{
			if (!enumType.IsEnum)
			{
				throw new ArgumentException("EnumType must be an enum type.");
			}
			EnumType = enumType;
		}

		/// <summary>
		/// Initializes the attribute with the name of a method that will provide the enum type at runtime.
		/// The method must return a System.Type object.
		/// </summary>
		/// <param name="methodName">The name of the parameterless method on the target object that returns the enum type.</param>
		public DisplayEnumAttribute(string methodName)
		{
			MethodName = methodName;
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// The custom property drawer for integer fields marked with the [DisplayEnum] attribute.
	/// It renders the integer field as an enum popup dropdown.
	/// </summary>
	[CustomPropertyDrawer(typeof(DisplayEnumAttribute))]
	public class DisplayEnumDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var displayEnum = (DisplayEnumAttribute)attribute;
			Type enumType = null;

			// Get the object that owns the property being drawn.
			object targetObject = GetTargetObjectWithProperty(property);

			// Determine the enum type to use, either from a method or a direct type reference.
			if (!string.IsNullOrEmpty(displayEnum.MethodName))
			{
				// Dynamically get the enum type by invoking the specified method.
				var methodInfo = targetObject.GetType().GetMethod(displayEnum.MethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (methodInfo != null && methodInfo.ReturnType == typeof(Type))
				{
					enumType = methodInfo.Invoke(targetObject, null) as Type;
				}
			}
			else if (displayEnum.EnumType != null)
			{
				// Use the hard-coded enum type.
				enumType = displayEnum.EnumType;
			}

			// If we have a valid enum type, draw the enum dropdown.
			if (enumType != null && enumType.IsEnum)
			{
				// Convert the integer property value to its corresponding enum value.
				var selectedEnum = (Enum)Enum.ToObject(enumType, property.intValue);
				// Draw the enum popup.
				selectedEnum = EditorGUI.EnumPopup(position, label, selectedEnum);
				// Convert the selected enum value back to an integer and save it.
				property.intValue = Convert.ToInt32(selectedEnum);
			}
			else
			{
				// If no valid enum type could be found, draw the integer field as a fallback.
				EditorGUI.PropertyField(position, property, label, true);
			}
		}

		/// <summary>
		/// Traverses the serialized property path to find the actual object that contains the field.
		/// This is necessary because the `property.serializedObject.targetObject` is the top-level object (e.g., the MonoBehaviour),
		/// but the field might be inside a nested class or a list element.
		/// </summary>
		private object GetTargetObjectWithProperty(SerializedProperty property)
		{
			object targetObject = property.serializedObject.targetObject;
			string[] propertyPath = property.propertyPath.Split('.');

			// Navigate through the path (e.g., "myList.Array.data[0].myIntField").
			for (int i = 0; i < propertyPath.Length - 1; i++)
			{
				var part = propertyPath[i];
				if (part == "Array" && propertyPath.Length > i + 1 && propertyPath[i + 1].StartsWith("data["))
				{
					// Handle array elements.
					int startIndex = propertyPath[i + 1].IndexOf('[') + 1;
					string indexString = propertyPath[i + 1].Substring(startIndex, propertyPath[i + 1].Length - startIndex - 1);
					var index = int.Parse(indexString);
					targetObject = GetValueFromArray(targetObject, index);
					i++; // Skip the "data[x]" part in the next iteration.
				}
				else
				{
					// Handle regular fields.
					var field = targetObject.GetType().GetField(part, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
					if (field != null)
						targetObject = field.GetValue(targetObject);
				}
			}
			return targetObject;
		}

		/// <summary>
		/// A helper method to get a value from an object that implements IList (like an array or List<>).
		/// </summary>
		private object GetValueFromArray(object source, int index)
		{
			if (source is IList array && index >= 0 && index < array.Count)
			{
				return array[index];
			}
			return null;
		}
	}
#endif
}