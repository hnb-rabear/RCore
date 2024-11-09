using RCore.Editor;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX
{
	public class SheetXSettingsWindow : EditorWindow
	{
		private Vector2 m_scrollPosition;
		private SheetXSettings m_sheetXSettings;
		private string m_googleClientId;
		private string m_googleClientSecret;
		
		private void OnEnable()
		{
			m_sheetXSettings = SheetXSettings.Load();
			m_googleClientId = m_sheetXSettings.GoogleClientId;
			m_googleClientSecret = m_sheetXSettings.GoogleClientSecret;
		}

		private void OnGUI()
		{
			EditorGUI.BeginChangeCheck();
			GUILayout.BeginVertical("box");
			m_sheetXSettings.constantsOutputFolder = EditorHelper.FolderField(m_sheetXSettings.constantsOutputFolder, "Constants output folder", 200);
			m_sheetXSettings.jsonOutputFolder = EditorHelper.FolderField(m_sheetXSettings.jsonOutputFolder, "Json output folder", 200);
			m_sheetXSettings.localizationOutputFolder = EditorHelper.FolderField(m_sheetXSettings.localizationOutputFolder, "Localization output folder", 200);
			m_sheetXSettings.@namespace = EditorHelper.TextField(m_sheetXSettings.@namespace, "Namespace", 200);
			m_sheetXSettings.separateIDs = EditorHelper.Toggle(m_sheetXSettings.separateIDs, "Separate IDs Sheets", 200);
			m_sheetXSettings.separateConstants = EditorHelper.Toggle(m_sheetXSettings.separateConstants, "Separate Constants Sheets", 200);
			m_sheetXSettings.separateLocalizations = EditorHelper.Toggle(m_sheetXSettings.separateLocalizations, "Separate Localizations Sheets", 200);
			m_sheetXSettings.combineJson = EditorHelper.Toggle(m_sheetXSettings.combineJson, "Combine Json Sheets", 200);
			m_sheetXSettings.langCharSets = EditorHelper.TextField(m_sheetXSettings.langCharSets, "Lang char sets", 200);
			m_sheetXSettings.persistentFields = EditorHelper.TextField(m_sheetXSettings.persistentFields, "Persistent fields", 200);
			string editGoogleClientId = EditorHelper.TextField(m_googleClientId, "Google client id", 200);
			if (editGoogleClientId != m_googleClientId)
			{
				m_googleClientId = editGoogleClientId;
				m_sheetXSettings.GoogleClientId = editGoogleClientId;
			}
			string editGoogleClientSecret = EditorHelper.TextField(m_googleClientSecret, "Google client secret", 200);
			if (editGoogleClientSecret != m_googleClientSecret)
			{
				m_googleClientSecret = editGoogleClientSecret;
				m_sheetXSettings.GoogleClientSecret = editGoogleClientSecret;
			}
			EditorHelper.TextField(m_sheetXSettings.googleClientId, "Google client id", 200);
			EditorHelper.TextField(m_sheetXSettings.googleClientSecret, "Google client secret", 200);
			
			GUILayout.EndVertical();
			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(m_sheetXSettings);
			if (GUILayout.Button("Reset to default settings"))
				m_sheetXSettings.ResetToDefault();
		}

		[MenuItem("Window/SheetX/Settings")]
		public static void ShowWindow()
		{
			var window = GetWindow<SheetXSettingsWindow>("Sheets Exporter Settings", true);
			window.Show();
		}
	}
}