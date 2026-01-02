/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// Handles the "Settings" tab within the SheetX window, allowing configuration of paths and options.
	/// </summary>
	public class SheetXSettingsWindow
	{
		private SheetXSettings m_sheetXSettings;

		public void OnEnable()
		{
			m_sheetXSettings = SheetXSettings.Init();
		}

		public void OnGUI()
		{
			EditorGUI.BeginChangeCheck();
			GUILayout.BeginVertical("box");
#if SX_LOCALIZATION
			m_sheetXSettings.constantsOutputFolder = EditorHelper.FolderField(m_sheetXSettings.constantsOutputFolder, "Scripts output folder", 200);
			m_sheetXSettings.localizationOutputFolder = EditorHelper.FolderField(m_sheetXSettings.localizationOutputFolder, "Localization output folder", 200);
			m_sheetXSettings.@namespace = EditorHelper.TextField(m_sheetXSettings.@namespace, "Namespace", 200);
			m_sheetXSettings.separateLocalizations = EditorHelper.Toggle(m_sheetXSettings.separateLocalizations, "Separate Localizations Sheets", 200);
			m_sheetXSettings.langCharSets = EditorHelper.TextField(m_sheetXSettings.langCharSets, "Lang char sets", 200);
			m_sheetXSettings.ObfGoogleClientId = EditorHelper.TextField(m_sheetXSettings.ObfGoogleClientId, "Google client id", 200);
			m_sheetXSettings.ObfGoogleClientSecret = EditorHelper.TextField(m_sheetXSettings.ObfGoogleClientSecret, "Google client secret", 200);
#else
			m_sheetXSettings.constantsOutputFolder = EditorHelper.FolderField(m_sheetXSettings.constantsOutputFolder, "Scripts output folder", 200);
			m_sheetXSettings.jsonOutputFolder = EditorHelper.FolderField(m_sheetXSettings.jsonOutputFolder, "Json output folder", 200);
#if !SX_NO_LOCALIZATION
			m_sheetXSettings.localizationOutputFolder = EditorHelper.FolderField(m_sheetXSettings.localizationOutputFolder, "Localization output folder", 200);
#endif
			m_sheetXSettings.@namespace = EditorHelper.TextField(m_sheetXSettings.@namespace, "Namespace", 200);
			m_sheetXSettings.separateIDs = EditorHelper.Toggle(m_sheetXSettings.separateIDs, "Separate IDs Sheets", 200);
			m_sheetXSettings.separateConstants = EditorHelper.Toggle(m_sheetXSettings.separateConstants, "Separate Constants Sheets", 200);
#if !SX_NO_LOCALIZATION
			m_sheetXSettings.separateLocalizations = EditorHelper.Toggle(m_sheetXSettings.separateLocalizations, "Separate Localizations Sheets", 200);
#endif
			m_sheetXSettings.onlyEnumAsIDs = EditorHelper.Toggle(m_sheetXSettings.onlyEnumAsIDs, "Only enum as IDs", 200);
			m_sheetXSettings.combineJson = EditorHelper.Toggle(m_sheetXSettings.combineJson, "Combine Json Sheets", 200);
#if !SX_NO_LOCALIZATION
			m_sheetXSettings.langCharSets = EditorHelper.TextField(m_sheetXSettings.langCharSets, "Lang char sets", 200);
#endif
			m_sheetXSettings.persistentFields = EditorHelper.TextField(m_sheetXSettings.persistentFields, "Persistent fields", 200);
			m_sheetXSettings.ObfGoogleClientId = EditorHelper.TextField(m_sheetXSettings.ObfGoogleClientId, "Google client id", 200);
			m_sheetXSettings.ObfGoogleClientSecret = EditorHelper.TextField(m_sheetXSettings.ObfGoogleClientSecret, "Google client secret", 200);
#endif
			GUILayout.EndVertical();
			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(m_sheetXSettings);
			if (GUILayout.Button("Reset to default settings"))
				m_sheetXSettings.ResetToDefault();

			SupportDev();
		}

		private void SupportDev()
		{
			var color = GUI.backgroundColor;
			GUILayout.Space(5);
			var labelStyle = new GUIStyle(EditorStyles.helpBox)
			{
				fontSize = 15,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter,
				padding = new RectOffset(10, 10, 10, 10)
			};
			GUILayout.Label("If you are enjoying this tool, please consider supporting the project.", labelStyle);
			GUILayout.BeginHorizontal();

			bool rated = EditorPrefs.GetBool($"{Application.identifier}.RateClicked", false);
			if (!rated) GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
			else GUI.backgroundColor = color;
			
			if (GUILayout.Button("Rate on Asset Store", GUILayout.Height(30)))
			{
				Application.OpenURL("https://assetstore.unity.com/packages/tools/utilities/sheetx-pro-manage-constants-data-localization-with-excel-google--300772");
				EditorPrefs.SetBool($"{Application.identifier}.RateClicked", true);
			}

			bool starred = EditorPrefs.GetBool($"{Application.identifier}.StarClicked", false);
			if (!starred) GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
			else GUI.backgroundColor = color;

			if (GUILayout.Button("Star on GitHub", GUILayout.Height(30)))
			{
				Application.OpenURL("https://github.com/hnb-rabear/RCore");
				EditorPrefs.SetBool($"{Application.identifier}.StarClicked", true);
			}
			GUILayout.EndHorizontal();

			GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
			if (GUILayout.Button("Buy me a coffee", GUILayout.Height(30)))
				Application.OpenURL("https://ko-fi.com/rabear");
			GUI.backgroundColor = color;
		}

		/// <summary>
		/// Saves the current settings to a JSON file (.sx).
		/// </summary>
		public void Save()
		{
			string content = JsonUtility.ToJson(m_sheetXSettings);
			string directory = Application.dataPath.Replace("Assets", "");
			EditorHelper.SaveFilePanel(directory, "SheetXSave", content, "sx", "Save SheetX Settings");
		}

		/// <summary>
		/// Loads settings from a JSON file (.sx).
		/// </summary>
		public void Load()
		{
			string directory = Application.dataPath.Replace("Assets", "");
			var path = EditorHelper.OpenFilePanel("Load SheetX Settings", "sx", directory);
			if (!string.IsNullOrEmpty(path))
			{
				string content = File.ReadAllText(path);
				try
				{
					JsonUtility.FromJsonOverwrite(content, m_sheetXSettings);
				}
				catch (JsonException)
				{
					Debug.LogError("The sx file is not valid.");
				}
			}
		}
	}
}