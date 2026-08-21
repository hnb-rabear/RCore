using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	public class UsageReportPanel : IAssetCatalogPanel
	{
		public string Title => "Usage Report";

		private const float ROW_HEIGHT = 22f;
		private const float ICON_WIDTH = 22f;
		private const float SELECT_WIDTH = 60f;
		private const float COMPONENT_WIDTH = 150f;

		private AssetCatalogWindow m_Window;
		private Dictionary<string, bool> m_Expanded = new Dictionary<string, bool>();
		private Vector2 m_ScrollPos;

		public void OnEnable(AssetCatalogWindow pWindow)
		{
			m_Window = pWindow;
		}

		public void OnDisable() { }

		public void OnGUI(Rect pRect)
		{
			GUILayout.BeginArea(pRect);
			m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

			if (m_Window.UsageCache.Count == 0)
			{
				EditorGUILayout.HelpBox("No cached usage data. Click 'Find All Usages' in Asset Grid tab, or click Refresh All below.", MessageType.Info);
				if (GUILayout.Button("Refresh All", GUILayout.Width(100), GUILayout.Height(30)))
					m_Window.FindUsagesForAllAssets();
				EditorGUILayout.EndScrollView();
				GUILayout.EndArea();
				return;
			}

			EditorGUILayout.BeginHorizontal();
			if (!string.IsNullOrEmpty(m_Window.LastScanTimestamp))
				EditorGUILayout.LabelField($"Last scan: {m_Window.LastScanTimestamp}", EditorStyles.miniLabel);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Refresh All", GUILayout.Width(100), GUILayout.Height(30)))
				m_Window.FindUsagesForAllAssets();
			EditorGUILayout.EndHorizontal();

			var spriteEntries = new List<AssetCatalogWindow.UsageCacheEntry>();
			var textureEntries = new List<AssetCatalogWindow.UsageCacheEntry>();
			var audioEntries = new List<AssetCatalogWindow.UsageCacheEntry>();

			foreach (var entry in m_Window.UsageCache.Values)
			{
				switch (entry.assetType)
				{
					case "Sprite": spriteEntries.Add(entry); break;
					case "Texture2D": textureEntries.Add(entry); break;
					case "AudioClip": audioEntries.Add(entry); break;
				}
			}

			if (spriteEntries.Count > 0)
				DrawGroup("Sprite", spriteEntries);
			if (textureEntries.Count > 0)
				DrawGroup("Texture2D", textureEntries);
			if (audioEntries.Count > 0)
				DrawGroup("AudioClip", audioEntries);

			if (m_Window.CacheSkippedPrefabs.Count > 0)
			{
				GUILayout.Space(4);
				EditorGUILayout.HelpBox($"Skipped {m_Window.CacheSkippedPrefabs.Count} prefab(s):\n- {string.Join("\n- ", m_Window.CacheSkippedPrefabs)}", MessageType.Warning);
			}

			EditorGUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		private void DrawGroup(string pAssetType, List<AssetCatalogWindow.UsageCacheEntry> pEntries)
		{
			var sorted = new List<AssetCatalogWindow.UsageCacheEntry>(pEntries);
			sorted.Sort((pA, pB) =>
			{
				if (pA.usages.Count == 0 && pB.usages.Count > 0) return -1;
				if (pA.usages.Count > 0 && pB.usages.Count == 0) return 1;
				return string.Compare(pA.key, pB.key, StringComparison.Ordinal);
			});

			GUILayout.Space(4);
			EditorGUILayout.LabelField($"{pAssetType} ({sorted.Count})", EditorStyles.boldLabel);

			foreach (var entry in sorted)
			{
				var expandKey = AssetCatalogWindow.GetCacheKey(entry.assetType, entry.key);
				if (!m_Expanded.ContainsKey(expandKey))
					m_Expanded[expandKey] = false;

				var usageCount = entry.usages.Count;
				var label = usageCount == 0
					? $"  ⚠ {entry.key} — 0 usages (UNUSED)"
					: $"  {entry.key} — {usageCount} usage(s)";

				EditorGUILayout.BeginHorizontal();
				m_Expanded[expandKey] = EditorGUILayout.Foldout(m_Expanded[expandKey], label, true);
				EditorGUILayout.EndHorizontal();

				if (!m_Expanded[expandKey])
					continue;

				if (entry.usages.Count == 0)
					continue;

				EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(18f));
				GUILayout.Space(ICON_WIDTH);
				GUILayout.Label("Prefab", EditorStyles.miniBoldLabel);
				GUILayout.Label("Component", EditorStyles.miniBoldLabel, GUILayout.Width(COMPONENT_WIDTH));
				GUILayout.Label("Action", EditorStyles.miniBoldLabel, GUILayout.Width(SELECT_WIDTH));
				EditorGUILayout.EndHorizontal();
				for (int i = 0; i < entry.usages.Count; i++)
					DrawUsageRow(entry.usages[i], i);
			}
		}

		private void DrawUsageRow(AssetCatalogWindow.UsageResult pUsage, int pIndex)
		{
			var rowRect = EditorGUILayout.GetControlRect(false, ROW_HEIGHT);
			if (pIndex % 2 == 0)
				EditorGUI.DrawRect(rowRect, new Color(0f, 0f, 0f, 0.1f));

			var iconRect = new Rect(rowRect.x, rowRect.y + 1f, ICON_WIDTH, ROW_HEIGHT - 2f);
			var actionRect = new Rect(rowRect.xMax - SELECT_WIDTH, rowRect.y + 2f, SELECT_WIDTH, ROW_HEIGHT - 4f);
			var componentRect = new Rect(actionRect.x - COMPONENT_WIDTH - 4f, rowRect.y + 2f, COMPONENT_WIDTH, ROW_HEIGHT - 4f);
			var pathRect = new Rect(iconRect.xMax + 4f, rowRect.y + 2f, componentRect.x - iconRect.xMax - 8f, ROW_HEIGHT - 4f);

			var icon = AssetDatabase.GetCachedIcon(pUsage.prefabPath);
			if (icon != null)
				GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
			GUI.Label(pathRect, AssetCatalogEditorGui.StripAssetsPrefix(pUsage.prefabPath), EditorStyles.miniLabel);
			GUI.Label(componentRect, pUsage.componentType, EditorStyles.miniLabel);

			if (GUI.Button(actionRect, "Select"))
			{
				var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pUsage.prefabPath);
				if (obj != null)
				{
					Selection.activeObject = obj;
					EditorGUIUtility.PingObject(obj);
				}
			}
		}
	}
}
