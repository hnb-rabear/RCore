/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using NPOI.SS.UserModel;
using Object = UnityEngine.Object;
using UnityEditor.Compilation;

namespace RCore.Editor.SheetX
{
	public class ExcelSheetXWindow : EditorWindow
	{
		private Vector2 m_scrollPosition;
		private SheetXSettings m_settings;
		private ExcelSheetHandler m_excelSheetHandler;
		private EditorTableView<SheetPath> m_tableSheets;
		private EditorTableView<ExcelSheetsPath> m_tableExcelSheetsPaths;
		private IWorkbook m_workbook;

		private void OnEnable()
		{
			m_settings = SheetXSettings.Load();
			m_excelSheetHandler = new ExcelSheetHandler(m_settings);
		}

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
			PageSingleFile();
			GUILayout.EndScrollView();
		}

		private bool ValidateExcelPath(string path, out string status)
		{
			string extension = Path.GetExtension(path)?.ToLower();
			if (extension != ".xlsx")
			{
				status = "Not Excel";
				return false;
			}
			if (!File.Exists(path))
			{
				status = "Not found";
				return false;
			}
			status = "Good";
			return true;
		}

		private void PageSingleFile()
		{
			GUILayout.BeginHorizontal("box");
			m_settings.excelSheetsPath.path = EditorHelper.TextField(m_settings.excelSheetsPath.path, "Excel File", 100);
			if (!string.IsNullOrEmpty(m_settings.excelSheetsPath.path))
			{
				bool validExcelPath = ValidateExcelPath(m_settings.excelSheetsPath.path, out string status);
				if (validExcelPath)
					EditorHelper.LabelField(status, 50, false, TextAnchor.MiddleCenter, Color.green);
				else
					EditorHelper.LabelField(status, 50, false, TextAnchor.MiddleCenter, Color.red);
				m_settings.excelSheetsPath.name = Path.GetFileNameWithoutExtension(m_settings.excelSheetsPath.path);
			}
			if (EditorHelper.Button("Select File", 100))
			{
				string directory = string.IsNullOrEmpty(m_settings.excelSheetsPath.path) ? null : Path.GetDirectoryName(m_settings.excelSheetsPath.path);
				string path = EditorHelper.OpenFilePanel("Select File", "xlsx", directory);
				if (!string.IsNullOrEmpty(path))
				{
					if (path.StartsWith(Application.dataPath))
						path = EditorHelper.FormatPathToUnityPath(path);
					m_settings.excelSheetsPath.path = path;
					m_settings.excelSheetsPath.Load();
				}
			}
			GUILayout.EndHorizontal();
			//-----
			GUILayout.BeginHorizontal();
			m_tableSheets ??= SheetXHelper.CreateSpreadsheetTable(this);
			m_tableSheets.viewWidthFillRatio = 0.8f;
			m_tableSheets.viewHeight = 250f;
			m_tableSheets.DrawOnGUI(m_settings.excelSheetsPath.sheets);

			var style = new GUIStyle("box");
			style.fixedWidth = position.width * 0.2f - 7;
			style.fixedHeight = 250f;
			EditorGUILayout.BeginVertical(style);
			if (EditorHelper.Button("Reload"))
			{
				if (ValidateExcelPath(m_settings.excelSheetsPath.path, out _))
					m_settings.excelSheetsPath.Load();
			}
			if (EditorHelper.Button("Export All", pHeight: 40))
			{
				m_excelSheetHandler.ExportExcelAll();
				CompilationPipeline.RequestScriptCompilation();
			}
			if (EditorHelper.Button("Export IDs", pHeight: 30))
			{
				m_excelSheetHandler.ExportIDs();
				CompilationPipeline.RequestScriptCompilation();
			}
			if (EditorHelper.Button("Export Constants", pHeight: 30))
			{
				m_excelSheetHandler.ExportConstants();
				CompilationPipeline.RequestScriptCompilation();
			}
			if (EditorHelper.Button("Export Json", pHeight: 30))
				m_excelSheetHandler.ExportJson();
			if (EditorHelper.Button("Export Localizations", pHeight: 30))
			{
				m_excelSheetHandler.ExportLocalizations();
				CompilationPipeline.RequestScriptCompilation();
			}
			if (EditorHelper.Button("Open Settings", pHeight: 30))
				SheetXSettingsWindow.ShowWindow();
			EditorGUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}

		public static void ShowWindow()
		{
			var window = GetWindow<ExcelSheetXWindow>("Excel Sheets Exporter", true);
			window.Show();
		}
	}
}