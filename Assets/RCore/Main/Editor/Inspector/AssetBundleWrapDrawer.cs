/***
 * Author HNB-RaBear - 2024
 */

using UnityEngine;
using UnityEditor;

namespace RCore.Editor.Inspector
{
#if ADDRESSABLES
	/// <summary>
	/// Custom property drawer for AssetBundleWrap, displaying both the reference and its parent group properly.
	/// </summary>
	[CustomPropertyDrawer(typeof(AssetBundleWrap<>))]
	public class AssetBundleWrapDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Ensure the properties exist
			var referenceProperty = property.FindPropertyRelative("reference");
			var parentProperty = property.FindPropertyRelative("parent");

			// Check if the property is in a list
			bool isInList = property.IsInList();
			bool isFirstElement = isInList && property.IsFirstElementOfList();

			// Adjust the position for the header if it's the first element
			float headerHeight = 0;
			if (isFirstElement)
			{
				// Draw headers for the list
				headerHeight = EditorGUIUtility.singleLineHeight;
				float columnLabelWidth = position.width * 0.5f;
				EditorGUI.LabelField(new Rect(position.x, position.y, columnLabelWidth, headerHeight), referenceProperty.name, EditorStyles.boldLabel);
				EditorGUI.LabelField(new Rect(position.x + columnLabelWidth + 15, position.y, columnLabelWidth, headerHeight), parentProperty.name, EditorStyles.boldLabel);
			}

			// Draw the field name if it is not in a list
			if (!isInList)
			{
				// Determine the editor theme and select the appropriate border color
				var borderColor = Color.black * 0.1f;
				// Draw a box
				EditorGUI.DrawRect(new Rect(position.x, position.y, position.width, position.height), borderColor);
				
				headerHeight = EditorGUIUtility.singleLineHeight;
				EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), label);
			}

			// Define specific label widths
			float referenceLabelWidth = 70;
			float parentLabelWidth = 50;
			float fieldWidth = (position.width - referenceLabelWidth - parentLabelWidth - 15) / 2;
			float fieldHeight = EditorGUIUtility.singleLineHeight;
			bool displayName = !isInList;
			// Draw the reference property with label
			if (displayName)
			{
				var labelRect = new Rect(position.x, position.y + headerHeight, referenceLabelWidth, fieldHeight);
				EditorGUI.LabelField(labelRect, new GUIContent(referenceProperty.displayName), EditorStyles.miniBoldLabel);
			}
			var referencePropertyRect = new Rect(position.x + (displayName ? referenceLabelWidth : 0), position.y + headerHeight, fieldWidth + (displayName ? 0 : referenceLabelWidth), fieldHeight);
			EditorGUI.PropertyField(referencePropertyRect, referenceProperty, GUIContent.none);

			// Draw the parent property with label
			if (displayName)
			{
				var labelRect = new Rect(position.x + referenceLabelWidth + fieldWidth + 5, position.y + headerHeight, parentLabelWidth, fieldHeight);
				EditorGUI.LabelField(labelRect, new GUIContent(parentProperty.displayName), EditorStyles.miniBoldLabel);
			}
			var parentPropertyRect = new Rect(position.x + referenceLabelWidth + fieldWidth + parentLabelWidth + 10 - (displayName ? 0 : parentLabelWidth), position.y + headerHeight, fieldWidth + (displayName ? 0 : parentLabelWidth), fieldHeight);
			EditorGUI.PropertyField(parentPropertyRect, parentProperty, GUIContent.none);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!property.IsInList())
				return EditorGUIUtility.singleLineHeight * 2;
			bool isFirstElement = property.IsFirstElementOfList();
			return isFirstElement ? EditorGUIUtility.singleLineHeight * 2 : EditorGUIUtility.singleLineHeight;
		}
	}
#endif
}