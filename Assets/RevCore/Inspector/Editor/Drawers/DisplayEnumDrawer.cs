using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
	[CustomPropertyDrawer(typeof(DisplayEnumAttribute))]
	public sealed class DisplayEnumDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var attr = (DisplayEnumAttribute)attribute;
			Type enumType = attr.EnumType ?? GetEnumTypeFromMethod(property, attr.MethodName);

			if (enumType == null || !enumType.IsEnum || property.propertyType != SerializedPropertyType.Integer)
			{
				EditorGUI.PropertyField(position, property, label, true);
				return;
			}

			var current = Enum.ToObject(enumType, property.intValue) as Enum;
			var selected = EditorGUI.EnumPopup(position, label, current);
			property.intValue = Convert.ToInt32(selected);
		}

		private static Type GetEnumTypeFromMethod(SerializedProperty property, string methodName)
		{
			if (string.IsNullOrEmpty(methodName)) return null;
			object target = GetTargetObjectWithProperty(property);
			if (target == null) return null;

			var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
			return method != null && method.ReturnType == typeof(Type) ? (Type)method.Invoke(target, null) : null;
		}

		private static object GetTargetObjectWithProperty(SerializedProperty property)
		{
			object obj = property.serializedObject.targetObject;
			string[] elements = property.propertyPath.Replace(".Array.data[", "[").Split('.');
			for (int i = 0; i < elements.Length - 1; i++)
			{
				string element = elements[i];
				if (element.Contains("["))
				{
					string name = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
					int index = int.Parse(element.Substring(element.IndexOf("[", StringComparison.Ordinal)).Replace("[", "").Replace("]", ""));
					obj = GetValue(obj, name, index);
				}
				else
				{
					obj = GetValue(obj, element);
				}
			}
			return obj;
		}

		private static object GetValue(object source, string name)
		{
			if (source == null) return null;
			var type = source.GetType();
			while (type != null)
			{
				var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (field != null) return field.GetValue(source);
				type = type.BaseType;
			}
			return null;
		}

		private static object GetValue(object source, string name, int index)
		{
			var enumerable = GetValue(source, name) as IEnumerable;
			if (enumerable == null) return null;
			var enumerator = enumerable.GetEnumerator();
			for (int i = 0; i <= index; i++)
				if (!enumerator.MoveNext()) return null;
			return enumerator.Current;
		}
	}
}
