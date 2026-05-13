using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
	[CustomPropertyDrawer(typeof(SpriteBoxAttribute))]
	public sealed class SpriteBoxDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var attr = (SpriteBoxAttribute)attribute;
			return Mathf.Max(EditorGUI.GetPropertyHeight(property, label, true), attr.height) + EditorGUIUtility.standardVerticalSpacing;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType != SerializedPropertyType.ObjectReference)
			{
				EditorGUI.HelpBox(position, "SpriteBox requires Sprite field.", MessageType.Error);
				return;
			}

			var attr = (SpriteBoxAttribute)attribute;
			int indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			var previewRect = new Rect(position.x, position.y, attr.width, attr.height);
			var fieldRect = new Rect(position.x + attr.width + 4f, position.y, position.width - attr.width - 4f, EditorGUIUtility.singleLineHeight);

			property.objectReferenceValue = EditorGUI.ObjectField(previewRect, property.objectReferenceValue, typeof(Sprite), false);
			property.objectReferenceValue = EditorGUI.ObjectField(fieldRect, label, property.objectReferenceValue, typeof(Sprite), false);

			EditorGUI.indentLevel = indent;
		}
	}
}
