#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace RCore.Inspector
{
	/// <summary>
	/// A property attribute to highlight a serialized field in the inspector by changing its GUI color.
	/// This can be used to draw attention to important fields.
	/// </summary>
	/// <seealso cref="UnityEngine.PropertyAttribute" />
	public class HighlightAttribute : PropertyAttribute { }

#if UNITY_EDITOR
	/// <summary>
	/// A custom property drawer for fields marked with the [HighlightAttribute].
	/// It changes the GUI color to cyan before drawing the property.
	/// </summary>
	[CustomPropertyDrawer(typeof(HighlightAttribute))]
	public class HighlightPropertyDrawer : PropertyDrawer
	{
		/// <summary>
		/// Renders the property with a custom highlight color.
		/// </summary>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Store the original GUI color.
			var oldColor = GUI.color;
			// Set the GUI color to cyan for highlighting.
			GUI.color = Color.cyan;
			// Draw the property field.
			EditorGUI.PropertyField(position, property, label, true);
			// Restore the original GUI color.
			GUI.color = oldColor;
		}

		/// <summary>
		/// Gets the correct height for the property.
		/// </summary>
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}
	}
#endif
}