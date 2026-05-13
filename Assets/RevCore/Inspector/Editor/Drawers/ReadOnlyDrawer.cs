using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
	[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
	public sealed class ReadOnlyDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			=> EditorGUI.GetPropertyHeight(property, label, true);

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			bool enabled = GUI.enabled;
			GUI.enabled = false;
			EditorGUI.PropertyField(position, property, label, true);
			GUI.enabled = enabled;
		}
	}
}
