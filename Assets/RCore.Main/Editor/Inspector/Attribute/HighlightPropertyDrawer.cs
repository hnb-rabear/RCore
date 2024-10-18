using RCore.Inspector;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Inspector
{
	/// <summary>
	/// A property drawer for fields marked with the Highlight Attribute.
	/// </summary>
	[CustomPropertyDrawer(typeof(HighlightAttribute))]
	public class HighlightPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var oldColor = GUI.color;
			GUI.color = Color.cyan;
			EditorGUI.PropertyField(position, property, label);
			GUI.color = oldColor;
		}
	}
}