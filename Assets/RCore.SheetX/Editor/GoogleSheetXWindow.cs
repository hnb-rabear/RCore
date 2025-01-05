/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Diagnostics;
using RCore.Editor;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	public class GoogleSheetXWindow
	{
		public EditorWindow editorWindow;
		
		private SheetXSettings m_settings;
		private GoogleSheetHandler m_googleSheetHandler;
		private EditorTableView<SheetPath> m_tableSheets;
		private EditorTableView<GoogleSheetsPath> m_tableGoogleSheetsPaths;

		public void OnEnable()
		{
			m_settings = SheetXSettings.Init();
			m_googleSheetHandler = new GoogleSheetHandler(m_settings);
		}

		public void OnGUI()
		{
#if SX_LITE
			PageSingleFile();
#elif SX_LOCALIZATION
			PageMultiFiles();
#else
			EditorHelper.DrawLine();
			var tab = EditorHelper.Tabs($"{nameof(GoogleSheetXWindow)}", "Export Single File", "Export Multi Files");
			switch (tab)
			{
				case "Export Single File":
					PageSingleFile();
					break;

				case "Export Multi Files":
					EditorGUILayout.BeginVertical("box");
					PageMultiFiles();
					EditorGUILayout.EndVertical();
					break;
			}
#endif
		}

#if !SX_LOCALIZATION
		private void PageSingleFile()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.BeginVertical();
				{
					m_settings.googleSheetsPath.id = EditorHelper.TextField(m_settings.googleSheetsPath.id, "Google Spreadsheets Id", 160);
					EditorGUILayout.BeginHorizontal();
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
					EditorGUILayout.EndHorizontal();
				}
				EditorGUILayout.EndVertical();
				if (EditorHelper.Button("Download", pHeight: 41))
					SheetXHelper.DownloadGoogleSheet(m_settings.ObfGoogleClientId, m_settings.ObfGoogleClientSecret, m_settings.googleSheetsPath);
			}
			EditorGUILayout.EndVertical();
			if (string.IsNullOrEmpty(m_settings.ObfGoogleClientId) || string.IsNullOrEmpty(m_settings.ObfGoogleClientSecret))
			{
				// Custom error message without border and default HelpBox style
				GUIStyle customStyle = new GUIStyle(GUI.skin.label);
				customStyle.normal.textColor = Color.red; // Set text color to red
				customStyle.fontSize = 12; // Set font size if needed
				customStyle.wordWrap = true; // Enable word wrap if needed
				customStyle.alignment = TextAnchor.MiddleCenter;
				GUILayout.Label("Google Client ID or Client Secret is missing.", customStyle);
			}
			EditorGUILayout.EndHorizontal();
			//-----
			EditorGUILayout.BeginHorizontal();
			m_tableSheets ??= SheetXHelper.CreateSpreadsheetTable(editorWindow, m_settings.googleSheetsPath.name);
			m_tableSheets.viewWidthFillRatio = 0.8f;
			m_tableSheets.viewHeight = 250f;
			m_tableSheets.DrawOnGUI(m_settings.googleSheetsPath.sheets);

			var style = new GUIStyle("box");
			style.fixedWidth = editorWindow.position.width * 0.2f - 7;
			style.fixedHeight = 250f;
			EditorGUILayout.BeginVertical(style);
			if (EditorHelper.Button("Export All", pHeight: 40))
			{
				m_googleSheetHandler.ExportAll();
				CompilationPipeline.RequestScriptCompilation();
			}
			if (EditorHelper.Button("Export IDs", pHeight: 30))
			{
				m_googleSheetHandler.ExportIDs();
				CompilationPipeline.RequestScriptCompilation();
			}
			if (EditorHelper.Button("Export Constants", pHeight: 30))
			{
				m_googleSheetHandler.ExportConstants();
				CompilationPipeline.RequestScriptCompilation();
			}
			if (EditorHelper.Button("Export Json", pHeight: 30))
				m_googleSheetHandler.ExportJson();
			if (EditorHelper.Button("Export Localizations", pHeight: 30))
			{
				m_googleSheetHandler.ExportLocalizations();
				CompilationPipeline.RequestScriptCompilation();
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
		}
#endif

#if !SX_LITE
		private void PageMultiFiles()
		{
			EditorGUILayout.BeginHorizontal();
			if (EditorHelper.Button("Add Google SpreadSheets", pWidth: 200, pHeight: 30))
			{
				EditGoogleSheetsWindow.ShowWindow(new GoogleSheetsPath(), m_settings.ObfGoogleClientId, m_settings.ObfGoogleClientSecret, output =>
				{
					if (!m_settings.googleSheetsPaths.Exists(x => x.id == output.id))
						m_settings.googleSheetsPaths.Add(output);
				});
			}
			GUILayout.FlexibleSpace();
			if (EditorHelper.Button("Export All", pWidth: 200, pHeight: 30))
			{
				m_googleSheetHandler.ExportAllFiles();
				CompilationPipeline.RequestScriptCompilation();
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space(8);
			
			if (string.IsNullOrEmpty(m_settings.ObfGoogleClientId) || string.IsNullOrEmpty(m_settings.ObfGoogleClientSecret))
			{
				// Custom error message without border and default HelpBox style
				GUIStyle customStyle = new GUIStyle(GUI.skin.label);
				customStyle.normal.textColor = Color.red; // Set text color to red
				customStyle.fontSize = 12; // Set font size if needed
				customStyle.wordWrap = true; // Enable word wrap if needed
				customStyle.alignment = TextAnchor.MiddleCenter;
				GUILayout.Label("Google Client ID or Client Secret is missing.", customStyle);
			}
			
			m_tableGoogleSheetsPaths ??= CreateTableGoogleSheetsPath();
			m_tableGoogleSheetsPaths.DrawOnGUI(m_settings.googleSheetsPaths);
		}

		private EditorTableView<GoogleSheetsPath> CreateTableGoogleSheetsPath()
		{
			var table = new EditorTableView<GoogleSheetsPath>(editorWindow, "Google Spreadsheets paths");
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
					EditGoogleSheetsWindow.ShowWindow(item, m_settings.ObfGoogleClientId, m_settings.ObfGoogleClientSecret, output =>
					{
						if (!m_settings.googleSheetsPaths.Exists(x => x.id == output.id))
							m_settings.googleSheetsPaths.Add(output);
					});
				}
			}).SetTooltip("Click to Edit");

			table.AddColumn("Delete", 60, 60, (rect, item) =>
			{
				var deleteIcon = EditorIcon.GetIcon(EditorIcon.Icon.DeletedLocal);
				if (GUI.Button(rect, deleteIcon))
					m_settings.googleSheetsPaths.Remove(item);
			}).SetTooltip("Click to Delete");

			return table;
		}
#endif
	}
}