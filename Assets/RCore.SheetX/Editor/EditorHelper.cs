/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using UnityEditor;
using UnityEngine;
using System;
using System.IO;

namespace RCore.Editor.SheetX
{
	public interface IDraw
	{
		void Draw(GUIStyle style = null);
	}

	public class EditorTabs : IDraw
	{
		public string key;
		public string[] tabsName;
		public string CurrentTab { get; private set; }

		public void Draw(GUIStyle style = null)
		{
			CurrentTab = EditorPrefs.GetString($"{key}_current_tab", tabsName[0]);

			GUILayout.BeginHorizontal();
			foreach (var tabName in tabsName)
			{
				bool isOn = CurrentTab == tabName;
				var buttonStyle = new GUIStyle(EditorStyles.toolbarButton)
				{
					fixedHeight = 0,
					padding = new RectOffset(4, 4, 4, 4),
					normal =
					{
						textColor = EditorGUIUtility.isProSkin ? Color.white : (isOn ? Color.black : Color.black * 0.6f)
					},
					fontStyle = FontStyle.Bold,
					fontSize = 13
				};

				var preColor = GUI.color;
				var color = isOn ? Color.white : Color.gray;
				GUI.color = color;

				if (GUILayout.Button(tabName, buttonStyle))
				{
					CurrentTab = tabName;
					EditorPrefs.SetString($"{key}_current_tab", CurrentTab);
				}

				GUI.color = preColor;
			}
			GUILayout.EndHorizontal();
		}
	}

	public class EditorText : IDraw
	{
		public string label;
		public int labelWidth = 80;
		public int valueWidth;
		public string value;
		public string OutputValue { get; private set; }
		public bool readOnly;
		public bool textArea;
		public Color color;

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
					str = EditorGUILayout.TextArea(value, style, GUILayout.MinWidth(40));
				else
					str = EditorGUILayout.TextField(value, style, GUILayout.MinWidth(40));
			}
			else
			{
				if (textArea)
					str = EditorGUILayout.TextArea(value, style, GUILayout.MinWidth(40), GUILayout.Width(valueWidth));
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

	public class EditorButton : IDraw
	{
		public string label;
		public Color color;
		public int width;
		public int height;
		public Action onPressed;
		public bool IsPressed { get; private set; }

		public EditorButton() { }

		public EditorButton(string pLabel, Action pOnPressed, Color pColor = default)
		{
			label = pLabel;
			onPressed = pOnPressed;
			color = pColor;
		}

		public void Draw(GUIStyle style = null)
		{
			var defaultColor = GUI.backgroundColor;
			style ??= new GUIStyle("Button");
			if (width > 0)
				style.fixedWidth = width;
			if (height > 0)
				style.fixedHeight = height;
			if (color != default)
				GUI.backgroundColor = color;
			IsPressed = GUILayout.Button(label, style, GUILayout.MinHeight(21));
			if (IsPressed && onPressed != null)
				onPressed();
			GUI.backgroundColor = defaultColor;
		}
	}

	public class EditorToggle : IDraw
	{
		public string label;
		public int labelWidth = 80;
		public bool value;
		public int valueWidth;
		public bool readOnly;
		public Color color;
		public bool OutputValue { get; private set; }

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

	public static class EditorHelper
	{
		private static string LastOpenedDirectory { get => EditorPrefs.GetString("LastOpenedDirectory"); set => EditorPrefs.SetString("LastOpenedDirectory", value); }

		public static string Tabs(string pKey, params string[] pTabsName)
		{
			var tabs = new EditorTabs()
			{
				key = pKey,
				tabsName = pTabsName,
			};
			tabs.Draw();
			return tabs.CurrentTab;
		}

		public static string TextField(string value, string label, int labelWidth = 80, int valueWidth = 0, bool readOnly = false, Color color = default)
		{
			var text = new EditorText()
			{
				value = value,
				label = label,
				labelWidth = labelWidth,
				valueWidth = valueWidth,
				readOnly = readOnly,
				color = color,
			};
			text.Draw();
			return text.OutputValue;
		}

		public static void LabelField(string label, int width = 0, bool isBold = true, TextAnchor pTextAnchor = TextAnchor.MiddleLeft, Color pTextColor = default)
		{
			var style = new GUIStyle(isBold ? EditorStyles.boldLabel : EditorStyles.label)
			{
				alignment = pTextAnchor,
				margin = new RectOffset(0, 0, 0, 0)
			};
			if (pTextColor != default)
				style.normal.textColor = pTextColor;
			if (width > 0)
				EditorGUILayout.LabelField(label, style, GUILayout.MinWidth(width), GUILayout.MaxWidth(width));
			else
				EditorGUILayout.LabelField(label, style);
		}

		public static bool Button(string pLabel, int pWidth = 0, int pHeight = 0)
		{
			var button = new EditorButton()
			{
				label = pLabel,
				width = pWidth,
				height = pHeight
			};
			button.Draw();
			return button.IsPressed;
		}

		public static string OpenFilePanel(string title, string extension, string directory = null)
		{
			string path = EditorUtility.OpenFilePanel(title, directory ?? LastOpenedDirectory, extension);
			if (!string.IsNullOrEmpty(path))
				LastOpenedDirectory = Path.GetDirectoryName(path);
			return path;
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

		public static T CreateScriptableAsset<T>(string path) where T : ScriptableObject
		{
			var asset = ScriptableObject.CreateInstance<T>();

			var directoryPath = Path.GetDirectoryName(path);
			if (!Directory.Exists(directoryPath))
				if (directoryPath != null)
					Directory.CreateDirectory(directoryPath);

			AssetDatabase.CreateAsset(asset, path);
			AssetDatabase.SaveAssets();
			EditorUtility.FocusProjectWindow();
			return asset;
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

		public static bool Toggle(bool value, string label, int labelWidth = 80, int valueWidth = 0, Color color = default)
		{
			var toggle = new EditorToggle()
			{
				label = label,
				labelWidth = labelWidth,
				value = value,
				valueWidth = valueWidth,
				color = color,
			};
			toggle.Draw();
			return toggle.OutputValue;
		}
	}
}