using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Represents a drawable dropdown list (popup) for integer selections in the Unity Editor.
	/// </summary>
	public class GuiDropdownListInt : IDraw
	{
		/// <summary>The label displayed next to the dropdown.</summary>
		public string label;
		/// <summary>The width of the label.</summary>
		public int labelWidth = 80;
		/// <summary>The array of integer options to display in the dropdown.</summary>
		public int[] selections;
		/// <summary>The currently selected integer value.</summary>
		public int value;
		/// <summary>The fixed width of the dropdown control. If 0, it auto-sizes.</summary>
		public int valueWidth;
		
		/// <summary>Gets the selected integer value after the GUI interaction.</summary>
		public int OutputValue { get; private set; }

		/// <summary>
		/// Draws the integer dropdown list.
		/// </summary>
		/// <param name="style">This parameter is ignored for this element.</param>
		public void Draw(GUIStyle style = null)
		{
			if (selections.Length == 0)
			{
				OutputValue = -1;
				return;
			}

			if (!string.IsNullOrEmpty(label))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
			}

			int index = 0;

			string[] selectionsStr = new string[selections.Length];
			for (int i = 0; i < selections.Length; i++)
			{
				if (value == selections[i])
					index = i;
				selectionsStr[i] = selections[i].ToString();
			}

			if (valueWidth != 0)
				index = EditorGUILayout.Popup(index, selectionsStr, GUILayout.Width(valueWidth));
			else
				index = EditorGUILayout.Popup(index, selectionsStr);

			if (!string.IsNullOrEmpty(label))
				EditorGUILayout.EndHorizontal();

			OutputValue = selections[index];
		}
	}
}