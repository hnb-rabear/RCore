using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Inspector
{
	public class SpritePreviewAttribute : PropertyAttribute
	{
		public float width, height;

		public SpritePreviewAttribute(float width = 36, float height = 36)
		{
			this.width = width;
			this.height = height;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(SpritePreviewAttribute))]
	public class SpritePreviewDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Get the attribute
			var spritePreview = (SpritePreviewAttribute)attribute;

			// Begin the property
			EditorGUI.BeginProperty(position, label, property);

			// Draw the label
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			// Check if the property type is correct
			if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is Sprite sprite)
			{
				// Calculate the rect for the sprite preview
				var spriteRect = new Rect(position.x, position.y, spritePreview.width, spritePreview.height);
				var fieldRect = new Rect(position.x + spritePreview.width + 5, position.y, position.width - spritePreview.width - 5, position.height);

				// Draw the sprite
				if (sprite != null)
					property.objectReferenceValue = EditorGUI.ObjectField(spriteRect, sprite, typeof(Sprite), allowSceneObjects: false);

				// Draw the property field
				EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
			}
			else
			{
				// If not a Sprite, draw the default property field
				EditorGUI.PropertyField(position, property, label);
			}

			// End the property
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			SpritePreviewAttribute spritePreview = (SpritePreviewAttribute)attribute;
			if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is Sprite sprite && sprite != null)
			{
				return Mathf.Max(base.GetPropertyHeight(property, label), spritePreview.height);
			}
			return base.GetPropertyHeight(property, label);
		}
	}
#endif
}