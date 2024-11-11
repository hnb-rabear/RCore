#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;

namespace RCore
{
	[CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
	public class SerializableDictionaryDrawer : PropertyDrawer
	{
		private const int THUMBNAIL_SIZE = 40;
		private HashSet<int> m_isTexture = new HashSet<int>();

		public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
		{
			var indentedRect = EditorGUI.IndentedRect(rect);

			void Head()
			{
				var headerRect = indentedRect;
				headerRect.height = EditorGUIUtility.singleLineHeight;

				void ExpandablePanel()
				{
					var fullHeaderRect = new Rect(headerRect);
					fullHeaderRect.x -= 17;
					fullHeaderRect.width += 34;

					if (Event.current != null && fullHeaderRect.Contains(Event.current.mousePosition))
					{
						var transparentGrey = new Color(0.4f, 0.4f, 0.4f, 0.4f);
						EditorGUI.DrawRect(fullHeaderRect, transparentGrey);
					}

					GUI.color = Color.clear;

					if (GUI.Button(new Rect(fullHeaderRect.x, fullHeaderRect.y, fullHeaderRect.width - 40, fullHeaderRect.height), ""))
					{
						prop.isExpanded = !prop.isExpanded;
					}

					GUI.color = Color.white;

					var triangleRect = rect;
					triangleRect.height = EditorGUIUtility.singleLineHeight;

					EditorGUI.Foldout(triangleRect, prop.isExpanded, "");
				}

				void DisplayName()
				{
					GUI.color = Color.white;

#if UNITY_2022_1_OR_NEWER
					var labelRect = headerRect;
					GUI.Label(labelRect, prop.displayName);
#else
                    GUI.Label(headerRect, prop.displayName);
#endif

					GUI.color = Color.white;
					GUI.skin.label.fontSize = 12;
					GUI.skin.label.fontStyle = FontStyle.Normal;
					GUI.skin.label.alignment = TextAnchor.MiddleLeft;
				}

				void DuplicatedKeysWarning()
				{
					if (Event.current != null && Event.current.type != EventType.Repaint)
					{
						return;
					}

					var hasRepeated = false;
					var repeatedKeys = new List<string>();

					for (int i = 0; i < m_dictionaryList.arraySize; i++)
					{
						var isKeyRepeatedProperty = m_dictionaryList.GetArrayElementAtIndex(i).FindPropertyRelative("keyDuplicated");
						if (isKeyRepeatedProperty.boolValue)
						{
							hasRepeated = true;
							var keyProperty = m_dictionaryList.GetArrayElementAtIndex(i).FindPropertyRelative("k");
							string keyString = GetSerializedPropertyValueAsString(keyProperty);
							repeatedKeys.Add(keyString);
						}
					}

					if (!hasRepeated)
					{
						return;
					}

					float with = GUI.skin.label.CalcSize(new GUIContent(prop.displayName)).x;
					headerRect.x += with + 24f;
					var warningRect = headerRect;
					var warningRectIcon = new Rect(headerRect.x - 18, headerRect.y, headerRect.width, headerRect.height);
					GUI.color = Color.white;
					GUI.Label(warningRectIcon, EditorGUIUtility.IconContent("console.erroricon"));
					GUI.color = new Color(1.0f, 0.443f, 0.443f);
					GUI.skin.label.fontStyle = FontStyle.Bold;
					GUI.Label(warningRect, "Duplicated keys: " + string.Join(", ", repeatedKeys));
					GUI.color = Color.white;
					GUI.skin.label.fontStyle = FontStyle.Normal;
				}

				string GetSerializedPropertyValueAsString(SerializedProperty property)
				{
					switch (property.propertyType)
					{
						case SerializedPropertyType.Integer:
							return property.intValue.ToString();
						case SerializedPropertyType.Boolean:
							return property.boolValue.ToString();
						case SerializedPropertyType.Float:
							return property.floatValue.ToString();
						case SerializedPropertyType.String:
							return property.stringValue;
						default:
							return "(Unsupported Type)";
					}
				}

				ExpandablePanel();
				DisplayName();
				DuplicatedKeysWarning();
			}

			void List()
			{
				if (!prop.isExpanded)
				{
					return;
				}

				SetupList(prop);

				float newHeight = indentedRect.height - EditorGUIUtility.singleLineHeight - 3;
				indentedRect.y += indentedRect.height - newHeight;
				indentedRect.height = newHeight;

				m_reorderableList.DoList(indentedRect);
			}

			SetupProps(prop);

			Head();
			List();
		}

		public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
		{
			SetupProps(prop);

			var height = EditorGUIUtility.singleLineHeight;
			if (prop.isExpanded)
			{
				SetupList(prop);
				height += m_reorderableList.GetHeight() + 5;
			}

			return height;
		}

		private float GetListElementHeight(int index)
		{
			var kvpProp = m_dictionaryList.GetArrayElementAtIndex(index);
			var keyProp = kvpProp.FindPropertyRelative("k");
			var valueProp = kvpProp.FindPropertyRelative("v");

			return Mathf.Max(GetPropHeight(keyProp), m_isTexture.Contains(index) ? THUMBNAIL_SIZE : GetPropHeight(valueProp));

			float GetPropHeight(SerializedProperty prop)
			{
				if (IsSingleLine(prop))
					return EditorGUI.GetPropertyHeight(prop);

				var height = 1f;

				foreach (var childProp in GetChildren(prop, false))
					height += EditorGUI.GetPropertyHeight(childProp) + 1;

				height += 10;

				return height;
			}
		}

		private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			Rect keyRect;
			Rect valueRect;
			Rect dividerRect;

			var kvpProp = m_dictionaryList.GetArrayElementAtIndex(index);
			var keyProp = kvpProp.FindPropertyRelative("k");
			var valueProp = kvpProp.FindPropertyRelative("v");

			void Draw(Rect rect, SerializedProperty prop)
			{
				if (IsSingleLine(prop))
				{
					rect.height = EditorGUIUtility.singleLineHeight;
					EditorGUI.PropertyField(rect, prop, GUIContent.none);
				}
				else
				{
					foreach (var childProp in GetChildren(prop, false))
					{
						var childPropHeight = EditorGUI.GetPropertyHeight(childProp);
						rect.height = childPropHeight;
						EditorGUI.PropertyField(rect, childProp, true);
						rect.y += childPropHeight + 2;
					}
				}
			}

			void DrawRects()
			{
				var dividerWidth = IsSingleLine(valueProp) ? 6 : 16f;
				var dividerPosition = 0.35f;

				var fullRect = rect;
				fullRect.width -= 1;
				fullRect.height -= 2;

				keyRect = fullRect;
				keyRect.width *= dividerPosition;
				keyRect.width -= dividerWidth / 2;

				valueRect = fullRect;
				valueRect.x += fullRect.width * dividerPosition;
				valueRect.width *= (1 - dividerPosition);
				valueRect.width -= dividerWidth / 2;

				dividerRect = fullRect;
				dividerRect.x += fullRect.width * dividerPosition - dividerWidth / 2;
				dividerRect.width = dividerWidth;
			}

			void Key()
			{
				Draw(keyRect, keyProp);

				if (kvpProp.FindPropertyRelative("keyDuplicated").boolValue)
				{
					GUI.Label(new Rect(keyRect.x + keyRect.width - 20, keyRect.y - 1, 20, 20), EditorGUIUtility.IconContent("console.erroricon"));
				}
			}

			void Value()
			{
				Object valueObj = null;
				if (valueProp.propertyType == SerializedPropertyType.ObjectReference)
				{
					valueObj = valueProp.objectReferenceValue;
					if (valueObj is Texture || valueObj is Sprite)
					{
						// Draw the value property as a box with the image
						if (valueObj != null)
						{
							valueRect.width -= THUMBNAIL_SIZE;
							m_isTexture.Add(index);
						}
						else m_isTexture.Remove(index);
					}
					else m_isTexture.Remove(index);
				}
				else m_isTexture.Remove(index);

				Draw(valueRect, valueProp);

				if (valueObj != null && m_isTexture.Contains(index))
				{
					// Create a rect for the image
					var imageRect = new Rect(valueRect.x + valueRect.width + 5, valueRect.y, THUMBNAIL_SIZE, THUMBNAIL_SIZE);
					if (valueObj is Texture)
						valueProp.objectReferenceValue = EditorGUI.ObjectField(imageRect, valueObj, typeof(Texture), allowSceneObjects: false);
					else if (valueObj is Sprite)
						valueProp.objectReferenceValue = EditorGUI.ObjectField(imageRect, valueObj, typeof(Sprite), allowSceneObjects: false);
				}
			}

			void Divider()
			{
				EditorGUIUtility.AddCursorRect(dividerRect, MouseCursor.ResizeHorizontal);

				if (Event.current == null || rect.Contains(Event.current.mousePosition) == false)
				{
					return;
				}

				if (Event.current != null && dividerRect.Contains(Event.current.mousePosition))
				{
					if (Event.current.type == EventType.MouseDown)
					{
						m_isDividerDragged = true;
					}
					else if (Event.current.type == EventType.MouseUp
					         || Event.current.type == EventType.MouseMove
					         || Event.current.type == EventType.MouseLeaveWindow)
					{
						m_isDividerDragged = false;
					}
				}

				if (m_isDividerDragged && Event.current != null && Event.current.type == EventType.MouseDrag)
				{
					m_dividerPosProp.floatValue = Mathf.Clamp(m_dividerPosProp.floatValue + Event.current.delta.x / rect.width, .2f, .8f);
				}
			}

			DrawRects();
			Key();
			Value();
			Divider();
		}

		private void ShowDictIsEmptyMessage(Rect rect)
		{
			GUI.Label(rect, "Empty");
		}

		private IEnumerable<SerializedProperty> GetChildren(SerializedProperty prop, bool enterVisibleGrandchildren)
		{
			prop = prop.Copy();

			var startPath = prop.propertyPath;

			var enterVisibleChildren = true;

			while (prop.NextVisible(enterVisibleChildren) && prop.propertyPath.StartsWith(startPath))
			{
				yield return prop;
				enterVisibleChildren = enterVisibleGrandchildren;
			}
		}

		private bool IsSingleLine(SerializedProperty prop)
		{
			return prop.propertyType != SerializedPropertyType.Generic || prop.hasVisibleChildren == false;
		}

		private void SetupList(SerializedProperty prop)
		{
			if (m_reorderableList != null)
			{
				return;
			}

			SetupProps(prop);

			this.m_reorderableList = new ReorderableList(m_dictionaryList.serializedObject, m_dictionaryList, true, false, true, true);
			this.m_reorderableList.drawElementCallback = DrawListElement;
			this.m_reorderableList.elementHeightCallback = GetListElementHeight;
			this.m_reorderableList.drawNoneElementCallback = ShowDictIsEmptyMessage;
		}

		private ReorderableList m_reorderableList;
		private bool m_isDividerDragged;

		public void SetupProps(SerializedProperty prop)
		{
			if (this.m_property != null)
			{
				return;
			}

			this.m_property = prop;
			this.m_dictionaryList = prop.FindPropertyRelative("keyValues");
			this.m_dividerPosProp = prop.FindPropertyRelative("dividerPos");
		}

		private SerializedProperty m_property;
		private SerializedProperty m_dictionaryList;
		private SerializedProperty m_dividerPosProp;
	}
}
#endif