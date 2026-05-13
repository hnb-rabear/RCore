using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
	[CustomPropertyDrawer(typeof(ShowIfAttribute))]
	public sealed class ShowIfDrawer : PropertyDrawer
	{
		private ShowIfAttribute Attribute => (ShowIfAttribute)attribute;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return ShouldShow(property)
				? EditorGUI.GetPropertyHeight(property, label, true)
				: -EditorGUIUtility.standardVerticalSpacing;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (ShouldShow(property))
				EditorGUI.PropertyField(position, property, label, true);
		}

		private bool ShouldShow(SerializedProperty property)
		{
			object target = property.serializedObject.targetObject;
			var type = target.GetType();
			string name = Attribute.ConditionMemberName;

			var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null && field.FieldType == typeof(bool))
				return (bool)field.GetValue(target);

			var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (prop != null && prop.PropertyType == typeof(bool))
				return (bool)prop.GetValue(target);

			var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);
			if (method != null && method.ReturnType == typeof(bool))
				return (bool)method.Invoke(target, null);

			Debug.LogError($"ShowIf: bool member '{name}' not found on {type.Name}");
			return true;
		}
	}
}
