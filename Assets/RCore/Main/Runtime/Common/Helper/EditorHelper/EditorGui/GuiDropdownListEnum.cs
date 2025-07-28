#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Represents a drawable dropdown list (popup) for an Enum type in the Unity Editor.
	/// </summary>
	/// <typeparam name="T">The Enum type to display in the dropdown.</typeparam>
	public class GuiDropdownListEnum<T> : IDraw
	{
		/// <summary>The label displayed next to the dropdown.</summary>
		public string label;
		/// <summary>The width of the label.</summary>
		public int labelWidth;
		/// <summary>The currently selected enum value.</summary>
		public T value;
		/// <summary>The fixed width of the dropdown control. If 0, it auto-sizes.</summary>
		public int valueWidth;
		
		/// <summary>Gets the selected enum value after the GUI interaction.</summary>
		public T OutputValue { get; private set; }

		/// <summary>
		/// Draws the enum dropdown list.
		/// </summary>
		/// <param name="style">This parameter is ignored for this element.</param>
		public void Draw(GUIStyle style = null)
		{
			if (!typeof(T).IsEnum) throw new ArgumentException("T must be an enumerated type");

			if (!string.IsNullOrEmpty(label))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
			}

			var enumValues = Enum.GetValues(typeof(T));
			string[] selections = new string[enumValues.Length];

			int i = 0;
			foreach (T item in enumValues)
			{
				selections[i] = item.ToString();
				i++;
			}

			int index = 0;
			for (i = 0; i < selections.Length; i++)
			{
				if (value.ToString() == selections[i])
				{
					index = i;
				}
			}

			if (valueWidth != 0)
				index = EditorGUILayout.Popup(index, selections, GUILayout.Width(valueWidth));
			else
				index = EditorGUILayout.Popup(index, selections);

			if (!string.IsNullOrEmpty(label))
				EditorGUILayout.EndHorizontal();

			i = 0;
			foreach (T item in enumValues)
			{
				if (i == index)
				{
					OutputValue = item;
					return;
				}

				i++;
			}

			OutputValue = default;
		}
	}
}
#endif