using System.Collections;
using System.Collections.Generic;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using RCore.Editor;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX
{
	public class GoogleSheetXWindow : EditorWindow
	{
		private Vector2 m_scrollPosition;
		private SheetXSettings m_sheetXSettings;
		private GoogleSheetHandler m_googleSheetHandler;

		private void OnEnable()
		{
			m_sheetXSettings = SheetXSettings.Load();
			m_googleSheetHandler = new GoogleSheetHandler(m_sheetXSettings);
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
					m_sheetXSettings.google.path = EditorHelper.TextField(m_sheetXSettings.google.path, "Google Spreadsheets Id", 160);
					EditorHelper.TextField("Google Spreadsheets Name", "Google Spreadsheets Name", 160, readOnly: true);
				}
				GUILayout.EndVertical();
				if (EditorHelper.Button("Download", pHeight: 41))
					m_googleSheetHandler.Download();
			}
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