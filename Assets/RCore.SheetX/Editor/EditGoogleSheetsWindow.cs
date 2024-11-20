/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.SheetX
{
	public class EditGoogleSheetsWindow : EditorWindow
	{
		private GoogleSheetsPath m_googleSheetsPath;
		private EditorTableView<SheetPath> m_tableSheets;
		private string m_googleClientId;
		private string m_googleClientSecret;
		private Action<GoogleSheetsPath> m_onQuit;
		private bool m_isEditing;

		public static void ShowWindow(GoogleSheetsPath googleSheetsPath, string googleClientId, string googleClientSecret, Action<GoogleSheetsPath> onQuit)
		{
			var window = CreateInstance<EditGoogleSheetsWindow>();
			window.titleContent = new GUIContent("Edit Spreadsheets");
			window.m_googleSheetsPath = googleSheetsPath;
			window.m_googleClientId = googleClientId;
			window.m_googleClientSecret = googleClientSecret;
			window.m_onQuit = onQuit;
			window.m_isEditing = !string.IsNullOrEmpty(googleSheetsPath.id);
			window.ShowUtility();
		}

		private void OnGUI()
		{
			GUILayout.BeginHorizontal("box");
			{
				GUILayout.BeginVertical();
				{
					if (!m_isEditing)
						m_googleSheetsPath.id = EditorHelper.TextField(m_googleSheetsPath.id, "Google Spreadsheets Id", 160);
					else
						EditorHelper.TextField(m_googleSheetsPath.id, "Google Spreadsheets Id", 160, readOnly: true);
					EditorHelper.TextField(m_googleSheetsPath.name, "Google Spreadsheets Name", 160, readOnly: true);
				}
				GUILayout.EndVertical();
				if (EditorHelper.Button("Download", pHeight: 41))
					SheetXHelper.DownloadGoogleSheet(m_googleClientId, m_googleClientSecret, m_googleSheetsPath);
			}
			GUILayout.EndHorizontal();
			//-----
			GUILayout.BeginVertical("box");
			m_tableSheets ??= SheetXHelper.CreateSpreadsheetTable(this);
			m_tableSheets.DrawOnGUI(m_googleSheetsPath.sheets);
			GUILayout.EndVertical();
		}

		private void OnLostFocus()
		{
			// Force window to regain focus to prevent clicking on other editor windows
			Focus();
		}

		private void OnDestroy()
		{
			if (m_googleSheetsPath.sheets.Count > 0)
				m_onQuit?.Invoke(m_googleSheetsPath);
		}
	}
}