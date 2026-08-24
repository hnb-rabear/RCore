/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// Editor window for selecting which sheets within an Excel file should be processed.
	/// </summary>
	public class EditExcelSheetsWindow : EditorWindow
	{
		private ExcelSheetsPath m_excelSheetsPath;
		private SheetXSettings m_settings;
		private EditorTableView<SheetPath> m_tableSheets;
		private bool m_tableCollectionsEnabled;
		private string m_tableSourceId;
		
		/// <summary>
		/// Opens the Edit Excel Sheets window for a specific Excel path.
		/// </summary>
		public static void ShowWindow(ExcelSheetsPath excelSheetsPath)
		{
			var window = CreateInstance<EditExcelSheetsWindow>();
			window.titleContent = new GUIContent("Edit Spreadsheets");
			window.m_excelSheetsPath = excelSheetsPath;
			window.m_settings = SheetXSettings.Init();
			window.ShowUtility();
		}

		private void OnGUI()
		{
			string sourceId = m_excelSheetsPath.path;
			if (m_tableSheets == null
				|| m_tableCollectionsEnabled != m_settings.enableCollections
				|| !string.Equals(m_tableSourceId, sourceId, System.StringComparison.Ordinal))
			{
				m_tableSheets = SheetXHelper.CreateSpreadsheetTable(this, m_excelSheetsPath.name, isOn =>
				{
					foreach (var sheetPath in m_excelSheetsPath.sheets)
						sheetPath.selected = isOn;
				}, m_settings, () => m_excelSheetsPath.path);
				m_tableCollectionsEnabled = m_settings.enableCollections;
				m_tableSourceId = sourceId;
				foreach (var sheet in m_excelSheetsPath.sheets)
					sheet.onSelected = _ => ValidateTopToggle(m_excelSheetsPath.sheets, m_tableSheets);
				ValidateTopToggle(m_excelSheetsPath.sheets, m_tableSheets);
			}
			m_tableSheets.DrawOnGUI(m_excelSheetsPath.sheets);
		}
		
		private void OnLostFocus()
		{
			// Force window to regain focus to prevent clicking on other editor windows
			Focus();
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