using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Tool
{
	public class FindAndReplaceAssetWindow : EditorWindow
	{
		private Vector2 m_scrollPosition;
		private FindAndReplaceAssetToolkit m_findAndReplaceAssetToolkit;
		private string m_tab;

		private void OnEnable()
		{
			m_findAndReplaceAssetToolkit = FindAndReplaceAssetToolkit.Load();
		}

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
			m_findAndReplaceAssetToolkit ??= FindAndReplaceAssetToolkit.Load();
			m_tab = EditorHelper.Tabs(nameof(FindAndReplaceAssetWindow), "Replace Sprite", "Cut Sprite Sheet", "Update Image Property", "Replace Object");
			GUILayout.BeginVertical("box");
			switch (m_tab)
			{
				case "Replace Sprite":
					m_findAndReplaceAssetToolkit.replaceSpriteTool.Draw();
					break;
				case "Cut Sprite Sheet":
					m_findAndReplaceAssetToolkit.cutSpriteSheetTool.Draw();
					break;
				case "Update Image Property":
					m_findAndReplaceAssetToolkit.updateImagePropertyTool.Draw();
					break;
				case "Replace Object":
					m_findAndReplaceAssetToolkit.replaceObjectTool.Draw();
					break;
			}
			GUILayout.EndVertical();
			GUILayout.EndScrollView();
		}
		
		public static void ShowWindow()
		{
			var window = GetWindow<FindAndReplaceAssetWindow>("Find And Replace Assets", true);
			window.Show();
		}
	}
}