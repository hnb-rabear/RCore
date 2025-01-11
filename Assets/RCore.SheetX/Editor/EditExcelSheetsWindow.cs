/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	public class EditExcelSheetsWindow : EditorWindow
	{
		private ExcelSheetsPath m_excelSheetsPath;
		private EditorTableView<SheetPath> m_tableSheets;
		
		public static void ShowWindow(ExcelSheetsPath excelSheetsPath)
		{
			var window = CreateInstance<EditExcelSheetsWindow>();
			window.titleContent = new GUIContent("Edit Spreadsheets");
			window.m_excelSheetsPath = excelSheetsPath;
			window.ShowUtility();
		}

		private void OnGUI()
		{
			if (m_tableSheets == null)
			{
				m_tableSheets = SheetXHelper.CreateSpreadsheetTable(this, m_excelSheetsPath.name, isOn =>
				{
					foreach (var sheetPath in m_excelSheetsPath.sheets)
						sheetPath.selected = isOn;
				});
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