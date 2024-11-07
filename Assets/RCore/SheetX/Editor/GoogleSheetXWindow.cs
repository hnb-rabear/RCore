using System;
using System.Diagnostics;
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
		private EditorTableView<SheetPath> m_tableSheets;
		private EditorTableView<GoogleSheetsPath> m_tableGoogleSheetsPaths;

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
					PageSingleFile();
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
			GUILayout.BeginHorizontal("box");
			{
				GUILayout.BeginVertical();
				{
					m_settings.googleSheetsPath.id = EditorHelper.TextField(m_settings.googleSheetsPath.id, "Google Spreadsheets Id", 160);
					GUILayout.BeginHorizontal();
					{
						EditorHelper.TextField(m_settings.googleSheetsPath.name, "Google Spreadsheets Name", 160, readOnly: true);
						if (!string.IsNullOrEmpty(m_settings.googleSheetsPath.name))
						{
							if (EditorHelper.Button("Ping", 50))
							{
								string url = $"https://docs.google.com/spreadsheets/d/{m_settings.googleSheetsPath.id}/edit";
								Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
							}
						}
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.EndVertical();
				if (EditorHelper.Button("Download", pHeight: 41))
					SheetXHelper.DownloadGoogleSheet(m_settings.googleClientId, m_settings.googleClientSecret, m_settings.googleSheetsPath);
			}
			GUILayout.EndHorizontal();
			//-----
			GUILayout.BeginHorizontal();
			m_tableSheets ??= SheetXHelper.CreateSpreadsheetTable(this);
			m_tableSheets.viewWidthFillRatio = 0.8f;
			m_tableSheets.viewHeight = 250f;
			m_tableSheets.DrawOnGUI(m_settings.googleSheetsPath.sheets);

			var style = new GUIStyle("box");
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

		private void PageMultiFiles()
		{
			GUILayout.BeginHorizontal();
			if (EditorHelper.Button("Add Google SpreadSheets", pWidth: 200, pHeight: 30))
			{
				EditGoogleSheetsWindow.ShowWindow(new GoogleSheetsPath(), m_settings.googleClientId, m_settings.googleClientSecret, output =>
				{
					if (!m_settings.googleSheetsPaths.Exists(x => x.id == output.id))
						m_settings.googleSheetsPaths.Add(output);
				});
			}
			GUILayout.FlexibleSpace();
			if (EditorHelper.Button("Export All", pWidth: 200, pHeight: 30))
				m_googleSheetHandler.ExportExcelsAll();
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
			m_tableGoogleSheetsPaths ??= CreateTableGoogleSheetsPath();
			m_tableGoogleSheetsPaths.DrawOnGUI(m_settings.googleSheetsPaths);
		}

		private EditorTableView<GoogleSheetsPath> CreateTableGoogleSheetsPath()
		{
			var table = new EditorTableView<GoogleSheetsPath>(this, "Google Spreadsheets paths");
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

			table.AddColumn("Selected", 60, 60, (rect, item) =>
			{
				rect.xMin += 10;
				item.selected = EditorGUI.Toggle(rect, item.selected);
			});

			table.AddColumn("Name", 100, 150, (rect, item) =>
			{
				var style = item.selected ? labelGUIStyle : disabledLabelGUIStyle;
				EditorGUI.LabelField(rect, item.name, style);
			}).SetSorting((a, b) => String.Compare(a.id, b.id, StringComparison.Ordinal));

			table.AddColumn("Id", 300, 0, (rect, item) =>
			{
				var style = item.selected ? labelGUIStyle : disabledLabelGUIStyle;
				EditorGUI.LabelField(rect, item.id, style);
			});

			table.AddColumn("Ping", 50, 50, (rect, item) =>
			{
				if (GUI.Button(rect, "Ping"))
				{
					string url = $"https://docs.google.com/spreadsheets/d/{item.id}/edit"; 
					Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
				}
			});

			table.AddColumn("Edit", 50, 50, (rect, item) =>
			{
				if (GUI.Button(rect, "Edit"))
				{
					EditGoogleSheetsWindow.ShowWindow(item, m_settings.googleClientId, m_settings.googleClientSecret, output =>
					{
						if (!m_settings.googleSheetsPaths.Exists(x => x.id == output.id))
							m_settings.googleSheetsPaths.Add(output);
					});
				}
			}).SetTooltip("Click to Edit");

			table.AddColumn("Delete", 60, 60, (rect, item) =>
			{
				var defaultColor = GUI.color;
				GUI.backgroundColor = Color.red;
				if (GUI.Button(rect, "Delete"))
					m_settings.googleSheetsPaths.Remove(item);
				GUI.backgroundColor = defaultColor;
			}).SetTooltip("Click to Delete");

			return table;
		}

		[MenuItem("Window/SheetX/Google Sheets Exporter")]
		public static void ShowWindow()
		{
			var window = GetWindow<GoogleSheetXWindow>("Google Sheets Exporter", true);
			window.Show();
		}
	}
}