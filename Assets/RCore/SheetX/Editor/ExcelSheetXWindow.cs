using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using NPOI.SS.UserModel;
using RCore.Editor;

namespace RCore.SheetX
{
	public class ExcelSheetXWindow : EditorWindow
	{
		private Vector2 m_scrollPosition;
		private SheetXSettings m_settings;
		private ExcelSheetHandler m_excelSheetHandler;
		private EditorTableView<ExcelSheetsPath> m_tableExcelFiles;
		private EditorTableView<SheetPath> m_tableSpreadSheet;
		private IWorkbook m_workbook;

		private void OnEnable()
		{
			m_settings = SheetXSettings.Load();
			m_excelSheetHandler = new ExcelSheetHandler(m_settings);

		}

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
			var tab = EditorHelper.Tabs($"{nameof(ExcelSheetXWindow)}", "Export Excel Spreadsheet", "Export Multi Excel Spreadsheets");
			switch (tab)
			{
				case "Export Excel Spreadsheet":
					GUILayout.BeginVertical("box");
					PageSingleFile();
					GUILayout.EndVertical();
					break;

				case "Export Multi Excel Spreadsheets":
					GUILayout.BeginVertical("box");
					PageMultiFiles();
					GUILayout.EndVertical();
					break;
			}
			GUILayout.EndScrollView();
		}

		private bool ValidateExcelPath(string path)
		{
			string extension = Path.GetExtension(path)?.ToLower();
			if (extension != ".xlsx" || !File.Exists(path))
				return false;
			return true;
		}

		private void PageSingleFile()
		{
			GUILayout.BeginHorizontal();
			m_settings.excelSheetsPath.path = EditorHelper.TextField(m_settings.excelSheetsPath.path, "Excel File", 100);
			bool validExcelPath = ValidateExcelPath(m_settings.excelSheetsPath.path);
			if (validExcelPath)
				EditorHelper.LabelField("Good", 50, false, TextAnchor.MiddleCenter, Color.green);
			else
				EditorHelper.LabelField("Bad", 50, false, TextAnchor.MiddleCenter, Color.red);
			if (EditorHelper.Button("Select File", 100))
			{
				string directory = string.IsNullOrEmpty(m_settings.excelSheetsPath.path) ? null : Path.GetDirectoryName(m_settings.excelSheetsPath.path);
				string path = EditorHelper.OpenFilePanel("Select File", "xlsx", directory);
				if (!string.IsNullOrEmpty(path))
				{
					m_settings.excelSheetsPath.path = path;
					m_settings.excelSheetsPath.Load();
				}
			}
			GUILayout.EndHorizontal();
			//-----
			GUILayout.BeginHorizontal();
			m_tableSpreadSheet ??= SheetXHelper.CreateSpreadsheetTable(this);
			m_tableSpreadSheet.viewWidthFillRatio = 0.8f;
			m_tableSpreadSheet.viewHeight = 250f;
			m_tableSpreadSheet.DrawOnGUI(m_settings.excelSheetsPath.sheets);

			var style = new GUIStyle(EditorStyles.helpBox);
			style.fixedWidth = position.width * 0.2f - 7;
			style.fixedHeight = 250f;
			EditorGUILayout.BeginVertical(style);
			if (EditorHelper.Button("Reload"))
			{
				if (ValidateExcelPath(m_settings.excelSheetsPath.path))
					m_settings.excelSheetsPath.Load();
			}
			if (EditorHelper.Button("Export All", pHeight: 40))
				m_excelSheetHandler.ExportAll();
			if (EditorHelper.Button("Export IDs", pHeight: 30))
				m_excelSheetHandler.ExportIDs();
			if (EditorHelper.Button("Export Constants", pHeight: 30))
				m_excelSheetHandler.ExportConstants();
			if (EditorHelper.Button("Export Json", pHeight: 30))
				m_excelSheetHandler.ExportJson();
			if (EditorHelper.Button("Export Localizations", pHeight: 30))
				m_excelSheetHandler.ExportLocalizations();
			if (EditorHelper.Button("Open Settings", pHeight: 30))
				SheetXSettingsWindow.ShowWindow();
			EditorGUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}

		private void PageMultiFiles()
		{
			GUILayout.BeginHorizontal();
			if (EditorHelper.Button("Add Excel Files", pWidth: 200, pHeight: 30))
			{
				var paths = EditorHelper.OpenFilePanelWithFilters("Select Excel Files", new[] { "Excel", "xlsx" });
				for (int i = 0; i < paths.Count; i++)
				{
					if (paths[i].StartsWith(Application.dataPath))
						paths[i] = EditorHelper.FormatPathToUnityPath(paths[i]);
					m_settings.AddExcelFileFile(paths[i]);
				}
			}
			GUILayout.FlexibleSpace();
			if (EditorHelper.Button("Export All", pWidth: 200, pHeight: 30))
			{
				
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
			m_tableExcelFiles ??= CreateExcelTable();
			m_tableExcelFiles.DrawOnGUI(m_settings.excelSheetsPaths);

		}

		private EditorTableView<ExcelSheetsPath> CreateExcelTable()
		{
			var table = new EditorTableView<ExcelSheetsPath>(this, "Excel files");
			var labelGUIStyle = new GUIStyle(GUI.skin.label)
			{
				padding = new RectOffset(left: 10, right: 10, top: 2, bottom: 2)
			};
			var disabledLabelGUIStyle = new GUIStyle(labelGUIStyle)
			{
				normal = new GUIStyleState
				{
					textColor = Color.gray
				}
			};
			table.AddColumn("Selected", 70, 90, (rect, item) =>
			{
				rect.xMin += 10;
				item.selected = EditorGUI.Toggle(rect, item.selected);
			}).SetAutoResize(true);

			table.AddColumn("Excel Path", 300, 0, (rect, item) =>
			{
				var style = item.selected ? labelGUIStyle : disabledLabelGUIStyle;
				item.path = EditorGUI.TextField(
					position: rect,
					text: item.path,
					style: style
				);
			}).SetAutoResize(true).SetSorting((a, b) => String.Compare(a.path, b.path, StringComparison.Ordinal));

			table.AddColumn("Status", 80, 100, (rect, item) =>
			{
				var defaultColor = GUI.color;
				var color = defaultColor;
				string status = "";
				if (!string.IsNullOrEmpty(item.path))
				{
					string extension = Path.GetExtension(item.path)?.ToLower();
					bool fileExists = File.Exists(item.path);
					bool valid = extension == ".xlsx" && fileExists;
					if (valid)
					{
						status = "Good";
						color = Color.green;
					}
					else
					{
						status = fileExists ? "File not found" : "File is invalid";
						color = Color.red;
					}
				}
				GUI.contentColor = color;
				GUI.Label(rect, status);
				GUI.contentColor = defaultColor;
			}).SetAutoResize(true);

			table.AddColumn("Edit", 60, 100, (rect, item) =>
			{
				if (GUI.Button(rect, "Edit"))
				{
					item.Load();
					EditSheetsWindow.ShowWindow(item, result => { });
				}
			}).SetAutoResize(true).SetTooltip("Click to Edit");

			table.AddColumn("Delete", 60, 100, (rect, item) =>
			{
				var defaultColor = GUI.color;
				GUI.backgroundColor = Color.red;
				if (GUI.Button(rect, "Delete"))
				{
					m_settings.excelSheetsPaths.Remove(item);
				}
				GUI.backgroundColor = defaultColor;
			}).SetAutoResize(true).SetTooltip("Click to Delete");

			return table;
		}

		[MenuItem("Window/SheetX/Excel Sheets Exporter")]
		public static void ShowWindow()
		{
			var window = GetWindow<ExcelSheetXWindow>("Excel Sheets Exporter", true);
			window.Show();
		}
	}
}