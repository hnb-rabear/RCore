#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCore.Editor
{
	/// <summary>
	/// Provides a collection of static helper methods that simplify the creation of common GUI controls
	/// for custom Unity Editor windows and inspectors. This class acts as a higher-level wrapper around
	/// the `Gui...` classes and `EditorGUILayout` functions.
	/// </summary>
	public static class EditorGui
	{
		/// <summary>
		/// Stores the last used directory path in EditorPrefs to provide a better user experience for file dialogs.
		/// </summary>
		private static string LastOpenedDirectory { get => EditorPrefs.GetString("LastOpenedDirectory"); set => EditorPrefs.SetString("LastOpenedDirectory", value); }

		/// <summary>
		/// Draws a GUILayout button.
		/// </summary>
		/// <param name="label">The text displayed on the button.</param>
		/// <param name="width">An optional fixed width for the button.</param>
		/// <param name="height">An optional fixed height for the button.</param>
		/// <returns>True if the button was clicked in this GUI frame.</returns>
		public static bool Button(string label, int width = 0, int height = 0)
		{
			var button = new GuiButton { label = label, width = width, height = height };
			button.Draw();
			return button.IsPressed;
		}

		/// <summary>
		/// Draws a GUILayout button with a custom background color.
		/// </summary>
		/// <param name="label">The text displayed on the button.</param>
		/// <param name="color">The background color tint for the button.</param>
		/// <returns>True if the button was clicked in this GUI frame.</returns>
		public static bool Button(string label, Color color, int width = 0, int height = 0)
		{
			var button = new GuiButton { label = label, width = width, color = color, height = height };
			button.Draw();
			return button.IsPressed;
		}

		/// <summary>
		/// Draws a GUILayout button with a label, an icon, and a custom color.
		/// </summary>
		/// <param name="label">The text displayed on the button.</param>
		/// <param name="icon">The icon to display on the button.</param>
		/// <param name="color">The background color tint for the button.</param>
		/// <returns>True if the button was clicked in this GUI frame.</returns>
		public static bool Button(string label, Texture2D icon, Color color = default, int width = 0, int height = 0)
		{
			var button = new GuiButton { label = label, icon = icon, color = color, width = width, height = height };
			button.Draw();
			return button.IsPressed;
		}

		/// <summary>
		/// Displays a modal confirmation dialog box.
		/// </summary>
		/// <param name="message">The message to display in the dialog.</param>
		/// <param name="yes">The text for the confirmation button.</param>
		/// <param name="no">The text for the cancellation button.</param>
		/// <returns>True if the user clicked the "yes" button; otherwise, false.</returns>
		public static bool ConfirmPopup(string message = "Are you sure?", string yes = null, string no = null)
		{
			if (string.IsNullOrEmpty(yes))
				yes = "Yes";
			if (string.IsNullOrEmpty(no))
				no = "No";
			return EditorUtility.DisplayDialog("Confirm Action", message, yes, no);
		}

		/// <summary>
		/// Draws a text field combined with a "..." button that opens a folder selection dialog.
		/// </summary>
		/// <param name="defaultPath">The initial path displayed in the text field.</param>
		/// <param name="label">The label for the field.</param>
		/// <param name="labelWidth">The width of the label.</param>
		/// <param name="pFormatToUnityPath">If true, converts the selected absolute path to a Unity-relative path (starting with "Assets/").</param>
		/// <returns>The selected folder path.</returns>
		public static string FolderField(string defaultPath, string label, int labelWidth = 0, bool pFormatToUnityPath = true)
		{
			EditorGUILayout.BeginHorizontal();
			var newPath = TextField(defaultPath, label, labelWidth > 0 ? labelWidth : label.Length * 7);
			if (Button("...", 25))
			{
				newPath = EditorUtility.OpenFolderPanel("Select Folder", string.IsNullOrEmpty(defaultPath) ? LastOpenedDirectory : defaultPath, "");
				if (!string.IsNullOrEmpty(newPath))
				{
					if (pFormatToUnityPath && newPath.StartsWith(Application.dataPath))
						newPath = FormatPathToUnityPath(newPath);
					LastOpenedDirectory = newPath;
				}
			}
			EditorGUILayout.EndHorizontal();
			return string.IsNullOrEmpty(newPath) ? defaultPath : newPath;
		}
		
		/// <summary>
		/// Draws a text field combined with a "..." button that opens a file selection dialog.
		/// </summary>
		/// <param name="defaultPath">The initial path displayed in the text field.</param>
		/// <param name="label">The label for the field.</param>
		/// <param name="extension">The file extension filter for the dialog.</param>
		/// <returns>The selected file path.</returns>
		public static string FileField(string defaultPath, string label, string extension, int labelWidth = 0, bool pFormatToUnityPath = true)
		{
			EditorGUILayout.BeginHorizontal();
			var newPath = TextField(defaultPath, label, labelWidth > 0 ? labelWidth : label.Length * 7);
			if (Button("...", 25))
			{
				newPath = EditorUtility.OpenFilePanel("Select File", string.IsNullOrEmpty(defaultPath) ? LastOpenedDirectory : defaultPath, extension);
				if (!string.IsNullOrEmpty(newPath))
				{
					LastOpenedDirectory = Path.GetDirectoryName(newPath);
					if (pFormatToUnityPath && newPath.StartsWith(Application.dataPath))
						newPath = FormatPathToUnityPath(newPath);
				}
			}
			EditorGUILayout.EndHorizontal();
			return string.IsNullOrEmpty(newPath) ? defaultPath : newPath;
		}

		/// <summary>
		/// Draws a single-line text input field.
		/// </summary>
		/// <returns>The string value from the text field.</returns>
		public static string TextField(string value, string label = "", int labelWidth = 80, int valueWidth = 0, bool readOnly = false, Color color = default)
		{
			var text = new GuiText { value = value, label = label, labelWidth = labelWidth, valueWidth = valueWidth, readOnly = readOnly, color = color };
			text.Draw();
			return text.OutputValue;
		}

		/// <summary>
		/// Draws a multi-line text input area.
		/// </summary>
		/// <returns>The string value from the text area.</returns>
		public static string TextArea(string value, string label = "", int labelWidth = 80, int valueWidth = 0, bool readOnly = false)
		{
			var text = new GuiText { value = value, label = label, labelWidth = labelWidth, valueWidth = valueWidth, readOnly = readOnly, textArea = true };
			text.Draw();
			return text.OutputValue;
		}

		/// <summary>
		/// Draws a dropdown list (popup) for a selection of strings.
		/// </summary>
		/// <returns>The selected string.</returns>
		public static string DropdownList(string value, string label, string[] selections, int labelWidth = 80, int valueWidth = 0)
		{
			var dropdown = new GuiDropdownListString { label = label, labelWidth = labelWidth, selections = selections ?? new string[0], value = value, valueWidth = valueWidth };
			dropdown.Draw();
			return dropdown.OutputValue;
		}

		/// <summary>
		/// Draws a dropdown list (popup) for a selection of integers.
		/// </summary>
		/// <returns>The selected integer.</returns>
		public static int DropdownList(int value, string label, int[] selections, int labelWidth = 80, int valueWidth = 0)
		{
			var dropdown = new GuiDropdownListInt { label = label, labelWidth = labelWidth, value = value, valueWidth = valueWidth, selections = selections ?? new int[0] };
			dropdown.Draw();
			return dropdown.OutputValue;
		}

		/// <summary>
		/// Draws a dropdown list (popup) for an Enum type.
		/// </summary>
		/// <typeparam name="T">The Enum type.</typeparam>
		/// <returns>The selected enum value.</returns>
		public static T DropdownList<T>(T value, string label = "", int labelWidth = 80, int valueWidth = 0) where T : struct, IConvertible
		{
			var dropdown = new GuiDropdownListEnum<T> { label = label, labelWidth = labelWidth, value = value, valueWidth = valueWidth };
			dropdown.Draw();
			return dropdown.OutputValue;
		}
		
		/// <summary>
		/// Draws a dropdown list populated with the names of objects from a given list.
		/// </summary>
		/// <typeparam name="T">The type of UnityEngine.Object.</typeparam>
		/// <param name="selectedObj">The currently selected object.</param>
		/// <param name="label">The label for the dropdown.</param>
		/// <param name="pOptions">The list of objects to populate the dropdown.</param>
		/// <returns>The object selected by the user.</returns>
		public static T DropdownList<T>(T selectedObj, string label, List<T> pOptions) where T : Object
		{
			string selectedName = selectedObj == null ? "None" : selectedObj.name;
			// Create string array for display, including a "None" option.
			string[] options = new string[pOptions.Count + 1];
			options[0] = "None";
			for (int i = 1; i < pOptions.Count + 1; i++)
			{
				if (pOptions[i - 1] != null)
					options[i] = pOptions[i - 1].name;
				else
					options[i] = "NULL";
			}

			var selected = DropdownList(selectedName, label, options);
			if (selectedName != selected)
			{
				selectedName = selected;
				// Find the corresponding object from the original list.
				foreach (var o in pOptions)
				{
					if (o.name == selectedName)
					{
						selectedObj = o;
						break;
					}
				}

				if (selectedName == "None")
					selectedObj = null;
			}

			return selectedObj;
		}

		/// <summary>
		/// Draws a toggle (checkbox).
		/// </summary>
		/// <returns>The boolean state of the toggle.</returns>
		public static bool Toggle(bool value, string label = "", int labelWidth = 80, int valueWidth = 0, Color color = default)
		{
			var toggle = new GuiToggle { label = label, labelWidth = labelWidth, value = value, valueWidth = valueWidth, color = color };
			toggle.Draw();
			return toggle.OutputValue;
		}

		/// <summary>
		/// Draws an integer input field.
		/// </summary>
		/// <returns>The integer value from the field.</returns>
		public static int IntField(int value, string label = "", int labelWidth = 80, int valueWidth = 0, bool readOnly = false, int min = 0, int max = 0)
		{
			var intField = new GuiInt { value = value, label = label, labelWidth = labelWidth, valueWidth = valueWidth, readOnly = readOnly, min = min, max = max };
			intField.Draw();
			return intField.OutputValue;
		}

		/// <summary>
		/// Draws a float input field. If min and max values are different, it is drawn as a slider.
		/// </summary>
		/// <returns>The float value from the field.</returns>
		public static float FloatField(float value, string label, int labelWidth = 80, int valueWidth = 0, float pMin = 0, float pMax = 0)
		{
			if (!string.IsNullOrEmpty(label))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
			}

			float result;
			if (valueWidth == 0)
			{
				if (pMin != pMax)
					result = EditorGUILayout.Slider(value, pMin, pMax, GUILayout.Height(20));
				else
					result = EditorGUILayout.FloatField(value, GUILayout.Height(20));
			}
			else
			{
				if (pMin != pMax)
					result = EditorGUILayout.Slider(value, pMin, pMax, GUILayout.Height(20), GUILayout.Width(valueWidth));
				else
					result = EditorGUILayout.FloatField(value, GUILayout.Height(20), GUILayout.Width(valueWidth));
			}

			if (!string.IsNullOrEmpty(label))
				EditorGUILayout.EndHorizontal();

			return result;
		}
		
		/// <summary>
		/// Draws an object field for assigning assets or scene objects.
		/// </summary>
		/// <typeparam name="T">The type of UnityEngine.Object to accept.</typeparam>
		/// <returns>The assigned object.</returns>
		public static Object ObjectField<T>(Object value, string label = "", int labelWidth = 80, int valueWidth = 0, bool showAsBox = false)
		{
			var obj = new GuiObject<T> { value = value, label = label, labelWidth = labelWidth, valueWidth = valueWidth, showAsBox = showAsBox };
			obj.Draw();
			return obj.OutputValue;
		}

		/// <summary>
		/// Draws a label with optional bold styling, alignment, and color.
		/// </summary>
		public static void Label(string label, int width = 0, bool isBold = true, TextAnchor alignment = TextAnchor.MiddleLeft, Color color = default)
		{
			var style = new GUIStyle(isBold ? EditorStyles.boldLabel : EditorStyles.label) { alignment = alignment };
			if (color != default) style.normal.textColor = color;

			if (width > 0)
				EditorGUILayout.LabelField(label, style, GUILayout.Width(width));
			else
				EditorGUILayout.LabelField(label, style);
		}

		/// <summary>
		/// Draws a color picker field.
		/// </summary>
		/// <returns>The selected color.</returns>
		public static Color ColorField(Color value, string label = "", int labelWidth = 80, int valueWidth = 0)
		{
			var colorField = new GuiColor { value = value, label = label, labelWidth = labelWidth, valueWidth = valueWidth };
			colorField.Draw();
			return colorField.outputValue;
		}
		
		/// <summary>
		/// Draws a field for editing a Vector2.
		/// </summary>
		/// <returns>The edited Vector2 value.</returns>
		public static Vector2 Vector2Field(Vector2 value, string label, int labelWidth = 80, int valueWidth = 0)
		{
			if (!string.IsNullOrEmpty(label))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
			}

			Vector2 result;
			if (valueWidth == 0)
				result = EditorGUILayout.Vector2Field("", value, GUILayout.Height(20), GUILayout.MinWidth(40));
			else
				result = EditorGUILayout.Vector2Field("", value, GUILayout.Height(20), GUILayout.MinWidth(40),
					GUILayout.Width(valueWidth));

			if (!string.IsNullOrEmpty(label))
				EditorGUILayout.EndHorizontal();

			return result;
		}
		
		/// <summary>
		/// Draws a field for editing a Vector3.
		/// </summary>
		/// <returns>The edited Vector3 value.</returns>
		public static Vector3 Vector3Field(Vector3 value, string label, int labelWidth = 80, int valueWidth = 0)
		{
			if (!string.IsNullOrEmpty(label))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
			}

			Vector3 result;
			if (valueWidth == 0)
				result = EditorGUILayout.Vector3Field("", value, GUILayout.Height(20), GUILayout.MinWidth(40));
			else
				result = EditorGUILayout.Vector3Field("", value, GUILayout.Height(20), GUILayout.MinWidth(40),
					GUILayout.Width(valueWidth));

			if (!string.IsNullOrEmpty(label))
				EditorGUILayout.EndHorizontal();

			return result;
		}
		
		/// <summary>
		/// Draws a series of float fields for editing an array of floats.
		/// </summary>
		/// <param name="values">The array of floats to edit.</param>
		/// <param name="label">The label for the entire array field.</param>
		/// <param name="showHorizontal">If true, the float fields are arranged horizontally; otherwise, vertically.</param>
		/// <returns>A new array with the edited float values.</returns>
		public static float[] ArrayField(float[] values, string label, bool showHorizontal = true, int labelWidth = 80, int valueWidth = 0)
		{
			if (!string.IsNullOrEmpty(label))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
			}

			if (showHorizontal)
				EditorGUILayout.BeginHorizontal();
			else
				EditorGUILayout.BeginVertical();
			float[] results = new float[values.Length];
			for (int i = 0; i < values.Length; i++)
			{
				float result;
				if (valueWidth == 0)
					result = EditorGUILayout.FloatField(values[i], GUILayout.Height(20));
				else
					result = EditorGUILayout.FloatField(values[i], GUILayout.Height(20), GUILayout.Width(valueWidth));

				results[i] = result;
			}

			if (showHorizontal)
				EditorGUILayout.EndHorizontal();
			else
				EditorGUILayout.EndVertical();

			if (!string.IsNullOrEmpty(label))
				EditorGUILayout.EndHorizontal();

			return results;
		}

		/// <summary>
		/// Formats a full system path to a Unity-relative path (starting with "Assets/").
		/// </summary>
		/// <param name="path">The full path to format.</param>
		/// <returns>A Unity-relative path.</returns>
		public static string FormatPathToUnityPath(string path)
		{
			string[] paths = path.Split('/');

			int startJoint = -1;
			string realPath = "";

			for (int i = 0; i < paths.Length; i++)
			{
				if (paths[i] == "Assets")
					startJoint = i;

				if (startJoint != -1 && i >= startJoint)
				{
					if (i == paths.Length - 1)
						realPath += paths[i];
					else
						realPath += $"{paths[i]}/";
				}
			}

			return realPath;
		}
	}
}
#endif