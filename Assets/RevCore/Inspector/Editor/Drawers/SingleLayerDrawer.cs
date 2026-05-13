using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
	[CustomPropertyDrawer(typeof(SingleLayerAttribute))]
	public sealed class SingleLayerDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType == SerializedPropertyType.Integer)
				property.intValue = EditorGUI.LayerField(position, label, property.intValue);
			else
				EditorGUI.HelpBox(position, "SingleLayer requires int field.", MessageType.Error);
		}
	}
}
