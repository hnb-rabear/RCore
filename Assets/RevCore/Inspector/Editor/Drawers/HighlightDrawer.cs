using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
	[CustomPropertyDrawer(typeof(HighlightAttribute))]
	public sealed class HighlightDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			=> EditorGUI.GetPropertyHeight(property, label, true);

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Color color = GUI.color;
			GUI.color = Color.cyan;
			EditorGUI.PropertyField(position, property, label, true);
			GUI.color = color;
		}
	}
}
