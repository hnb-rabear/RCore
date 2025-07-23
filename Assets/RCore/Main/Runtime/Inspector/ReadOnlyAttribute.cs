using UnityEditor;
using UnityEngine;

namespace RCore.Inspector
{
	/// <summary>
	/// A property attribute that makes a serialized field read-only in the Unity Inspector.
	/// The field will be displayed but cannot be edited.
	/// </summary>
	public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
	/// <summary>
	/// Custom property drawer for fields marked with [ReadOnlyAttribute].
	/// This drawer disables the GUI before drawing the property, making it non-editable.
	/// </summary>
	[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
	public class ReadOnlyPropertyDrawer : PropertyDrawer
	{
		/// <summary>
		/// Gets the correct height for the property.
		/// </summary>
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}

		/// <summary>
		/// Renders the property field in a disabled state.
		/// </summary>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Disable the GUI, effectively making the property read-only.
			bool wasEnabled = GUI.enabled;
			GUI.enabled = false;
			EditorGUI.PropertyField(position, property, label, true);
			// Restore the previous GUI enabled state.
			GUI.enabled = wasEnabled;
		}
	}
#endif
}