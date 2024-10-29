using System.Collections;
using System.Collections.Generic;
using RCore.Editor;
using UnityEditor;
using UnityEngine;

namespace RCore.E2U
{
	public class E2USettingsWindow : EditorWindow
	{
		private Vector2 m_scrollPosition;
		private E2USettings m_e2USettings;

		private void OnEnable()
		{
			m_e2USettings = E2USettings.Load();
		}

		private void OnGUI()
		{
			EditorGUI.BeginChangeCheck();
			GUILayout.BeginVertical("box");
			m_e2USettings.constantsOutputFolder = EditorHelper.FolderField(m_e2USettings.constantsOutputFolder, "Constants output folder", 200);
			m_e2USettings.jsonOutputFolder = EditorHelper.FolderField(m_e2USettings.jsonOutputFolder, "Json output folder", 200);
			m_e2USettings.localizationOutputFolder = EditorHelper.FolderField(m_e2USettings.localizationOutputFolder, "Localization output folder", 200);
			m_e2USettings.@namespace = EditorHelper.TextField(m_e2USettings.@namespace, "Namespace", 200);
			m_e2USettings.separateIDs = EditorHelper.Toggle(m_e2USettings.separateIDs, "Separate IDs Sheets", 200);
			m_e2USettings.separateConstants = EditorHelper.Toggle(m_e2USettings.separateConstants, "Separate Constants Sheets", 200);
			m_e2USettings.separateLocalizations = EditorHelper.Toggle(m_e2USettings.separateLocalizations, "Separate Localizations Sheets", 200);
			m_e2USettings.combineJson = EditorHelper.Toggle(m_e2USettings.combineJson, "Combine Json Sheets", 200);
			m_e2USettings.languageMaps = EditorHelper.TextField(m_e2USettings.languageMaps, "Language maps", 200);
			m_e2USettings.persistentFields = EditorHelper.TextField(m_e2USettings.persistentFields, "Persistent fields", 200);
			m_e2USettings.excludedSheets = EditorHelper.TextField(m_e2USettings.excludedSheets, "Excluded sheets", 200);
			m_e2USettings.googleClientId = EditorHelper.TextField(m_e2USettings.googleClientId, "Google client id", 200);
			m_e2USettings.googleClientSecret = EditorHelper.TextField(m_e2USettings.googleClientSecret, "Google client secret", 200);
			GUILayout.EndVertical();
			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(m_e2USettings);
			if (GUILayout.Button("Reset to default settings"))
				m_e2USettings.ResetToDefault();
		}

		[MenuItem("Window/E2U/Settings")]
		public static void ShowWindow()
		{
			var window = GetWindow<E2USettingsWindow>("E2U Settings", true);
			window.Show();
		}
	}
}