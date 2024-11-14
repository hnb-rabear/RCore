using UnityEditor;
using UnityEngine;

namespace RCore.Editor.SheetX
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
			m_tableSheets ??= SheetXHelper.CreateSpreadsheetTable(this);
			m_tableSheets.DrawOnGUI(m_excelSheetsPath.sheets);
		}
		
		private void OnLostFocus()
		{
			// Force window to regain focus to prevent clicking on other editor windows
			Focus();
		}
	}
}