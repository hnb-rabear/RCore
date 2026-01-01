using UnityEngine;
using UnityEditor;
using RCore.Inspector;
using RCore.Editor;
using System;

namespace RCore.Editor.Inspector
{
	/// <summary>
	/// Custom property drawer for fields with the [TimeStamp] attribute, providing a date picker and human-readable date display.
	/// </summary>
	[CustomPropertyDrawer(typeof(TimeStampAttribute))]
	public class TimeStampDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType != SerializedPropertyType.Integer)
			{
				EditorGUI.LabelField(position, label.text, "Use [TimeStamp] with int.");
				return;
			}
			
			EditorGUI.BeginProperty(position, label, property);
			
			// Draw label
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
			
			// Calculate rects
			float buttonWidth = 24f;
			float dateWidth = 130f;
			float spacing = 2f;
			
			float intFieldWidth = position.width - buttonWidth - dateWidth - (spacing * 2);
			if (intFieldWidth < 50f) 
			{
				intFieldWidth = 50f;
				// Adjust date width if space is tight, though usually Inspector has enough width.
				// For now let's just let it overflow or clip if really small.
			}

			Rect intRect = new Rect(position.x, position.y, intFieldWidth, position.height);
			Rect dateRect = new Rect(intRect.xMax + spacing, position.y, dateWidth, position.height);
			Rect buttonRect = new Rect(dateRect.xMax + spacing, position.y, buttonWidth, position.height);
			
			// Draw int field
			EditorGUI.PropertyField(intRect, property, GUIContent.none);
			
			// Draw Date Time Box
			long currentValue = property.intValue;
			string dateString = "-";
			if (currentValue != 0)
			{
				try
				{
					// Check for potential out of range before converting if needed, but int range is generally safe for DateTimeOffset
					dateString = DateTimeOffset.FromUnixTimeSeconds(currentValue).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
				}
				catch
				{
					dateString = "Invalid Date";
				}
			}
			
			// Using EditorStyles.textField to look like a box, but disabled so it's read-only
			using (new EditorGUI.DisabledScope(true))
			{
				EditorGUI.TextField(dateRect, dateString);
			}

			// Draw button
			if (GUI.Button(buttonRect, "...", EditorStyles.miniButton))
			{
				DateTime initialDate;
				
				if (currentValue == 0)
					initialDate = DateTime.Now;
				else
				{
					try
					{
						initialDate = DateTimeOffset.FromUnixTimeSeconds(currentValue).LocalDateTime;
					}
					catch
					{
						initialDate = DateTime.Now;
					}
				}

				var window = DateTimePickerWindow.ShowWindow(initialDate);
				window.onQuit = (dt) => 
				{
					// Convert Local time back to Unix Timestamp (UTC-based)
					long newUnixKey = new DateTimeOffset(dt).ToUnixTimeSeconds();
					property.intValue = (int)newUnixKey;
					property.serializedObject.ApplyModifiedProperties();
				};
			}
			
			EditorGUI.EndProperty();
		}
	}
}
