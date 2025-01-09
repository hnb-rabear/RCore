using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace RCore.Inspector
{
    [CustomPropertyDrawer(typeof(DisplayEnumAttribute))]
    public class DisplayEnumDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var displayEnum = (DisplayEnumAttribute)attribute;
            Type enumType = null;

            // Get the target object and traverse the property path
            object targetObject = GetTargetObjectWithProperty(property);

            // Try to get the enum type from the method, if provided
            if (!string.IsNullOrEmpty(displayEnum.MethodName))
            {
                var methodInfo = targetObject.GetType().GetMethod(displayEnum.MethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (methodInfo != null && methodInfo.ReturnType == typeof(Type))
                {
                    enumType = methodInfo.Invoke(targetObject, null) as Type;
                }
            }
            else if (displayEnum.EnumType != null)
            {
                enumType = displayEnum.EnumType;
            }

            // Display the enum popup if we have a valid enum type
            if (enumType != null && enumType.IsEnum)
            {
                var selectedEnum = (Enum)Enum.ToObject(enumType, property.intValue);
                selectedEnum = EditorGUI.EnumPopup(position, label, selectedEnum);
                property.intValue = Convert.ToInt32(selectedEnum);
            }
            else
            {
                // Draw the property normally if the enum type is invalid
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        private object GetTargetObjectWithProperty(SerializedProperty property)
        {
            object targetObject = property.serializedObject.targetObject;
            string[] propertyPath = property.propertyPath.Split('.');

            for (int i = 0; i < propertyPath.Length - 1; i++)
            {
                if (propertyPath[i].StartsWith("data["))
                {
                    int startIndex = propertyPath[i].IndexOf('[') + 1;
                    int endIndex = propertyPath[i].IndexOf(']');
                    string indexString = propertyPath[i].Substring(startIndex, endIndex - startIndex);
                    var index = int.Parse(indexString);
                    targetObject = GetValueFromArray(targetObject, index);
                }
                else
                {
                    var field = targetObject.GetType().GetField(propertyPath[i], BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (field != null)
                        targetObject = field.GetValue(targetObject);
                }
            }
            return targetObject;
        }

        private object GetValueFromArray(object source, int index)
        {
            if (source is IList array && index >= 0 && index < array.Count)
            {
                return array[index];
            }
            return null;
        }
    }
}