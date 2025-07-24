using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Represents a drawable dropdown list (popup) for string selections in the Unity Editor.
	/// </summary>
	public class GuiDropdownListString : IDraw
	{
		/// <summary>The label displayed next to the dropdown.</summary>
		public string label;
		/// <summary>The width of the label.</summary>
		public int labelWidth = 80;
		/// <summary>The array of string options to display in the dropdown.</summary>
		public string[] selections;
		/// <summary>The currently selected string value.</summary>
		public string value;
		/// <summary>The fixed width of the dropdown control. If 0, it auto-sizes.</summary>
		public int valueWidth;
		
		/// <summary>Gets the selected string value after the GUI interaction.</summary>
		public string OutputValue { get; private set; }

		/// <summary>
		/// Draws the string dropdown list.
		/// </summary>
		/// <param name="style">This parameter is ignored for this element.</param>
		public void Draw(GUIStyle style = null)
		{
			if (selections.Length == 0)
			{
				OutputValue = "";
				return;
			}

			if (!string.IsNullOrEmpty(label))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
			}

			int index = 0;

			for (int i = 0; i < selections.Length; i++)
			{
				if (value == selections[i])
					index = i;
			}

			if (valueWidth != 0)
				index = EditorGUILayout.Popup(index, selections, "DropDown", GUILayout.Width(valueWidth));
			else
				index = EditorGUILayout.Popup(index, selections, "DropDown");

			if (!string.IsNullOrEmpty(label))
				EditorGUILayout.EndHorizontal();

			OutputValue = selections[index] == null ? "" : selections[index];
		}
	}
}