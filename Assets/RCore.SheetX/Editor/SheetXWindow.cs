using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// The main editor window for the SheetX tool, providing tabs for Excel, Google Sheets, and Settings.
	/// </summary>
	public class SheetXWindow : EditorWindow
	{
		private const string NAME = "SheetX: Sheets Exporter";
		private const string MENU = "SheetX";

		private Vector2 m_scrollPosition;

		private SheetXSettings m_settings;
		private ExcelSheetXWindow m_excelSheetXWindow;
		private GoogleSheetXWindow m_googleSheetXWindow;
		private SheetXSettingsWindow m_settingsWindow;

		private void OnEnable()
		{
			m_settings = SheetXSettings.Init();
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

		// Every tab mutates m_settings in place, so flush once on focus loss / close rather than
		// after each individual edit. Both callbacks also fire during assembly reload, where an
		// AssetDatabase write is unsafe — mark dirty here and let delayCall do the actual write.
		private void OnLostFocus() => FlushSettings();

		private void OnDisable() => FlushSettings();

		private void FlushSettings()
		{
			if (m_settings == null)
				return;
			var settings = m_settings;
			EditorUtility.SetDirty(settings);
			EditorApplication.delayCall += () =>
			{
				if (settings != null)
					AssetDatabase.SaveAssetIfDirty(settings);
			};
		}

#if !IKIT_SHEETX
#if ASSETS_STORE
		[MenuItem("Window/" + MENU)]
#else
		[MenuItem("RCore/" + MENU, priority = 24)]
#endif
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