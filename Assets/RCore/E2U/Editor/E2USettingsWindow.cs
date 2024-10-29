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
			m_e2USettings.constantsOutputFolder = EditorHelper.FolderField(m_e2USettings.constantsOutputFolder, "Constants output folder", false);
			m_e2USettings.jsonOutputFolder = EditorHelper.FolderField(m_e2USettings.jsonOutputFolder, "Json output folder", false);
			m_e2USettings.localizationOutputFolder = EditorHelper.FolderField(m_e2USettings.localizationOutputFolder, "Localization output folder", false);
			// m_e2USettings.constantsOutputFolder = EditorHelper.TextField(m_e2USettings.constantsOutputFolder, "Constants output folder", 160);
			// m_e2USettings.constantsOutputFolder = EditorHelper.TextField(m_e2USettings.constantsOutputFolder, "Constants output folder", 160);
		}

		[MenuItem("Window/E2U/Settings")]
		public static void ShowWindow()
		{
			var window = GetWindow<E2USettingsWindow>("E2U Settings", true);
			window.Show();
		}
	}
}