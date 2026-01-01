/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// Editor window for selecting which sheets within a Google Spreadsheet should be processed.
	/// </summary>
	public class EditGoogleSheetsWindow : EditorWindow
	{
		private GoogleSheetsPath m_googleSheetsPath;
		private EditorTableView<SheetPath> m_tableSheets;
		private string m_googleClientId;
		private string m_googleClientSecret;
		private Action<GoogleSheetsPath> m_onQuit;
		private bool m_isEditing;

		/// <summary>
		/// Opens the Edit Google Sheets window for a specific Google Sheets path configuration.
		/// </summary>
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
			if (m_tableSheets == null)
			{
				m_tableSheets = SheetXHelper.CreateSpreadsheetTable(this, m_googleSheetsPath.name, isOn =>
				{
					foreach (var sheetPath in m_googleSheetsPath.sheets)
						sheetPath.selected = isOn;
				});
				foreach (var sheet in m_googleSheetsPath.sheets)
					sheet.onSelected = _ => ValidateTopToggle(m_googleSheetsPath.sheets, m_tableSheets);
				ValidateTopToggle(m_googleSheetsPath.sheets, m_tableSheets);
			}
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
		
		private void ValidateTopToggle<T>(List<T> sheets, EditorTableView<T> tableSheets) where T : Selectable
		{
			bool selectAll = sheets.Count > 0;
			foreach (var sheet in sheets)
				if (!sheet.selected)
				{
					selectAll = false;
					break;
				}
			tableSheets.GetColumnByIndex(0).column.allowToggleVisibility = selectAll;
		}
	}
}