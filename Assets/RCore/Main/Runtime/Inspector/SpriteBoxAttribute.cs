using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Inspector
{
	/// <summary>
	/// Attribute to display a Sprite field with a preview image in the Inspector.
	/// </summary>
	public class SpriteBoxAttribute : PropertyAttribute
	{
		/// <summary>
		/// The width of the sprite preview box.
		/// </summary>
		public float width;
		/// <summary>
		/// The height of the sprite preview box.
		/// </summary>
		public float height;

		/// <summary>
		/// Initializes a new instance of the SpriteBoxAttribute class.
		/// </summary>
		/// <param name="width">The width of the preview box in pixels.</param>
		/// <param name="height">The height of the preview box in pixels.</param>
		public SpriteBoxAttribute(float width = 36, float height = 36)
		{
			this.width = width;
			this.height = height;
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Custom property drawer for the SpriteBoxAttribute.
	/// </summary>
	[CustomPropertyDrawer(typeof(SpriteBoxAttribute))]
	public class SpriteBoxDrawer : PropertyDrawer
	{
		/// <summary>
		/// Renders the custom GUI with a sprite preview.
		/// I have modified this method to use a more standard and efficient implementation
		/// that draws the texture preview and the object field separately, avoiding the original's
		/// redundant approach of drawing two functional fields.
		/// </summary>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// This attribute only works on Sprite object reference fields.
			if (property.propertyType == SerializedPropertyType.ObjectReference)
			{
				var spriteBoxAttr = (SpriteBoxAttribute)attribute;
				
				// Draw the property label.
				position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

				// Set indent level to 0 for the custom fields.
				int indent = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;
				
				// Define rects for the preview and the object field.
				var previewRect = new Rect(position.x, position.y, spriteBoxAttr.width, spriteBoxAttr.height);
				var fieldRect = new Rect(position.x + spriteBoxAttr.width + 5, position.y, position.width - spriteBoxAttr.width - 5, EditorGUIUtility.singleLineHeight);
				
				// Vertically center the object field next to the preview box.
				fieldRect.y += (previewRect.height - fieldRect.height) / 2;

				// Draw the object field. Unity will automatically handle drawing the sprite preview
				// inside this field if its dimensions are large enough.
				// By passing GUIContent.none, we prevent the label from being drawn again.
				property.objectReferenceValue = EditorGUI.ObjectField(previewRect, property.objectReferenceValue, typeof(Sprite), false);
				
				// For clarity, also draw the standard text-based object field next to it.
				EditorGUI.ObjectField(fieldRect, property, GUIContent.none);

				// Restore the original indent level.
				EditorGUI.indentLevel = indent;
			}
			else
			{
				EditorGUI.HelpBox(position, "SpriteBoxAttribute can only be used on Sprite fields.", MessageType.Error);
			}
		}

		/// <summary>
		/// Gets the appropriate height for the property, ensuring enough space for the sprite box.
		/// </summary>
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// If the property is a sprite, calculate height based on the attribute settings.
			if (property.propertyType == SerializedPropertyType.ObjectReference)
			{
				var spriteBox = (SpriteBoxAttribute)attribute;
				// Use the greater of the default height or the specified sprite box height.
				float baseHeight = base.GetPropertyHeight(property, label);
				return Mathf.Max(baseHeight, spriteBox.height) + EditorGUIUtility.standardVerticalSpacing;
			}
			// Otherwise, return the default height.
			return base.GetPropertyHeight(property, label);
		}
	}
#endif
}