using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using NPOI.SS;
using NPOI.SS.UserModel;
using NPOI.XSSF;
using NPOI.XSSF.UserModel;
using RCore.Editor;

namespace RCore.E2U
{
	public class E2UWindow : EditorWindow
	{
		private Vector2 m_scrollPosition;
		private E2USettings m_e2USettings;
		private EditorTableView<ExcelFile> m_tableExcelFiles;
		private EditorTableView<Spreadsheet> m_tableSpreadSheet;
		private IWorkbook m_workbook;

		private void OnEnable()
		{
			m_e2USettings = E2USettings.Load();
		}

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
			var tab = EditorHelper.Tabs($"{nameof(E2UWindow)}", "Export Excel", "Export Multi Excel");
			switch (tab)
			{
				case "Export Excel":
					GUILayout.BeginVertical("box");
					SingleExcelOnGUI();
					GUILayout.EndVertical();
					break;
				
				case "Export Multi Excel":
					GUILayout.BeginVertical("box");
					MultiExcelFilesOnGUI();
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

		private void SingleExcelOnGUI()
		{
			GUILayout.BeginHorizontal();
			m_e2USettings.excelFile.path = EditorHelper.TextField(m_e2USettings.excelFile.path, "Excel File", 100);
			bool validExcelPath = ValidateExcelPath(m_e2USettings.excelFile.path);
			if (validExcelPath)
				EditorHelper.LabelField("Good", 50, false, TextAnchor.MiddleCenter, Color.green);
			else
				EditorHelper.LabelField("Bad", 50, false, TextAnchor.MiddleCenter, Color.red);
			if (EditorHelper.Button("Select File", 100))
			{
				string path = EditorHelper.OpenFilePanel("Select File", "xlsx");
				if (!string.IsNullOrEmpty(path))
				{
					m_e2USettings.excelFile.path = path;
					m_e2USettings.excelFile.Load();
				}
			}
			GUILayout.EndHorizontal();
			//-----
			GUILayout.BeginHorizontal();
			m_tableSpreadSheet ??= CreateSpreadsheetTable();
			m_tableSpreadSheet.viewWidthFillRatio = 0.8f;
			m_tableSpreadSheet.viewHeight = 200f;
			m_tableSpreadSheet.DrawOnGUI(m_e2USettings.excelFile.sheets);
				
			var style = new GUIStyle(EditorStyles.helpBox);
			style.fixedWidth = position.width * 0.2f - 7;
			style.fixedHeight = 200f;
			EditorGUILayout.BeginVertical(style);
			if (EditorHelper.Button("Reload"))
			{
				if (ValidateExcelPath(m_e2USettings.excelFile.path))
					m_e2USettings.excelFile.Load();
			}
			if (EditorHelper.Button("Export All"))
			{
				m_e2USettings.ExportAll();
			}
			if (EditorHelper.Button("Export IDs"))
			{
				m_e2USettings.ExportIDs();
			}
			if (EditorHelper.Button("Export Constants"))
			{
				m_e2USettings.ExportConstants();
			}
			if (EditorHelper.Button("Export Json"))
			{
				m_e2USettings.ExportJson();
			}
			if (EditorHelper.Button("Export Localizations"))
			{
				m_e2USettings.ExportLocalizations();
			}
			if (EditorHelper.Button("Open Settings"))
			{
				E2USettingsWindow.ShowWindow();
			}
			EditorGUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}
		
		private void MultiExcelFilesOnGUI()
		{
			if (EditorHelper.Button("Add Excel Files", 200))
			{
				var paths = EditorHelper.OpenFilePanelWithFilters("Select Excel Files", new[] { "Excel", "xlsx" });
				foreach (string path in paths)
					m_e2USettings.AddExcelFileFile(path);
			}
			m_tableExcelFiles ??= CreateExcelTable();
			m_tableExcelFiles.DrawOnGUI(m_e2USettings.excelFiles);
			if (EditorHelper.Button("Export All"))
			{
				m_e2USettings.ExportAll();
			}
		}

		private EditorTableView<Spreadsheet> CreateSpreadsheetTable()
		{
			var table = new EditorTableView<Spreadsheet>(this, "Spreadsheets");
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
				item.name = EditorGUI.TextField(
					position: rect,
					text: item.name,
					style: style
				);
			}).SetAutoResize(true).SetSorting((a, b) => String.Compare(a.name, b.name, StringComparison.Ordinal));
			return table;
		} 
		
		private EditorTableView<ExcelFile> CreateExcelTable()
		{
			var table = new EditorTableView<ExcelFile>(this, "Excel files");
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

			table.AddColumn("Export Constants", 120, 140, (rect, item) =>
			{
				rect.xMin += 10;
				item.exportConstants = EditorGUI.Toggle(rect, item.exportConstants);
			}).SetAutoResize(true);
			
			table.AddColumn("Export IDs", 80, 100, (rect, item) =>
			{
				rect.xMin += 10;
				item.exportIDs = EditorGUI.Toggle(rect, item.exportIDs);
			}).SetAutoResize(true);

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
				var defaultColor = GUI.color;
				GUI.backgroundColor = Color.red;
				if (GUI.Button(rect, "Delete"))
				{
					m_e2USettings.excelFiles.Remove(item);
				}
				GUI.backgroundColor = defaultColor;
			}).SetAutoResize(true).SetTooltip("Click to delete");

			return table;
		}
		
		[MenuItem("Window/E2U/Excel To Unity")]
		public static void ShowWindow()
		{
			var window = GetWindow<E2UWindow>("E2U", true);
			window.Show();
		}
	}
}