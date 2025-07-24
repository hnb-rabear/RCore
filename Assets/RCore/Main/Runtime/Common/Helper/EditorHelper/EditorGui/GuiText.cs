using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Represents a drawable text field or text area for the Unity Editor.
	/// </summary>
	public class GuiText : IDraw
	{
		/// <summary>The label displayed next to the text field.</summary>
		public string label;
		/// <summary>The width of the label.</summary>
		public int labelWidth = 80;
		/// <summary>The fixed width of the input field. If 0, it auto-sizes.</summary>
		public int valueWidth;
		/// <summary>The string value to display and edit.</summary>
		public string value;
		/// <summary>If true, the field will be displayed but cannot be edited.</summary>
		public bool readOnly;
		/// <summary>If true, renders a multi-line text area instead of a single-line text field.</summary>
		public bool textArea;
		/// <summary>The color of the text in the input field.</summary>
		public Color color;
		
		/// <summary>Gets the string value after the GUI interaction.</summary>
		public string OutputValue { get; private set; }

		/// <summary>
		/// Draws the text field or text area.
		/// </summary>
		/// <param name="style">Optional custom GUIStyle. If null, a default style is used.</param>
		public void Draw(GUIStyle style = null)
		{
			if (!string.IsNullOrEmpty(label))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
			}

			value ??= "";

			if (style == null)
			{
				style = new GUIStyle(EditorStyles.textField)
				{
					alignment = TextAnchor.MiddleLeft,
					margin = new RectOffset(0, 0, 4, 4)
				};
				var normalColor = style.normal.textColor;
				if (color != default)
					normalColor = color;
				style.normal.textColor = normalColor;
			}

			if (readOnly)
				GUI.enabled = false;
			string str;
			if (valueWidth == 0)
			{
				if (textArea)
					str = EditorGUILayout.TextArea(value, style, GUILayout.MinWidth(40), GUILayout.Height(80));
				else
					str = EditorGUILayout.TextField(value, style, GUILayout.MinWidth(40));
			}
			else
			{
				if (textArea)
					str = EditorGUILayout.TextArea(value, style, GUILayout.MinWidth(40), GUILayout.Width(valueWidth), GUILayout.Height(80));
				else
					str = EditorGUILayout.TextField(value, style, GUILayout.MinWidth(40), GUILayout.Width(valueWidth));
			}
			if (readOnly)
				GUI.enabled = true;

			if (!string.IsNullOrEmpty(label))
				EditorGUILayout.EndHorizontal();

			OutputValue = str;
		}
	}
}