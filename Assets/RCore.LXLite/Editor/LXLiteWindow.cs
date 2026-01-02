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

			SupportDev();
		}

		private void SupportDev()
		{
			EditorGUILayout.BeginVertical("box");
			var color = GUI.backgroundColor;
			GUILayout.Space(5);
			var labelStyle = new GUIStyle(EditorStyles.helpBox)
			{
				fontSize = 15,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter,
				padding = new RectOffset(10, 10, 10, 10)
			};
			GUILayout.Label("If you are enjoying this tool, please consider supporting the project.", labelStyle);
			GUILayout.BeginHorizontal();

			bool rated = EditorPrefs.GetBool($"{Application.identifier}.LXLite.RateClicked", false);
			if (!rated) GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
			else GUI.backgroundColor = color;

			if (GUILayout.Button("Rate on Asset Store", GUILayout.Height(30)))
			{
				Application.OpenURL("https://assetstore.unity.com/packages/tools/utilities/sheetx-pro-manage-constants-data-localization-with-excel-google--300772");
				EditorPrefs.SetBool($"{Application.identifier}.LXLite.RateClicked", true);
			}

			bool starred = EditorPrefs.GetBool($"{Application.identifier}.LXLite.StarClicked", false);
			if (!starred) GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
			else GUI.backgroundColor = color;

			if (GUILayout.Button("Star on GitHub", GUILayout.Height(30)))
			{
				Application.OpenURL("https://github.com/hnb-rabear/RCore");
				EditorPrefs.SetBool($"{Application.identifier}.LXLite.StarClicked", true);
			}
			GUILayout.EndHorizontal();

			GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
			if (GUILayout.Button("Buy me a coffee", GUILayout.Height(30)))
				Application.OpenURL("https://ko-fi.com/rabear");
			GUI.backgroundColor = color;
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