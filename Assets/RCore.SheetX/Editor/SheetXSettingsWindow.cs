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