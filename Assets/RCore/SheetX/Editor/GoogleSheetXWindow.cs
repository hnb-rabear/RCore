using RCore.Editor;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX
{
	public class GoogleSheetXWindow : EditorWindow
	{
		private Vector2 m_scrollPosition;
		private SheetXSettings m_settings;
		private GoogleSheetHandler m_googleSheetHandler;
		private EditorTableView<SheetPath> m_tableSpreadSheet;

		private void OnEnable()
		{
			m_settings = SheetXSettings.Load();
			m_googleSheetHandler = new GoogleSheetHandler(m_settings);
		}

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
			var tab = EditorHelper.Tabs($"{nameof(GoogleSheetXWindow)}", "Export Google Spreadsheet", "Export Multi Google Spreadsheets");
			switch (tab)
			{
				case "Export Google Spreadsheet":
					GUILayout.BeginVertical("box");
					PageSingleFile();
					GUILayout.EndVertical();
					break;

				case "Export Multi Google Spreadsheets":
					GUILayout.BeginVertical("box");
					PageMultiFiles();
					GUILayout.EndVertical();
					break;
			}
			GUILayout.EndScrollView();
		}

		private void PageSingleFile()
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.BeginVertical();
				{
					m_settings.googleSheetsPath.id = EditorHelper.TextField(m_settings.googleSheetsPath.id, "Google Spreadsheets Id", 160);
					EditorHelper.TextField(m_settings.googleSheetsPath.name, "Google Spreadsheets Name", 160, readOnly: true);
				}
				GUILayout.EndVertical();
				if (EditorHelper.Button("Download", pHeight: 41))
					m_googleSheetHandler.Download(m_settings.googleSheetsPath);
			}
			GUILayout.EndHorizontal();
			//-----
			GUILayout.BeginHorizontal();
			m_tableSpreadSheet ??= SheetXHelper.CreateSpreadsheetTable(this);
			m_tableSpreadSheet.viewWidthFillRatio = 0.8f;
			m_tableSpreadSheet.viewHeight = 250f;
			m_tableSpreadSheet.DrawOnGUI(m_settings.googleSheetsPath.sheets);

			var style = new GUIStyle(EditorStyles.helpBox);
			style.fixedWidth = position.width * 0.2f - 7;
			style.fixedHeight = 250f;
			EditorGUILayout.BeginVertical(style);
			if (EditorHelper.Button("Export All", pHeight: 40))
				m_googleSheetHandler.ExportAll();
			if (EditorHelper.Button("Export IDs", pHeight: 30))
				m_googleSheetHandler.ExportIDs();
			if (EditorHelper.Button("Export Constants", pHeight: 30))
				m_googleSheetHandler.ExportConstants();
			if (EditorHelper.Button("Export Json", pHeight: 30))
				m_googleSheetHandler.ExportJson();
			if (EditorHelper.Button("Export Localizations", pHeight: 30))
				m_googleSheetHandler.ExportLocalizations();
			if (EditorHelper.Button("Open Settings", pHeight: 30))
				SheetXSettingsWindow.ShowWindow();
			EditorGUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}

		private void PageMultiFiles() { }

		[MenuItem("Window/SheetX/Google Sheets Exporter")]
		public static void ShowWindow()
		{
			var window = GetWindow<GoogleSheetXWindow>("Google Sheets Exporter", true);
			window.Show();
		}
	}
}