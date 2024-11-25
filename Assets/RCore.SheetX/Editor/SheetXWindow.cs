using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.SheetX
{
	public class SheetXWindow : EditorWindow
	{
		private Vector2 m_scrollPosition;
		
		private ExcelSheetXWindow m_excelSheetXWindow;
		private GoogleSheetXWindow m_googleSheetXWindow;
		private SheetXSettingsWindow m_settingsWindow;

		private void OnEnable()
		{
			m_excelSheetXWindow ??= new ExcelSheetXWindow();
			m_excelSheetXWindow.OnEnable();
			m_excelSheetXWindow.editorWindow = this;
			m_googleSheetXWindow ??= new GoogleSheetXWindow();
			m_googleSheetXWindow.OnEnable();
			m_googleSheetXWindow.editorWindow = this;
			m_settingsWindow ??= new SheetXSettingsWindow();
			m_settingsWindow.OnEnable();
		}

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
			
			var tab = EditorHelper.Tabs($"{nameof(SheetXWindow)}", "Excel Spreadsheets", "Google Spreadsheets", "Settings");
			switch (tab)
			{
				case "Excel Spreadsheets":
					EditorHelper.DrawLine();
					m_excelSheetXWindow.OnGUI();
					break;
				case "Settings":
					m_settingsWindow.OnGUI();
					break;
				case "Google Spreadsheets":
					EditorHelper.DrawLine();
					m_googleSheetXWindow.OnGUI();
					break;
			}
			
			GUILayout.EndScrollView();
		}
		
#if ASSETS_STORE
		[MenuItem("Window/SheetX")]
#else
		[MenuItem("RCore/Tools/SheetX")]
#endif
		public static void ShowWindow()
		{
			var window = GetWindow<SheetXWindow>("Sheet X: Sheets Exporter", true);
			window.Show();
		}
	}
}