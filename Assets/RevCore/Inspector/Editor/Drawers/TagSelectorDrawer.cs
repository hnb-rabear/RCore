using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace RevCore.Editor
{
	[CustomPropertyDrawer(typeof(TagSelectorAttribute))]
	public sealed class TagSelectorDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType != SerializedPropertyType.String)
			{
				EditorGUI.HelpBox(position, "TagSelector requires string field.", MessageType.Error);
				return;
			}

			var tagAttr = (TagSelectorAttribute)attribute;
			EditorGUI.BeginProperty(position, label, property);

			if (tagAttr.UseDefaultTagFieldDrawer)
			{
				property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
			}
			else
			{
				string[] tags = InternalEditorUtility.tags;
				string[] options = new string[tags.Length + 1];
				options[0] = "<NoTag>";
				System.Array.Copy(tags, 0, options, 1, tags.Length);

				int index = 0;
				for (int i = 1; i < options.Length; i++)
				{
					if (options[i] == property.stringValue)
					{
						index = i;
						break;
					}
				}

				int newIndex = EditorGUI.Popup(position, label.text, index, options);
				property.stringValue = newIndex == 0 ? "" : options[newIndex];
			}

			EditorGUI.EndProperty();
		}
	}
}
