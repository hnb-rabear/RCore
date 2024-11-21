/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace RCore.Editor.SheetX
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
			PageSingleFile();
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
					SheetXHelper.DownloadGoogleSheet(m_settings.ObfGoogleClientId, m_settings.ObfGoogleClientSecret, m_settings.googleSheetsPath);
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
			if (EditorHelper.Button("Open Settings", pHeight: 30))
				SheetXSettingsWindow.ShowWindow();
			EditorGUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}

		public static void ShowWindow()
		{
			var window = GetWindow<GoogleSheetXWindow>("Google Sheets Exporter", true);
			window.Show();
		}
	}
}