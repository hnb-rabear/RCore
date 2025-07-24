using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Represents a drawable Object field for assigning assets or scene objects in the Unity Editor.
	/// </summary>
	/// <typeparam name="T">The type of Object to accept.</typeparam>
	public class GuiObject<T> : IDraw
	{
		/// <summary>The Object value to display.</summary>
		public Object value;
		/// <summary>The label displayed next to the object field.</summary>
		public string label;
		/// <summary>The width of the label.</summary>
		public int labelWidth = 80;
		/// <summary>The fixed width of the object field. If 0, it auto-sizes.</summary>
		public int valueWidth;
		/// <summary>If true, displays the field as a square box, suitable for textures or sprites.</summary>
		public bool showAsBox;
		
		/// <summary>Gets the assigned Object after the GUI interaction.</summary>
		public Object OutputValue { get; private set; }

		/// <summary>
		/// Draws the Object field.
		/// </summary>
		/// <param name="style">This parameter is ignored for this element.</param>
		public void Draw(GUIStyle style = null)
		{
			if (!string.IsNullOrEmpty(label))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
			}

			if (valueWidth == 0 && showAsBox)
				valueWidth = 34;

			Object result;

			if (showAsBox)
				result = EditorGUILayout.ObjectField(value, typeof(T), true, GUILayout.Width(valueWidth), GUILayout.Height(valueWidth));
			else
			{
				if (valueWidth == 0)
					result = EditorGUILayout.ObjectField(value, typeof(T), true);
				else
					result = EditorGUILayout.ObjectField(value, typeof(T), true, GUILayout.Width(valueWidth));
			}

			if (!string.IsNullOrEmpty(label))
				EditorGUILayout.EndHorizontal();

			OutputValue = result;
		}
	}
}