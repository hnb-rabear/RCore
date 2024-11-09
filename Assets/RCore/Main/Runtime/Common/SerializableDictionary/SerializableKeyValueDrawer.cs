/***
 * Author RaBear - HNB - 2024
 */
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	[CustomPropertyDrawer(typeof(SerializableKeyValue<,>))]
	public class SerializableKeyValueDrawer : PropertyDrawer
	{
		private const float THUMBNAIL_SIZE = 40;
		private bool m_isTexture;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Get the key and value properties
			var keyProperty = property.FindPropertyRelative("k");
			var valueProperty = property.FindPropertyRelative("v");

			// Check if the property is in a list
			bool isInList = property.IsInList();
			bool isFirstElement = isInList && property.IsFirstElementOfList();

			float headerHeight = 0;
			if (isFirstElement)
				headerHeight = EditorGUIUtility.singleLineHeight;

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

			// Calculate rects
			float kWidth = position.width * 0.5f - THUMBNAIL_SIZE;
			float vWidth = position.width * 0.5f - 5 + THUMBNAIL_SIZE;
			float fieldHeight = EditorGUIUtility.singleLineHeight;

			// Display name of Columns
			if (isFirstElement)
			{
				EditorGUI.LabelField(new Rect(position.x, position.y, kWidth, headerHeight), "Key", EditorStyles.boldLabel);
				EditorGUI.LabelField(new Rect(position.x + kWidth + 5, position.y, vWidth, headerHeight), "Value", EditorStyles.boldLabel);
			}

			var keyRect = new Rect(position.x, position.y + headerHeight, kWidth, fieldHeight);
			var valueRect = new Rect(position.x + kWidth + 5, position.y + headerHeight, vWidth, fieldHeight);

			// Check if the value property is of type Texture or Sprite
			if (valueProperty.propertyType == SerializedPropertyType.ObjectReference)
			{
				var valueObj = valueProperty.objectReferenceValue;
				if (valueObj is Texture || valueObj is Sprite)
				{
					valueRect.width -= THUMBNAIL_SIZE + 5;
					m_isTexture = true;
				}
			}

			// Draw the key property field
			EditorGUI.PropertyField(keyRect, keyProperty, GUIContent.none);

			// Check if the value property is of type Texture or Sprite
			if (valueProperty.propertyType == SerializedPropertyType.ObjectReference)
			{
				var valueObj = valueProperty.objectReferenceValue;
				if (valueObj is Texture || valueObj is Sprite)
				{
					// Draw the value property as a box with the image
					EditorGUI.ObjectField(valueRect, valueProperty, GUIContent.none);

					if (valueObj != null)
					{
						// Create a rect for the image with fixed size 40x40
						var imageRect = new Rect(valueRect.x + valueRect.width + 5, valueRect.y, THUMBNAIL_SIZE, THUMBNAIL_SIZE);

						// Draw the texture or sprite thumbnail
						var texture = valueObj is Sprite sprite ? sprite.texture : valueObj as Texture;
						if (texture != null)
							EditorGUI.ObjectField(imageRect, valueObj, typeof(Texture), allowSceneObjects: false);
					}
				}
				else
				{
					EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);
				}
			}
			else
			{
				EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float lineHeight = EditorGUIUtility.singleLineHeight * (m_isTexture ? 2 : 1);
			if (!property.IsInList())
				return lineHeight * 2;
			bool isFirstElement = property.IsFirstElementOfList();
			return isFirstElement ? lineHeight + EditorGUIUtility.singleLineHeight : lineHeight;
		}
	}
}
#endif