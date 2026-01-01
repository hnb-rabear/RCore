using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Tool
{
	/// <summary>
	/// Editor window hosting the FindAndReplaceAssetToolkit, allowing users to replace sprites, cut sprite sheets, fix image properties, and replace objects.
	/// </summary>
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
					m_findAndReplaceAssetToolkit.spriteReplacer.Draw();
					break;
				case "Cut Sprite Sheet":
					m_findAndReplaceAssetToolkit.spriteSheetCutter.Draw();
					break;
				case "Update Image Property":
					m_findAndReplaceAssetToolkit.imagePropertyFixer.Draw();
					break;
				case "Replace Object":
					m_findAndReplaceAssetToolkit.objectReplacer.Draw();
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