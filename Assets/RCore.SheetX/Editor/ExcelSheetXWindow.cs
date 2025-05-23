/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using NPOI.SS.UserModel;
using Object = UnityEngine.Object;
using UnityEditor.Compilation;

namespace RCore.SheetX.Editor
{
	public class ExcelSheetXWindow
	{
		public EditorWindow editorWindow;

		private SheetXSettings m_settings;
		private ExcelSheetHandler m_excelSheetHandler;
		private EditorTableView<SheetPath> m_tableSheets;
		private EditorTableView<ExcelSheetsPath> m_tableExcelSheetsPaths;
		private IWorkbook m_workbook;

		public void OnEnable()
		{
			m_settings = SheetXSettings.Init();
			m_excelSheetHandler = new ExcelSheetHandler(m_settings);
		}

		public void OnGUI()
		{
#if SX_LITE
			PageSingleFile();
#elif SX_LOCALIZATION
			PageMultiFiles();
#else
			EditorHelper.DrawLine();
			var tab = EditorHelper.Tabs($"{nameof(ExcelSheetXWindow)}", "Export Single File", "Export Multi Files");
			switch (tab)
			{
				case "Export Single File":
					PageSingleFile();
					break;

				case "Export Multi Files":
					GUILayout.BeginVertical("box");
					PageMultiFiles();
					GUILayout.EndVertical();
					break;
			}
#endif
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

#if !SX_LOCALIZATION
		private void PageSingleFile()
		{
			GUILayout.BeginHorizontal("box");
			m_settings.excelSheetsPath.path = EditorHelper.TextField(m_settings.excelSheetsPath.path, "Excel File", 80);
			if (!string.IsNullOrEmpty(m_settings.excelSheetsPath.path))
			{
				bool validExcelPath = ValidateExcelPath(m_settings.excelSheetsPath.path, out string status);
				if (validExcelPath)
				{
					string path = m_settings.excelSheetsPath.path;
					if (!Path.IsPathRooted(path))
						path = Path.Combine(Application.dataPath.Replace("Assets", ""), path);
					var folderIcon = EditorIcon.GetIcon(EditorIcon.Icon.Folder);
					var fileIcon = EditorIcon.GetIcon(EditorIcon.Icon.DefaultAsset);
					if (EditorHelper.Button(null, folderIcon, default, 30, 20))
					{
						var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
						if (obj != null)
							Selection.activeObject = obj;
						else
						{
							var psi = new ProcessStartInfo(Path.GetDirectoryName(path));
							Process.Start(psi);
						}
					}
					if (EditorHelper.Button(null, fileIcon, default, 30, 20))
					{
						var psi = new ProcessStartInfo(path)
						{
							UseShellExecute = true
						};
						Process.Start(psi);
					}
				}
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
					// validate the top toggle
					foreach (var sheetsPath in m_settings.excelSheetsPath.sheets)
						sheetsPath.onSelected = _ => ValidateTopToggle(m_settings.excelSheetsPath.sheets, m_tableSheets);
				}
			}
			GUILayout.EndHorizontal();
			//-----
			GUILayout.BeginHorizontal();
			if (m_tableSheets == null)
			{
				m_tableSheets = SheetXHelper.CreateSpreadsheetTable(editorWindow, m_settings.excelSheetsPath.name, isOn =>
				{
					foreach (var sheet in m_settings.excelSheetsPath.sheets)
						sheet.selected = isOn;
				});
				foreach (var sheetPath in m_settings.excelSheetsPath.sheets)
					sheetPath.onSelected = _ => ValidateTopToggle(m_settings.excelSheetsPath.sheets, m_tableSheets);
				ValidateTopToggle(m_settings.excelSheetsPath.sheets, m_tableSheets);
			}
			m_tableSheets.viewWidthFillRatio = 0.8f;
			m_tableSheets.viewHeight = 250f;
			m_tableSheets.DrawOnGUI(m_settings.excelSheetsPath.sheets);

			var style = new GUIStyle("box");
			style.fixedWidth = editorWindow.position.width * 0.2f - 7;
			style.fixedHeight = 250f;
			EditorGUILayout.BeginVertical(style);
			if (EditorHelper.Button("Reload"))
			{
				if (ValidateExcelPath(m_settings.excelSheetsPath.path, out _))
					m_settings.excelSheetsPath.Load();
				else
					m_settings.excelSheetsPath.sheets.Clear();
			}
			if (EditorHelper.Button("Export All", pHeight: 40))
			{
				m_excelSheetHandler.ExportAll();
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
#if !SX_NO_LOCALIZATION
			if (EditorHelper.Button("Export Localizations", pHeight: 30))
			{
				m_excelSheetHandler.ExportLocalizations();
				CompilationPipeline.RequestScriptCompilation();
			}
#endif
			EditorGUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}
#endif

#if !SX_LITE
		private void PageMultiFiles()
		{
			GUILayout.BeginHorizontal();
			if (EditorHelper.Button("Add Excel SpreadSheets", pWidth: 200, pHeight: 30))
			{
				var path = EditorHelper.OpenFilePanel("Select Excel SpreadSheets", "xlsx");
				if (!string.IsNullOrEmpty(path))
				{
					if (path.StartsWith(Application.dataPath))
						path = EditorHelper.FormatPathToUnityPath(path);
					var excel = m_settings.AddExcelFileFile(path);
					if (excel != null)
						excel.onSelected = _ => ValidateTopToggle(m_settings.excelSheetsPaths, m_tableExcelSheetsPaths);
				}
			}
			GUILayout.FlexibleSpace();
			if (EditorHelper.Button("Export All", pWidth: 200, pHeight: 30))
			{
				m_excelSheetHandler.ExportAllFiles();
				CompilationPipeline.RequestScriptCompilation();
			}
			GUILayout.EndHorizontal();
			if (m_tableExcelSheetsPaths == null)
			{
				m_tableExcelSheetsPaths = CreateTableExcelSheetsPaths(isOn =>
				{
					foreach (var excelSheetsPath in m_settings.excelSheetsPaths)
						excelSheetsPath.selected = isOn;
				});
				foreach (var sheetsPath in m_settings.excelSheetsPaths)
					sheetsPath.onSelected = _ => ValidateTopToggle(m_settings.excelSheetsPaths, m_tableExcelSheetsPaths);
				ValidateTopToggle(m_settings.excelSheetsPaths, m_tableExcelSheetsPaths);
			}
			m_tableExcelSheetsPaths.DrawOnGUI(m_settings.excelSheetsPaths);
		}

		private EditorTableView<ExcelSheetsPath> CreateTableExcelSheetsPaths(Action<bool> pOnTogSelected)
		{
			var table = new EditorTableView<ExcelSheetsPath>(editorWindow, "Excel paths");
			var labelGUIStyle = new GUIStyle(GUI.skin.label)
			{
				padding = new RectOffset(4, 4, 0, 0)
			};
			var disabledLabelGUIStyle = new GUIStyle(labelGUIStyle)
			{
				normal = new GUIStyleState
				{
					textColor = Color.gray
				}
			};

			table.AddColumn(null, 25, 25, (rect, item) =>
				{
					rect.xMin += 4;
					item.Selected = EditorGUI.Toggle(rect, item.selected);
				})
				.ShowToggle(true)
				.OnToggleChanged(pOnTogSelected);

			table.AddColumn("Name", 100, 120, (rect, item) =>
			{
				var style = item.selected ? labelGUIStyle : disabledLabelGUIStyle;
				EditorGUI.LabelField(rect, item.name, style);
			}).SetSorting((a, b) => String.Compare(a.name, b.name, StringComparison.Ordinal));

			table.AddColumn("Excel Path", 300, 0, (rect, item) =>
			{
				var style = item.selected ? labelGUIStyle : disabledLabelGUIStyle;
				EditorGUI.LabelField(rect, item.path, style);
			}).SetSorting((a, b) => String.Compare(a.path, b.path, StringComparison.Ordinal));

			table.AddColumn("Status", 80, 100, (rect, item) =>
			{
				var defaultColor = GUI.color;
				var color = defaultColor;
				string status = "";
				if (!string.IsNullOrEmpty(item.path))
				{
					ValidateExcelPath(item.path, out status);
					color = status == "Good" ? Color.green : Color.red;
				}
				GUI.contentColor = color;
				GUI.Label(rect, status);
				GUI.contentColor = defaultColor;
			});

			table.AddColumn("Open", 60, 50, (rect, item) =>
			{
				string path = item.path;
				if (!Path.IsPathRooted(path))
					path = Path.Combine(Application.dataPath.Replace("Assets", ""), path);
				GUILayout.BeginHorizontal();
				var folderIcon = EditorIcon.GetIcon(EditorIcon.Icon.Folder);
				var r1 = rect;
				r1.width /= 2f;
				if (GUI.Button(r1, folderIcon))
				{
					var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
					if (obj != null)
						Selection.activeObject = obj;
					else
					{
						var psi = new ProcessStartInfo(Path.GetDirectoryName(path));
						Process.Start(psi);
					}
				}
				var fileIcon = EditorIcon.GetIcon(EditorIcon.Icon.Edit);
				r1.x += r1.width;
				if (GUI.Button(r1, fileIcon))
				{
					var psi = new ProcessStartInfo(path)
					{
						UseShellExecute = true
					};
					Process.Start(psi);
				}
				GUILayout.EndHorizontal();
			});

			table.AddColumn("Select", 50, 50, (rect, item) =>
			{
				if (GUI.Button(rect, $"{item.CountSelected()}/{item.sheets.Count}"))
				{
					item.Load();
					EditExcelSheetsWindow.ShowWindow(item);
				}
			}).SetTooltip("Click to Edit");

			table.AddColumn("Remove", 55, 50, (rect, item) =>
			{
				var deleteIcon = EditorIcon.GetIcon(EditorIcon.Icon.DeletedLocal);
				if (GUI.Button(rect, deleteIcon))
					m_settings.excelSheetsPaths.Remove(item);
			}).SetTooltip("Click to Delete");

			return table;
		}
#endif
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