using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
using UnityEngine;

namespace RCore.Inspector
{
	/// <summary>
	/// Attribute to make a string field a tag selector dropdown in the Inspector.
	/// </summary>
	public class TagSelectorAttribute : PropertyAttribute
	{
		/// <summary>
		/// If true, the default Unity tag field will be used.
		/// If false, a custom popup with a "<NoTag>" option will be used.
		/// </summary>
		public bool UseDefaultTagFieldDrawer = false;
	}

#if UNITY_EDITOR
	/// <summary>
	/// Custom property drawer for the TagSelectorAttribute.
	/// </summary>
	[CustomPropertyDrawer(typeof(TagSelectorAttribute))]
	public class TagSelectorPropertyDrawer : PropertyDrawer
	{
		/// <summary>
		/// Renders the custom GUI for the tag selector.
		/// </summary>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Ensure the property is a string, otherwise, draw the default field.
			if (property.propertyType == SerializedPropertyType.String)
			{
				EditorGUI.BeginProperty(position, label, property);

				var attrib = this.attribute as TagSelectorAttribute;

				// If the attribute is configured to use the default drawer, use EditorGUI.TagField.
				if (attrib.UseDefaultTagFieldDrawer)
				{
					property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
				}
				else
				{
					// --- Custom Tag Popup Implementation ---

					// Generate a list of all available tags and add a custom "<NoTag>" option at the beginning.
					List<string> tagList = new List<string> { "<NoTag>" };
					tagList.AddRange(InternalEditorUtility.tags);
					string propertyString = property.stringValue;
					int index = 0; // Default to "<NoTag>"

					// If the tag is not empty, find its index in the list.
					if (!string.IsNullOrEmpty(propertyString))
					{
						for (int i = 1; i < tagList.Count; i++)
						{
							if (tagList[i] == propertyString)
							{
								index = i;
								break;
							}
						}
					}

					// Draw the popup dropdown menu.
					index = EditorGUI.Popup(position, label.text, index, tagList.ToArray());

					// Update the property's string value based on the user's selection.
					if (index == 0)
					{
						// If "<NoTag>" was selected, set the string to empty.
						property.stringValue = "";
					}
					else if (index > 0)
					{
						// Otherwise, set it to the selected tag.
						property.stringValue = tagList[index];
					}
				}

				EditorGUI.EndProperty();
			}
			else
			{
				// If the field is not a string, draw a help box to inform the user.
				EditorGUI.HelpBox(position, "TagSelectorAttribute can only be used on string fields.", MessageType.Error);
			}
		}
	}
#endif
}