using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Tool
{
	public class SearchAndReplaceAssetWindow : EditorWindow
	{
		private Vector2 m_scrollPosition;
		private SearchAndReplaceAssetToolkit m_searchAndReplaceAssetToolkit;
		private string m_tab;

		private void OnEnable()
		{
			m_searchAndReplaceAssetToolkit = SearchAndReplaceAssetToolkit.Load();
		}

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
			m_searchAndReplaceAssetToolkit ??= SearchAndReplaceAssetToolkit.Load();
			m_tab = EditorHelper.Tabs(nameof(SearchAndReplaceAssetWindow), "Replace Sprite", "Cut Sprite Sheet", "Update Image Property", "Replace Object");
			GUILayout.BeginVertical("box");
			switch (m_tab)
			{
				case "Replace Sprite":
					m_searchAndReplaceAssetToolkit.replaceSpriteTool.Draw();
					break;
				case "Cut Sprite Sheet":
					m_searchAndReplaceAssetToolkit.cutSpriteSheetTool.Draw();
					break;
				case "Update Image Property":
					m_searchAndReplaceAssetToolkit.updateImagePropertyTool.Draw();
					break;
				case "Replace Object":
					m_searchAndReplaceAssetToolkit.replaceObjectTool.Draw();
					break;
			}
			GUILayout.EndVertical();
			GUILayout.EndScrollView();
		}
		
		public static void ShowWindow()
		{
			var window = GetWindow<SearchAndReplaceAssetWindow>("Search And Replace Asset Toolkit", true);
			window.Show();
		}
	}
}