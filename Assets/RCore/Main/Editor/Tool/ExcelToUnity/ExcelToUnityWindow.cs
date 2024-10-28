using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Tool.ExcelToUnity
{
	public class ExcelToUnityWindow : EditorWindow
	{
		private Vector2 m_scrollPosition;
		private ExcelToUnitySettings m_excelToUnitySettings;
		private SimpleEditorTableView<ExcelFile> m_tableExcelFiles;
		private SimpleEditorTableView<Spreadsheet> m_tableSpreadSheet;

		private void OnEnable()
		{
			m_excelToUnitySettings = ExcelToUnitySettings.Load();
		}

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
			var tab = EditorHelper.Tabs($"{nameof(ExcelToUnityWindow)}", "Export Excel", "Export Multi Excel");
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
			m_excelToUnitySettings.excelFile.path = EditorHelper.TextField(m_excelToUnitySettings.excelFile.path, "Excel File", 100);
			bool validExcelPath = ValidateExcelPath(m_excelToUnitySettings.excelFile.path);
			if (validExcelPath)
				EditorHelper.LabelField("Good", 50, false, TextAnchor.MiddleCenter, Color.green);
			else
				EditorHelper.LabelField("Bad", 50, false, TextAnchor.MiddleCenter, Color.red);
			if (EditorHelper.Button("Select File", 100))
			{
				string path = EditorUtility.OpenFilePanel("Select File", Application.dataPath, "xlsx");
				if (!string.IsNullOrEmpty(path))
					m_excelToUnitySettings.excelFile.path = path;
			}
			GUILayout.EndHorizontal();
			if (validExcelPath)
			{
				m_tableSpreadSheet ??= CreateSpreadsheetTable();
				m_tableSpreadSheet.DrawTableGUI(m_excelToUnitySettings.excelFile.sheets, viewWidth:370, viewHeight:370);
				m_tableSpreadSheet.DrawTableGUI(m_excelToUnitySettings.excelFile.sheets, viewWidth:370, viewHeight:370);
				m_tableSpreadSheet.DrawTableGUI(m_excelToUnitySettings.excelFile.sheets, viewWidth:370, viewHeight:370);
			}
		}
		
		private void MultiExcelFilesOnGUI()
		{
			if (EditorHelper.Button("Add Excel Files", 200))
			{
				var paths = EditorHelper.OpenFilePanelWithFilters("Select Excel Files", new[] { "Excel", "xlsx" });
				foreach (string path in paths)
					m_excelToUnitySettings.AddExcelFileFile(path);
			}
			m_tableExcelFiles ??= CreateExcelTable();
			m_tableExcelFiles.DrawTableGUI(m_excelToUnitySettings.excelFiles);
			if (EditorHelper.Button("Export All"))
			{
				m_excelToUnitySettings.ExportAll();
			}
		}

		private SimpleEditorTableView<Spreadsheet> CreateSpreadsheetTable()
		{
			var table = new SimpleEditorTableView<Spreadsheet>();
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
		
		private SimpleEditorTableView<ExcelFile> CreateExcelTable()
		{
			var table = new SimpleEditorTableView<ExcelFile>();
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
					m_excelToUnitySettings.excelFiles.Remove(item);
				}
				GUI.backgroundColor = defaultColor;
			}).SetAutoResize(true).SetTooltip("Click to delete");

			return table;
		}
		
		[MenuItem("Window/Excel To Unity")]
		public static void ShowWindow()
		{
			var window = GetWindow<ExcelToUnityWindow>("Excel To Unity", true);
			window.Show();
		}
	}
}