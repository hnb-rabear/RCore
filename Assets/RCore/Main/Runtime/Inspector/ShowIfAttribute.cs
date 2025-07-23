using UnityEngine;
using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Inspector
{
	/// <summary>
	/// Conditionally shows a field in the Inspector based on the value of a boolean field,
	/// property, or method within the same component.
	/// Usage: [ShowIf("MyBooleanField")] or [ShowIf("MyBoolProperty")] or [ShowIf("MyBoolMethod")]
	/// The target member must return a boolean value and be parameterless if it is a method.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class ShowIfAttribute : PropertyAttribute
	{
		/// <summary>
		/// The name of the boolean field, property, or parameterless method used as the condition.
		/// </summary>
		public string ConditionMemberName { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ShowIfAttribute"/> class.
		/// </summary>
		/// <param name="conditionMemberName">The name of the boolean field, property, or parameterless method to check.</param>
		public ShowIfAttribute(string conditionMemberName)
		{
			ConditionMemberName = conditionMemberName;
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Custom property drawer for fields marked with [ShowIfAttribute].
	/// This drawer checks a condition and shows or hides the field accordingly.
	/// </summary>
	[CustomPropertyDrawer(typeof(ShowIfAttribute))]
	public class ShowIfDrawer : PropertyDrawer
	{
		/// <summary>
		/// Gets the ShowIfAttribute instance for this drawer.
		/// </summary>
		private ShowIfAttribute Attribute => (ShowIfAttribute)attribute;

		/// <summary>
		/// Checks if the condition defined by the ShowIf attribute is met by evaluating
		/// the specified member (field, property, or method).
		/// </summary>
		/// <param name="property">The SerializedProperty representing the field this drawer is for.</param>
		/// <returns>True if the condition is met and the field should be shown; otherwise, false.</returns>
		private bool ShouldShow(SerializedProperty property)
		{
			object targetObject = property.serializedObject.targetObject;
			var targetType = targetObject.GetType();
			string conditionMemberName = Attribute.ConditionMemberName;

			if (string.IsNullOrEmpty(conditionMemberName))
			{
				Debug.LogError($"ShowIfAttribute on '{property.displayName}' has no ConditionMemberName specified.");
				return true; // Show by default if misconfigured
			}

			var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

			// Check for a field first
			var field = targetType.GetField(conditionMemberName, bindingFlags);
			if (field != null && field.FieldType == typeof(bool))
			{
				return (bool)field.GetValue(targetObject);
			}

			// Then check for a property
			var propertyInfo = targetType.GetProperty(conditionMemberName, bindingFlags);
			if (propertyInfo != null && propertyInfo.PropertyType == typeof(bool))
			{
				return (bool)propertyInfo.GetValue(targetObject);
			}

			// Finally, check for a parameterless method
			var methodInfo = targetType.GetMethod(conditionMemberName, bindingFlags);
			if (methodInfo != null && methodInfo.ReturnType == typeof(bool) && methodInfo.GetParameters().Length == 0)
			{
				return (bool)methodInfo.Invoke(targetObject, null);
			}

			Debug.LogError($"ShowIfAttribute: Could not find a boolean field, property, or parameterless method named '{conditionMemberName}' on {targetType.Name}.");
			return true; // Show by default if the condition member isn't found
		}

		/// <summary>
		/// Renders the property field if the condition is met.
		/// </summary>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Only draw the property if the condition is true
			if (ShouldShow(property))
			{
				EditorGUI.PropertyField(position, property, label, true);
			}
		}

		/// <summary>
		/// Gets the height of the property. Returns 0 if the property should be hidden.
		/// </summary>
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (ShouldShow(property))
			{
				// Return the standard height for this property
				return EditorGUI.GetPropertyHeight(property, label, true);
			}
			else
			{
				// Return a negative value to collapse the vertical spacing between properties
				return -EditorGUIUtility.standardVerticalSpacing;
			}
		}
	}
#endif
}