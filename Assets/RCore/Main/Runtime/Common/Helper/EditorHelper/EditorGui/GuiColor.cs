using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Represents a drawable color field for the Unity Editor.
	/// </summary>
	public class GuiColor : IDraw
	{
		/// <summary>The label displayed next to the color field.</summary>
		public string label;
		/// <summary>The width of the label.</summary>
		public int labelWidth = 80;
		/// <summary>The width of the color field. If 0, it auto-sizes.</summary>
		public int valueWidth;
		/// <summary>The initial color value to display.</summary>
		public Color value;
		/// <summary>The color value returned after the GUI interaction.</summary>
		public Color outputValue;

		/// <summary>
		/// Draws the color field.
		/// </summary>
		/// <param name="style">This parameter is ignored for this element.</param>
		public void Draw(GUIStyle style = null)
		{
			if (!string.IsNullOrEmpty(label))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
			}

			Color color;
			if (valueWidth == 0)
				color = EditorGUILayout.ColorField(value, GUILayout.Height(16), GUILayout.MinWidth(40));
			else
				color = EditorGUILayout.ColorField(value, GUILayout.Height(20), GUILayout.MinWidth(40),
					GUILayout.Width(valueWidth));
			outputValue = color;

			if (!string.IsNullOrEmpty(label))
				EditorGUILayout.EndHorizontal();
		}
	}
}