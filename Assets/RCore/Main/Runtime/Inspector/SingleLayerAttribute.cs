#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace RCore.Inspector
{
	/// <summary>
	/// An attribute used to mark an integer field as a layer selection dropdown in the Inspector.
	/// The integer field will store the selected layer's index.
	/// </summary>
	public class SingleLayerAttribute : PropertyAttribute { }

#if UNITY_EDITOR
	/// <summary>
	/// Custom property drawer for fields marked with [SingleLayerAttribute].
	/// This drawer displays an integer field as a layer dropdown menu.
	/// </summary>
	[CustomPropertyDrawer(typeof(SingleLayerAttribute))]
	public class SingleLayerPropertyDrawer : PropertyDrawer
	{
		/// <summary>
		/// Renders the property as a layer selection field.
		/// </summary>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Ensure the property is an integer
			if (property.propertyType == SerializedPropertyType.Integer)
			{
				// Use EditorGUI.LayerField to create the dropdown.
				// It automatically handles displaying layer names and storing the selected index.
				property.intValue = EditorGUI.LayerField(position, label, property.intValue);
			}
			else
			{
				EditorGUI.HelpBox(position, "SingleLayerAttribute can only be used on integer fields.", MessageType.Error);
			}
		}
	}
#endif
}