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
			m_sheetXSettings.constantsOutputFolder = EditorHelper.FolderField(
				m_sheetXSettings.constantsOutputFolder, "Scripts output folder", 200,
				tooltip: "Destination directory where generated C# constants and enum scripts are saved.");
			m_sheetXSettings.jsonOutputFolder = EditorHelper.FolderField(
				m_sheetXSettings.jsonOutputFolder, "Json output folder", 200,
				tooltip: "Destination directory where exported JSON data files are saved.");
			m_sheetXSettings.localizationOutputFolder = EditorHelper.FolderField(
				m_sheetXSettings.localizationOutputFolder, "Localization output folder", 200,
				tooltip: "Destination directory where exported localization text files and CSVs are saved.");
			m_sheetXSettings.@namespace = EditorHelper.TextField(
				m_sheetXSettings.@namespace, "Namespace", 200,
				tooltip: "Default C# namespace applied to generated constants, collections, and data classes.");
			m_sheetXSettings.separateIDs = EditorHelper.Toggle(
				m_sheetXSettings.separateIDs, "Separate IDs Sheets", 200,
				tooltip: "When enabled, generates separate C# ID enum files for each ID sheet instead of combining into one.");
			m_sheetXSettings.separateConstants = EditorHelper.Toggle(
				m_sheetXSettings.separateConstants, "Separate Constants Sheets", 200,
				tooltip: "When enabled, generates separate C# constant classes for each constant sheet.");
			m_sheetXSettings.separateLocalizations = EditorHelper.Toggle(
				m_sheetXSettings.separateLocalizations, "Separate Localizations Sheets", 200,
				tooltip: "When enabled, exports each localization sheet to its own dedicated file.");
			m_sheetXSettings.onlyEnumAsIDs = EditorHelper.Toggle(
				m_sheetXSettings.onlyEnumAsIDs, "Only enum as IDs", 200,
				tooltip: "When enabled, only sheets marked with enum types produce ID definitions.");
			m_sheetXSettings.combineJson = EditorHelper.Toggle(
				m_sheetXSettings.combineJson, "Combine Json Sheets", 200,
				tooltip: "When enabled, combines all table JSON outputs into a single consolidated JSON file.");
			m_sheetXSettings.langCharSets = EditorHelper.TextField(
				m_sheetXSettings.langCharSets, "Lang char sets", 200,
				tooltip: "Specific character sets to extract from localization text (e.g. for generating font asset atlases).");
			m_sheetXSettings.persistentFields = EditorHelper.TextField(
				m_sheetXSettings.persistentFields, "Persistent fields", 200,
				tooltip: "Comma-separated list of field names that should preserve values across data reloads.");
			m_sheetXSettings.ObfGoogleClientId = EditorHelper.TextField(
				m_sheetXSettings.ObfGoogleClientId, "Google client id", 200,
				tooltip: "Google OAuth 2.0 Client ID used to authenticate and read Google Spreadsheets.");
			m_sheetXSettings.ObfGoogleClientSecret = EditorHelper.TextField(
				m_sheetXSettings.ObfGoogleClientSecret, "Google client secret", 200,
				tooltip: "Google OAuth 2.0 Client Secret used to authenticate and read Google Spreadsheets.");
			GUILayout.EndVertical();
			DrawCollections();
			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(m_sheetXSettings);
			if (EditorHelper.Button("Reset to default settings", tooltip: "Reset all settings in this window back to their default values."))
				m_sheetXSettings.ResetToDefault();

			SupportDev();
		}

		private void DrawCollections()
		{
			GUILayout.BeginVertical("box");
			m_sheetXSettings.enableCollections = EditorHelper.Toggle(
				m_sheetXSettings.enableCollections, "Enable Data Config Collections", 200,
				tooltip: "Enables ScriptableObject Data Config Collections workflow to bake Excel/Google sheets into Unity assets.");
			if (m_sheetXSettings.enableCollections)
			{
				m_sheetXSettings.collectionNamespace = EditorHelper.TextFieldWithFallback(
					m_sheetXSettings.collectionNamespace, m_sheetXSettings.ResolveCollectionNamespace(),
					"Collection namespace", 200,
					tooltip: "C# namespace for generated collection ScriptableObjects and row data classes. Falls back to global Namespace if empty.");
				m_sheetXSettings.collectionCodeFolder = EditorHelper.FolderFieldWithFallback(
					m_sheetXSettings.collectionCodeFolder, m_sheetXSettings.ResolveCollectionCodeFolder(),
					"Generated code folder", 200,
					tooltip: "Folder where collection and row C# scripts are generated. Falls back to Scripts output folder if empty.");
				m_sheetXSettings.collectionAssetFolder = EditorHelper.FolderFieldWithFallback(
					m_sheetXSettings.collectionAssetFolder, m_sheetXSettings.ResolveCollectionAssetFolder(),
					"Collection asset folder", 200,
					tooltip: "Folder where feature collection ScriptableObject assets (.asset) are saved. Falls back to Assets/Resources if empty.");
				m_sheetXSettings.collectionJsonFolder = EditorHelper.FolderFieldWithFallback(
					m_sheetXSettings.collectionJsonFolder, m_sheetXSettings.ResolveCollectionJsonFolder(),
					"Collection JSON folder", 200,
					tooltip: "Folder for collection bake-source JSON files. Must stay outside Resources and StreamingAssets. Falls back to Json output folder if empty.");
				m_sheetXSettings.globalResourcesFolder = EditorHelper.FolderFieldWithFallback(
					m_sheetXSettings.globalResourcesFolder, m_sheetXSettings.ResolveGlobalResourcesFolder(),
					"Global Resources folder", 200,
					tooltip: "Resources folder for GlobalConfigCollection.asset so GlobalConfigCollection.Instance can load it. Must end in 'Resources'.");

				m_sheetXSettings.autoLoadAfterExport = EditorHelper.Toggle(
					m_sheetXSettings.autoLoadAfterExport, "Auto load after export", 200,
					tooltip: "Automatically bake JSON into ScriptableObject assets immediately after exporting sheets.");
				m_sheetXSettings.autoLoadBeforePlay = EditorHelper.Toggle(
					m_sheetXSettings.autoLoadBeforePlay, "Auto load before Play Mode", 200,
					tooltip: "Automatically re-bake dirty collection assets before entering Unity Play Mode.");

				if (EditorHelper.Button("Manage Collections...", 200, 24, tooltip: "Open popup window to manage root (Global) and feature collections, auto-load rules, and manual bake actions."))
				{
					SheetXCollectionsWindow.ShowWindow(m_sheetXSettings);
				}
			}
			GUILayout.EndVertical();
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
					// An .sx written before credentials moved to EditorPrefs still carries them, and
					// FromJsonOverwrite repopulates the legacy fields. Drain them straight back out.
					m_sheetXSettings.MigrateCredentialsToEditorPrefs();
				}
				catch (JsonException)
				{
					Debug.LogError("The sx file is not valid.");
				}
			}
		}
	}
}