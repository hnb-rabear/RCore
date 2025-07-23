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
	public static class EditorGui
	{
		private static string LastOpenedDirectory { get => EditorPrefs.GetString("LastOpenedDirectory"); set => EditorPrefs.SetString("LastOpenedDirectory", value); }

		public static bool Button(string label, int width = 0, int height = 0)
		{
			var button = new EditorButton { label = label, width = width, height = height };
			button.Draw();
			return button.IsPressed;
		}

		public static bool Button(string label, Color color, int width = 0, int height = 0)
		{
			var button = new EditorButton { label = label, width = width, color = color, height = height };
			button.Draw();
			return button.IsPressed;
		}

		public static bool Button(string label, Texture2D icon, Color color = default, int width = 0, int height = 0)
		{
			var button = new EditorButton { label = label, icon = icon, color = color, width = width, height = height };
			button.Draw();
			return button.IsPressed;
		}

		public static bool ConfirmPopup(string message = "Are you sure?", string yes = "Yes", string no = "No")
		{
			return EditorUtility.DisplayDialog("Confirm Action", message, yes, no);
		}

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

		public static string FileField(string defaultPath, string label, string extension, int labelWidth = 0, bool pFormatToUnityPath = true)
		{
			EditorGUILayout.BeginHorizontal();
			var newPath = TextField(defaultPath, label, labelWidth > 0 ? labelWidth : label.Length * 7);
			if (Button("...", 25))
			{
				newPath = EditorUtility.OpenFilePanel("Select Folder", string.IsNullOrEmpty(defaultPath) ? LastOpenedDirectory : defaultPath, extension);
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

		public static string TextField(string value, string label = "", int labelWidth = 80, int valueWidth = 0, bool readOnly = false, Color color = default)
		{
			var text = new EditorText { value = value, label = label, labelWidth = labelWidth, valueWidth = valueWidth, readOnly = readOnly, color = color };
			text.Draw();
			return text.OutputValue;
		}

		public static string TextArea(string value, string label = "", int labelWidth = 80, int valueWidth = 0, bool readOnly = false)
		{
			var text = new EditorText { value = value, label = label, labelWidth = labelWidth, valueWidth = valueWidth, readOnly = readOnly, textArea = true };
			text.Draw();
			return text.OutputValue;
		}

		public static string DropdownList(string value, string label, string[] selections, int labelWidth = 80, int valueWidth = 0)
		{
			var dropdown = new EditorDropdownListString { label = label, labelWidth = labelWidth, selections = selections ?? new string[0], value = value, valueWidth = valueWidth };
			dropdown.Draw();
			return dropdown.OutputValue;
		}

		public static int DropdownList(int value, string label, int[] selections, int labelWidth = 80, int valueWidth = 0)
		{
			var dropdown = new EditorDropdownListInt { label = label, labelWidth = labelWidth, value = value, valueWidth = valueWidth, selections = selections ?? new int[0] };
			dropdown.Draw();
			return dropdown.OutputValue;
		}

		public static T DropdownList<T>(T value, string label = "", int labelWidth = 80, int valueWidth = 0) where T : struct, IConvertible
		{
			var dropdown = new EditorDropdownListEnum<T> { label = label, labelWidth = labelWidth, value = value, valueWidth = valueWidth };
			dropdown.Draw();
			return dropdown.OutputValue;
		}

		public static T DropdownList<T>(T selectedObj, string label, List<T> pOptions) where T : Object
		{
			string selectedName = selectedObj == null ? "None" : selectedObj.name;
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

		public static bool Toggle(bool value, string label = "", int labelWidth = 80, int valueWidth = 0, Color color = default)
		{
			var toggle = new EditorToggle { label = label, labelWidth = labelWidth, value = value, valueWidth = valueWidth, color = color };
			toggle.Draw();
			return toggle.OutputValue;
		}

		public static int IntField(int value, string label = "", int labelWidth = 80, int valueWidth = 0, bool readOnly = false, int min = 0, int max = 0)
		{
			var intField = new EditorInt { value = value, label = label, labelWidth = labelWidth, valueWidth = valueWidth, readOnly = readOnly, min = min, max = max };
			intField.Draw();
			return intField.OutputValue;
		}

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

		public static Object ObjectField<T>(Object value, string label = "", int labelWidth = 80, int valueWidth = 0, bool showAsBox = false)
		{
			var obj = new EditorObject<T> { value = value, label = label, labelWidth = labelWidth, valueWidth = valueWidth, showAsBox = showAsBox };
			obj.Draw();
			return obj.OutputValue;
		}

		public static void Label(string label, int width = 0, bool isBold = true, TextAnchor alignment = TextAnchor.MiddleLeft, Color color = default)
		{
			var style = new GUIStyle(isBold ? EditorStyles.boldLabel : EditorStyles.label) { alignment = alignment };
			if (color != default) style.normal.textColor = color;

			if (width > 0)
				EditorGUILayout.LabelField(label, style, GUILayout.Width(width));
			else
				EditorGUILayout.LabelField(label, style);
		}

		public static Color ColorField(Color value, string label = "", int labelWidth = 80, int valueWidth = 0)
		{
			var colorField = new EditorColor { value = value, label = label, labelWidth = labelWidth, valueWidth = valueWidth };
			colorField.Draw();
			return colorField.outputValue;
		}

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