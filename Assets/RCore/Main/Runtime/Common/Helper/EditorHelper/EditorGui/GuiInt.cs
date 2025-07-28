#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Represents a drawable integer input field for the Unity Editor.
	/// </summary>
	public class GuiInt : IDraw
	{
		/// <summary>The label displayed next to the integer field.</summary>
		public string label;
		/// <summary>The width of the label.</summary>
		public int labelWidth = 80;
		/// <summary>The fixed width of the input field. If 0, it auto-sizes.</summary>
		public int valueWidth;
		/// <summary>The integer value to display and edit.</summary>
		public int value;
		/// <summary>The minimum allowed value (inclusive). Used with a slider if max is also set.</summary>
		public int min;
		/// <summary>The maximum allowed value (inclusive). Used with a slider if min is also set.</summary>
		public int max;
		/// <summary>If true, the field will be displayed but cannot be edited.</summary>
		public bool readOnly;
		
		/// <summary>Gets the integer value after the GUI interaction.</summary>
		public int OutputValue { get; private set; }

		/// <summary>
		/// Draws the integer field.
		/// </summary>
		/// <param name="style">Optional custom GUIStyle. If null, a default style is used.</param>
		public void Draw(GUIStyle style = null)
		{
			if (!string.IsNullOrEmpty(label))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
			}

			if (style == null)
			{
				style = new GUIStyle(EditorStyles.textField)
				{
					alignment = TextAnchor.MiddleLeft,
					margin = new RectOffset(0, 0, 4, 4)
				};
				var normalColor = style.normal.textColor;
				normalColor.a = readOnly ? 0.5f : 1;
				style.normal.textColor = normalColor;
			}

			int result;
			if (valueWidth == 0)
			{
				if (min == max)
					result = EditorGUILayout.IntField(value, style, GUILayout.Height(20), GUILayout.MinWidth(40));
				else
					result = EditorGUILayout.IntField(value, style, GUILayout.Height(20), GUILayout.MinWidth(40));
			}
			else
			{
				if (min == max)
					result = EditorGUILayout.IntField(value, style, GUILayout.Height(20), GUILayout.MinWidth(40), GUILayout.Width(valueWidth));
				else
					result = EditorGUILayout.IntField(value, style, GUILayout.Height(20), GUILayout.MinWidth(40), GUILayout.Width(valueWidth));
			}

			if (!string.IsNullOrEmpty(label))
				EditorGUILayout.EndHorizontal();

			OutputValue = result;
		}
	}
}
#endif