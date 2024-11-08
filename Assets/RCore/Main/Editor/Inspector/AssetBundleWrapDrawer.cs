using RCore;
using UnityEngine;
using UnityEditor;

namespace RCore.Editor
{
	[CustomPropertyDrawer(typeof(AssetBundleWrap<>))]
	public class AssetBundleWrapDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Ensure the properties exist
			var referenceProperty = property.FindPropertyRelative("reference");
			var parentProperty = property.FindPropertyRelative("parent");

			// Define specific label widths
			float referenceLabelWidth = 80;
			float parentLabelWidth = 60;
			float fieldWidth = (position.width - referenceLabelWidth - parentLabelWidth - 15) / 2;
			float fieldHeight = EditorGUIUtility.singleLineHeight;

			// Draw the reference property with label
			EditorGUI.LabelField(new Rect(position.x, position.y, referenceLabelWidth, fieldHeight), new GUIContent(referenceProperty.displayName));
			EditorGUI.PropertyField(
				new Rect(position.x + referenceLabelWidth, position.y, fieldWidth, fieldHeight),
				referenceProperty,
				GUIContent.none
			);

			// Draw the parent property with label
			EditorGUI.LabelField(new Rect(position.x + referenceLabelWidth + fieldWidth + 5, position.y, parentLabelWidth, fieldHeight), new GUIContent(parentProperty.displayName));
			EditorGUI.PropertyField(
				new Rect(position.x + referenceLabelWidth + fieldWidth + parentLabelWidth + 10, position.y, fieldWidth, fieldHeight),
				parentProperty,
				GUIContent.none
			);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}
	}
}