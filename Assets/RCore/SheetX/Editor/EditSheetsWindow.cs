using System;
using System.Collections;
using System.Collections.Generic;
using RCore.Editor;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX
{
	public class EditSheetsWindow : EditorWindow
	{
		private Action<ExcelSheetsPath> m_onContentSaved;
		private ExcelSheetsPath m_excelSheetsPath;
		private EditorTableView<SheetPath> m_tableSpreadSheet;
		
		public static void ShowWindow(ExcelSheetsPath excelSheetsPath, Action<ExcelSheetsPath> callback)
		{
			var window = CreateInstance<EditSheetsWindow>();
			window.titleContent = new GUIContent("Edit Spreadsheets");
			window.m_excelSheetsPath = excelSheetsPath;
			window.m_onContentSaved = callback;
			window.ShowUtility();
		}

		private void OnGUI()
		{
			m_tableSpreadSheet ??= SheetXHelper.CreateSpreadsheetTable(this);
			m_tableSpreadSheet.DrawOnGUI(m_excelSheetsPath.sheets);
		}
		
		private void OnLostFocus()
		{
			// Force window to regain focus to prevent clicking on other editor windows
			Focus();
		}
	}
}