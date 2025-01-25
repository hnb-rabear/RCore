/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
							var fileIcon = EditorIcon.GetIcon(EditorIcon.Icon.DefaultAsset);
							if (EditorHelper.Button(null, fileIcon, default, 30, 20))
								m_settings.googleSheetsPath.OpenFile();
						}
					}
					EditorGUILayout.EndHorizontal();
				}
				EditorGUILayout.EndVertical();
				if (EditorHelper.Button("Download", pHeight: 41))
				{
					SheetXHelper.DownloadGoogleSheet(m_settings.ObfGoogleClientId, m_settings.ObfGoogleClientSecret, m_settings.googleSheetsPath);
					// validate the top toggle
					foreach (var sheetsPath in m_settings.googleSheetsPath.sheets)
						sheetsPath.onSelected = _ => ValidateTopToggle(m_settings.googleSheetsPath.sheets, m_tableSheets);
				}
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
			if (m_tableSheets == null)
			{
				m_tableSheets = SheetXHelper.CreateSpreadsheetTable(editorWindow, m_settings.googleSheetsPath.name, isOn =>
				{
					foreach (var sheetPath in m_settings.googleSheetsPath.sheets)
						sheetPath.selected = isOn;
				});
				foreach (var sheetPath in m_settings.googleSheetsPath.sheets)
					sheetPath.onSelected = _ => ValidateTopToggle(m_settings.googleSheetsPath.sheets, m_tableSheets);
				ValidateTopToggle(m_settings.googleSheetsPath.sheets, m_tableSheets);
			}
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
#if !SX_NO_LOCALIZATION
			if (EditorHelper.Button("Export Localizations", pHeight: 30))
			{
				m_googleSheetHandler.ExportLocalizations();
				CompilationPipeline.RequestScriptCompilation();
			}
#endif
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
					{
						m_settings.googleSheetsPaths.Add(output);
						output.onSelected = _ => ValidateTopToggle(m_settings.googleSheetsPaths, m_tableGoogleSheetsPaths);
					}
				});
			}
			GUILayout.FlexibleSpace();
			if (EditorHelper.Button("Export All", pWidth: 200, pHeight: 30))
			{
				m_googleSheetHandler.ExportAllFiles();
				CompilationPipeline.RequestScriptCompilation();
			}
			EditorGUILayout.EndHorizontal();

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
			if (m_tableGoogleSheetsPaths == null)
			{
				m_tableGoogleSheetsPaths = CreateTableGoogleSheetsPath(isOn =>
				{
					foreach (var googleSheetsPath in m_settings.googleSheetsPaths)
						googleSheetsPath.selected = isOn;
				});
				foreach (var sheetsPath in m_settings.googleSheetsPaths)
					sheetsPath.onSelected = _ => ValidateTopToggle(m_settings.googleSheetsPaths, m_tableGoogleSheetsPaths);
				ValidateTopToggle(m_settings.googleSheetsPaths, m_tableGoogleSheetsPaths);
			}
			m_tableGoogleSheetsPaths.DrawOnGUI(m_settings.googleSheetsPaths);
		}

		private EditorTableView<GoogleSheetsPath> CreateTableGoogleSheetsPath(Action<bool> pOnTogSelected)
		{
			var table = new EditorTableView<GoogleSheetsPath>(editorWindow, "Google Spreadsheets paths");
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

			table.AddColumn("Open", 50, 50, (rect, item) =>
			{
				var fileIcon = EditorIcon.GetIcon(EditorIcon.Icon.Edit);
				if (GUI.Button(rect, fileIcon))
					item.OpenFile();
			});

			table.AddColumn("Select", 50, 50, (rect, item) =>
			{
				if (GUI.Button(rect, $"{item.CountSelected()}/{item.sheets.Count}"))
				{
					EditGoogleSheetsWindow.ShowWindow(item, m_settings.ObfGoogleClientId, m_settings.ObfGoogleClientSecret, output =>
					{
						if (!m_settings.googleSheetsPaths.Exists(x => x.id == output.id))
							m_settings.googleSheetsPaths.Add(output);
					});
				}
			}).SetTooltip("Click to Edit");

			table.AddColumn("Remove", 55, 60, (rect, item) =>
			{
				var deleteIcon = EditorIcon.GetIcon(EditorIcon.Icon.DeletedLocal);
				if (GUI.Button(rect, deleteIcon))
					m_settings.googleSheetsPaths.Remove(item);
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