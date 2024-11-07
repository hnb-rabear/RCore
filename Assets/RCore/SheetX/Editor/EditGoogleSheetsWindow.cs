using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;
using RCore.Editor;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX
{
	public class EditGoogleSheetsWindow : EditorWindow
	{
		private GoogleSheetsPath m_googleSheetsPath;
		private EditorTableView<SheetPath> m_tableSheets;
		private string m_googleClientId;
		private string m_googleClientSecret;

		public static void ShowWindow(GoogleSheetsPath googleSheetsPath, string googleClientId, string googleClientSecret)
		{
			var window = CreateInstance<EditGoogleSheetsWindow>();
			window.titleContent = new GUIContent("Edit Spreadsheets");
			window.m_googleSheetsPath = googleSheetsPath;
			window.m_googleClientId = googleClientId;
			window.m_googleClientSecret = googleClientSecret;
			window.ShowUtility();
		}

		private void OnGUI()
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.BeginVertical();
				{
					m_googleSheetsPath.id = EditorHelper.TextField(m_googleSheetsPath.id, "Google Spreadsheets Id", 160);
					EditorHelper.TextField(m_googleSheetsPath.name, "Google Spreadsheets Name", 160, readOnly: true);
				}
				GUILayout.EndVertical();
				if (EditorHelper.Button("Download", pHeight: 41))
					SheetXHelper.DownloadGoogleSheet(m_googleClientId, m_googleClientSecret, m_googleSheetsPath);
			}
			GUILayout.EndHorizontal();
			//-----
			m_tableSheets ??= SheetXHelper.CreateSpreadsheetTable(this);
			m_tableSheets.viewWidthFillRatio = 0.8f;
			m_tableSheets.viewHeight = 250f;
			m_tableSheets.DrawOnGUI(m_googleSheetsPath.sheets);
		}

		private void OnLostFocus()
		{
			// Force window to regain focus to prevent clicking on other editor windows
			Focus();
		}
	}
}