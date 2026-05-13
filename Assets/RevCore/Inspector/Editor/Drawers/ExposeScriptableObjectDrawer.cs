using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
	[CustomPropertyDrawer(typeof(ExposeScriptableObjectAttribute))]
	public sealed class ExposeScriptableObjectDrawer : PropertyDrawer
	{
		private const float Padding = 5f;
		private static readonly Color BackgroundColor = new(0.2f, 0.2f, 0.2f, 0.5f);
		private bool m_foldout = true;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = EditorGUIUtility.singleLineHeight;
			if (!m_foldout || property.objectReferenceValue is not ScriptableObject scriptableObject) return height;

			var serializedObject = new SerializedObject(scriptableObject);
			var iterator = serializedObject.GetIterator();
			bool enterChildren = true;
			while (iterator.NextVisible(enterChildren))
			{
				enterChildren = false;
				if (iterator.propertyPath == "m_Script") continue;
				height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
			}

			return height + Padding * 2f;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var foldoutRect = new Rect(position.x, position.y, 14f, EditorGUIUtility.singleLineHeight);
			var fieldRect = new Rect(position.x + 14f, position.y, position.width - 14f, EditorGUIUtility.singleLineHeight);

			m_foldout = EditorGUI.Foldout(foldoutRect, m_foldout, GUIContent.none);
			EditorGUI.PropertyField(fieldRect, property, label, true);

			if (!m_foldout || property.objectReferenceValue is not ScriptableObject scriptableObject) return;

			var serializedObject = new SerializedObject(scriptableObject);
			var bgRect = new Rect(position.x + Padding, position.y + EditorGUIUtility.singleLineHeight + Padding, position.width - Padding * 2f, position.height - EditorGUIUtility.singleLineHeight - Padding);
			EditorGUI.DrawRect(bgRect, BackgroundColor);

			var iterator = serializedObject.GetIterator();
			bool enterChildren = true;
			float y = bgRect.y + Padding;
			while (iterator.NextVisible(enterChildren))
			{
				enterChildren = false;
				if (iterator.propertyPath == "m_Script") continue;

				float propHeight = EditorGUI.GetPropertyHeight(iterator, true);
				var propRect = new Rect(bgRect.x + Padding, y, bgRect.width - Padding * 2f, propHeight);
				EditorGUI.PropertyField(propRect, iterator, true);
				y += propHeight + EditorGUIUtility.standardVerticalSpacing;
			}
			serializedObject.ApplyModifiedProperties();
		}
	}
}
