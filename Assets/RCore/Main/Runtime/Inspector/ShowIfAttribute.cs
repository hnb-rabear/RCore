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
	/// The target member must return a boolean value.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class ShowIfAttribute : PropertyAttribute
	{
		public string ConditionMemberName { get; private set; }

		/// <param name="conditionMemberName">The name of the boolean field, property, or parameterless method to check.</param>
		public ShowIfAttribute(string conditionMemberName)
		{
			ConditionMemberName = conditionMemberName;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(ShowIfAttribute))]
	public class ShowIfDrawer : PropertyDrawer
	{
		// Cast the attribute for easier access
		private ShowIfAttribute Attribute => (ShowIfAttribute)attribute;

		/// <summary>
		/// Checks if the condition defined by the ShowIf attribute is met.
		/// </summary>
		/// <param name="property">The SerializedProperty representing the field this drawer is for.</param>
		/// <returns>True if the condition is met (field should be shown), false otherwise.</returns>
		private bool ShouldShow(SerializedProperty property)
		{
			// Get the containing object (the MonoBehaviour script)
			object targetObject = property.serializedObject.targetObject;
			var targetType = targetObject.GetType();
			string conditionMemberName = Attribute.ConditionMemberName;

			if (string.IsNullOrEmpty(conditionMemberName))
			{
				Debug.LogError($"ShowIfAttribute on '{property.displayName}' has no ConditionMemberName specified.");
				return true; // Show by default if misconfigured
			}

			var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

			// Try to find a Field
			var field = targetType.GetField(conditionMemberName, bindingFlags);
			if (field != null)
			{
				if (field.FieldType == typeof(bool))
				{
					return (bool)field.GetValue(targetObject);
				}
				else
				{
					Debug.LogError($"ShowIfAttribute: Member '{conditionMemberName}' on {targetType.Name} is not a boolean field.");
					return true; // Show on error
				}
			}

			// Try to find a Property
			var propertyInfo = targetType.GetProperty(conditionMemberName, bindingFlags);
			if (propertyInfo != null)
			{
				if (propertyInfo.PropertyType == typeof(bool))
				{
					try
					{
						return (bool)propertyInfo.GetValue(targetObject);
					}
					catch (System.Exception e)
					{
						Debug.LogError($"ShowIfAttribute: Error evaluating property '{conditionMemberName}' on {targetType.Name}. \n{e}");
						return true; // Show on error
					}
				}
				else
				{
					Debug.LogError($"ShowIfAttribute: Member '{conditionMemberName}' on {targetType.Name} is not a boolean property.");
					return true; // Show on error
				}
			}

			// Try to find a Method (parameterless)
			var methodInfo = targetType.GetMethod(conditionMemberName, bindingFlags, null, System.Type.EmptyTypes, null);
			if (methodInfo != null)
			{
				if (methodInfo.ReturnType == typeof(bool))
				{
					try
					{
						// Ensure it's parameterless
						if (methodInfo.GetParameters().Length == 0)
						{
							return (bool)methodInfo.Invoke(targetObject, null);
						}
						else
						{
							Debug.LogError($"ShowIfAttribute: Method '{conditionMemberName}' on {targetType.Name} must be parameterless.");
							return true; // Show on error
						}
					}
					catch (System.Exception e)
					{
						Debug.LogError($"ShowIfAttribute: Error evaluating method '{conditionMemberName}' on {targetType.Name}. \n{e}");
						return true; // Show on error
					}
				}
				else
				{
					Debug.LogError($"ShowIfAttribute: Method '{conditionMemberName}' on {targetType.Name} does not return boolean.");
					return true; // Show on error
				}
			}


			// If no matching member found
			Debug.LogError($"ShowIfAttribute: Could not find boolean field, property, or parameterless method named '{conditionMemberName}' on {targetType.Name}.");
			return true; // Show by default if condition member not found
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Check the condition. If it's not met, don't draw anything.
			if (ShouldShow(property))
			{
				EditorGUI.PropertyField(position, property, label, true);
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Check the condition. If it's not met, return 0 height (or negative to collapse spacing).
			if (ShouldShow(property))
			{
				// Return the standard height for this property
				return EditorGUI.GetPropertyHeight(property, label, true);
			}
			else
			{
				// Return minimal height to hide the field and collapse space
				return -EditorGUIUtility.standardVerticalSpacing; // Collapses the default spacing between properties
				// return 0; // Alternative: just returns zero height
			}
		}
	}
#endif
}