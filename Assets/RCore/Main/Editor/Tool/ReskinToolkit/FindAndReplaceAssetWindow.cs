using System.Collections.Generic;
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

		private static readonly Dictionary<string, string> m_TabHelp = new Dictionary<string, string>
		{
			["Replace Sprite"] = "Pair Old and New sprites, fix pivots, then swap Old for New in prefabs, scenes and assets.",
			["Cut Sprite Sheet"] = "Export every sprite of a sheet as its own texture.",
			["Update Image Property"] = "Set Image options on every Image that uses the listed sprites.",
			["Replace Object"] = "Swap references to any asset for another asset.",
			["Replace Font"] = "Swap TMP and legacy fonts in assets and in the open scene.",
		};

		private void OnEnable()
		{
			m_findAndReplaceAssetToolkit = FindAndReplaceAssetToolkit.Load();
		}

		private void OnDisable()
		{
			if (m_findAndReplaceAssetToolkit == null)
				return;
			EditorUtility.SetDirty(m_findAndReplaceAssetToolkit);
			AssetDatabase.SaveAssetIfDirty(m_findAndReplaceAssetToolkit);
		}

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
			m_findAndReplaceAssetToolkit ??= FindAndReplaceAssetToolkit.Load();
			m_tab = EditorHelper.Tabs(nameof(FindAndReplaceAssetWindow), "Replace Sprite", "Cut Sprite Sheet", "Update Image Property", "Replace Object", "Replace Font");
			if (m_tab != null && m_TabHelp.TryGetValue(m_tab, out var help))
				EditorGUILayout.HelpBox(help, MessageType.None);
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
				case "Replace Font":
					m_findAndReplaceAssetToolkit.fontReplacer.Draw();
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