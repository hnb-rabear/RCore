using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Inspector
{
	public class SpriteBoxAttribute : PropertyAttribute
	{
		public float width, height;

		public SpriteBoxAttribute(float width = 36, float height = 36)
		{
			this.width = width;
			this.height = height;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(SpriteBoxAttribute))]
	public class SpriteBoxDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Check if the property type is correct
			if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is Sprite sprite)
			{
				EditorGUI.BeginProperty(position, label, property);

				position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
				var spritePreview = (SpriteBoxAttribute)attribute;
				var spriteRect = new Rect(position.x, position.y, spritePreview.width, spritePreview.height);
				var fieldRect = new Rect(position.x + spritePreview.width + 5, position.y, position.width - spritePreview.width - 5, position.height);

				if (sprite != null)
					property.objectReferenceValue = EditorGUI.ObjectField(spriteRect, sprite, typeof(Sprite), allowSceneObjects: false);

				EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
				EditorGUI.EndProperty();
			}
			else
			{
				EditorGUI.PropertyField(position, property, label);
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var spriteBox = (SpriteBoxAttribute)attribute;
			if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is Sprite sprite && sprite != null)
			{
				return Mathf.Max(base.GetPropertyHeight(property, label), spriteBox.height);
			}
			return base.GetPropertyHeight(property, label);
		}
	}
#endif
}