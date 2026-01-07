using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// The main editor window for the SheetX tool, providing tabs for Excel, Google Sheets, and Settings.
	/// </summary>
	public class SheetXWindow : EditorWindow
	{
#if !SX_LOCALIZATION
		private const string NAME = "SheetX: Sheets Exporter";
#if !SX_LITE
		private const string MENU = "SheetX";
#else
		private const string MENU = "SheetX Lite";
#endif
#else
		private const string NAME = "LocalizationX: Localization Exporter";
		private const string MENU = "LocalizationX";
#endif

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

			GUILayout.BeginHorizontal();
			var iconSave = EditorIcon.GetIcon(EditorIcon.Icon.SaveAs);
			if (EditorHelper.Button(null, iconSave, default, 30, 30))
				m_settingsWindow.Save();
			var iconLoad = EditorIcon.GetIcon(EditorIcon.Icon.FolderOpened);
			if (EditorHelper.Button(null, iconLoad, default, 30, 30))
				m_settingsWindow.Load();
			GUILayout.EndHorizontal();

			var tab = EditorHelper.Tabs($"{nameof(SheetXWindow)}", "Excel Spreadsheets", "Google Spreadsheets", "Settings");
			switch (tab)
			{
				case "Excel Spreadsheets":
					m_excelSheetXWindow.OnGUI();
					break;
				case "Settings":
					m_settingsWindow.OnGUI();
					break;
				case "Google Spreadsheets":
					m_googleSheetXWindow.OnGUI();
					break;
			}

			GUILayout.EndScrollView();
		}

#if ASSETS_STORE
		[MenuItem("Window/" + MENU)]
#else
		public const int GROUP_14 = 140;

		[MenuItem("RCore/Tools/" + MENU, priority = GROUP_14)]
#endif
		/// <summary>
		/// Opens the SheetX editor window.
		/// </summary>
		public static void ShowWindow()
		{
			var window = GetWindow<SheetXWindow>(NAME, true);
			window.Show();
		}
	}
}