using UnityEditor;
using UnityEngine;

namespace RCore.LXLite.Editor
{
	public class LXLiteWindow : EditorWindow
	{
		private const string NAME = "LocalizationX: Localization Exporter";
		private const string MENU = "LocalizationX Lite";
		
		private Vector2 m_scrollPosition;
		private LXLiteExcelWindow m_excelWindow;
		private LXLiteGoogleWindow m_googleWindow;
		
		private void OnEnable()
		{
			m_excelWindow ??= new LXLiteExcelWindow();
			m_excelWindow.OnEnable();
			m_googleWindow ??= new LXLiteGoogleWindow();
			m_googleWindow.OnEnable();
		}

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);

			EditorGUILayout.BeginVertical("box");
			LXLiteConfig.LocalizationOutputFolder = EditorHelper.FolderField(LXLiteConfig.LocalizationOutputFolder, "Localizations Output Folder", 170);
			LXLiteConfig.ScriptsOutputFolder = EditorHelper.FolderField(LXLiteConfig.ScriptsOutputFolder, "Scripts Output Folder", 170);
			LXLiteConfig.LangCharSets = EditorHelper.TextField(LXLiteConfig.LangCharSets, "Language Character Sets", 170);
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.BeginVertical("box");
			var tab = EditorHelper.Tabs($"{nameof(LXLiteWindow)}", "Excel Spreadsheets", "Google Spreadsheets");
			switch (tab)
			{
				case "Excel Spreadsheets":
					m_excelWindow.OnGUI();
					break;
				case "Google Spreadsheets":
					m_googleWindow.OnGUI();
					break;
			}
			EditorGUILayout.EndVertical();
			
			GUILayout.EndScrollView();
		}
		
#if ASSETS_STORE
		[MenuItem("Window/" + MENU)]
#else
		[MenuItem("RCore/Tools/" + MENU)]
#endif
		public static void ShowWindow()
		{
			var window = GetWindow<LXLiteWindow>(NAME, true);
			window.Show();
		}
	}
}