using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Represents a drawable toggle (checkbox) for the Unity Editor.
	/// </summary>
	public class GuiToggle : IDraw
	{
		/// <summary>The label displayed next to the toggle.</summary>
		public string label;
		/// <summary>The width of the label.</summary>
		public int labelWidth = 80;
		/// <summary>The boolean state of the toggle (true for checked, false for unchecked).</summary>
		public bool value;
		/// <summary>The fixed width of the toggle control. If 0, it auto-sizes.</summary>
		public int valueWidth;
		/// <summary>If true, the toggle will be displayed but cannot be changed.</summary>
		public bool readOnly;
		/// <summary>The color tint of the toggle's checkmark and box.</summary>
		public Color color;
		
		/// <summary>Gets the boolean state of the toggle after the GUI interaction.</summary>
		public bool OutputValue { get; private set; }

		/// <summary>
		/// Draws the toggle control.
		/// </summary>
		/// <param name="style">Optional custom GUIStyle. If null, a default style is used.</param>
		public void Draw(GUIStyle style = null)
		{
			if (!string.IsNullOrEmpty(label))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
			}

			bool result;

			var defaultColor = GUI.color;
			if (color != default)
				GUI.color = color;

			if (style == null)
			{
				style = new GUIStyle(EditorStyles.toggle);
				style.alignment = TextAnchor.MiddleCenter;
				var normalColor = style.normal.textColor;
				normalColor.a = readOnly ? 0.5f : 1;
				style.normal.textColor = normalColor;
			}

			if (valueWidth == 0)
				result = EditorGUILayout.Toggle(value, style, GUILayout.Height(20), GUILayout.MinWidth(40));
			else
				result = EditorGUILayout.Toggle(value, style, GUILayout.Height(20), GUILayout.MinWidth(40), GUILayout.Width(valueWidth));

			if (!string.IsNullOrEmpty(label))
				EditorGUILayout.EndHorizontal();

			if (color != default)
				GUI.color = defaultColor;

			OutputValue = result;
		}
	}
}