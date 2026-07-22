using System;
using System.Collections.Generic;
using System.IO;
using RCore.Config;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	public class AssetCatalogWindow : EditorWindow
	{
		[Serializable]
		public class UsageResult
		{
			public string prefabPath;
			public string componentType;
		}

		[Serializable]
		public class UsageCacheEntry
		{
			public string key;
			public string assetType;
			public List<UsageResult> usages = new List<UsageResult>();
			public List<string> skippedPrefabs = new List<string>();
		}

		[Serializable]
		private class UsageCacheFile
		{
			public string lastScanTimestamp;
			public List<UsageCacheEntry> entries = new List<UsageCacheEntry>();
			public List<string> skippedPrefabs = new List<string>();
		}

		private AssetCatalog m_Catalog;
		private int m_ActiveTabIndex = 0;
		private IAssetCatalogPanel[] m_Panels;
		private string[] m_TabLabels;

		private Dictionary<string, UsageCacheEntry> m_UsageCache = new Dictionary<string, UsageCacheEntry>();
		private string m_LastScanTimestamp = string.Empty;
		private List<string> m_CacheSkippedPrefabs = new List<string>();

		public AssetCatalog Catalog => m_Catalog;
		public Dictionary<string, UsageCacheEntry> UsageCache => m_UsageCache;

		public string LastScanTimestamp
		{
			get => m_LastScanTimestamp;
			set => m_LastScanTimestamp = value;
		}

		public List<string> CacheSkippedPrefabs
		{
			get => m_CacheSkippedPrefabs;
			set => m_CacheSkippedPrefabs = value;
		}

		[MenuItem("RCore/Asset Catalog Editor")]
		public static void Open()
		{
			var window = GetWindow<AssetCatalogWindow>("Asset Catalog");
			window.Show();
		}

		private void OnEnable()
		{
			m_Catalog = AssetCatalog.Instance;
			m_Panels = new IAssetCatalogPanel[]
			{
				new AssetGridPanel(),
				new UsageReportPanel(),
				new RelinkPanel(),
			};
			m_TabLabels = new string[m_Panels.Length];
			for (int i = 0; i < m_Panels.Length; i++)
			{
				m_TabLabels[i] = m_Panels[i].Title;
				m_Panels[i].OnEnable(this);
			}
			LoadUsageCache();
		}

		private void OnDisable()
		{
			if (m_Panels == null)
				return;

			foreach (var panel in m_Panels)
				panel.OnDisable();
		}

		private void OnGUI()
		{
			if (m_Catalog == null)
			{
				EditorGUILayout.HelpBox("No AssetCatalog found in Resources.", MessageType.Error);
				if (GUILayout.Button("Retry"))
					m_Catalog = AssetCatalog.Instance;
				return;
			}

			const float TAB_HEIGHT = 20f;
			const float CONTENT_TOP_PADDING = 6f;

			var newTab = GUILayout.Toolbar(m_ActiveTabIndex, m_TabLabels, GUILayout.Height(TAB_HEIGHT));
			if (newTab != m_ActiveTabIndex)
			{
				m_ActiveTabIndex = newTab;
				GUIUtility.keyboardControl = 0;
			}

			var contentY = TAB_HEIGHT + CONTENT_TOP_PADDING;
			var contentRect = new Rect(0f, contentY, position.width, Mathf.Max(0f, position.height - contentY));
			if (m_Panels != null && m_ActiveTabIndex >= 0 && m_ActiveTabIndex < m_Panels.Length)
				m_Panels[m_ActiveTabIndex].OnGUI(contentRect);
		}

		public void SwitchToTab(int pIndex)
		{
			if (m_Panels != null && pIndex >= 0 && pIndex < m_Panels.Length)
			{
				m_ActiveTabIndex = pIndex;
				Repaint();
			}
		}

		public static string GetCacheKey(string pAssetType, string pKey)
		{
			return $"{pAssetType}:{pKey}";
		}

		private string GetCacheAssetPath()
		{
			if (m_Catalog == null)
				return string.Empty;

			var assetPath = AssetDatabase.GetAssetPath(m_Catalog);
			if (string.IsNullOrEmpty(assetPath))
				return string.Empty;

			var dir = Path.GetDirectoryName(assetPath);
			return Path.Combine(dir ?? string.Empty, "AssetCatalog_Usages.json").Replace('\\', '/');
		}

		private string GetCacheFullPath()
		{
			var cacheAssetPath = GetCacheAssetPath();
			if (string.IsNullOrEmpty(cacheAssetPath))
				return string.Empty;

			var projectRoot = Directory.GetParent(Application.dataPath).FullName;
			return Path.Combine(projectRoot, cacheAssetPath);
		}

		public void LoadUsageCache()
		{
			m_UsageCache.Clear();
			m_CacheSkippedPrefabs.Clear();
			m_LastScanTimestamp = string.Empty;

			var path = GetCacheFullPath();
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
				return;

			try
			{
				var json = File.ReadAllText(path);
				var cacheFile = JsonUtility.FromJson<UsageCacheFile>(json);
				if (cacheFile == null)
					return;

				m_LastScanTimestamp = cacheFile.lastScanTimestamp;
				m_CacheSkippedPrefabs = cacheFile.skippedPrefabs ?? new List<string>();
				var entries = cacheFile.entries ?? new List<UsageCacheEntry>();
				foreach (var entry in entries)
				{
					if (entry == null || string.IsNullOrEmpty(entry.key) || string.IsNullOrEmpty(entry.assetType))
						continue;

					entry.usages = entry.usages ?? new List<UsageResult>();
					entry.skippedPrefabs = entry.skippedPrefabs ?? new List<string>();
					var cacheKey = GetCacheKey(entry.assetType, entry.key);
					m_UsageCache[cacheKey] = entry;
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[AssetCatalogWindow] Failed to load usage cache: {ex.Message}");
			}
		}

		public void SaveUsageCache()
		{
			var entries = new List<UsageCacheEntry>(m_UsageCache.Values);
			entries.Sort((pA, pB) =>
			{
				int typeComparison = string.Compare(pA.assetType, pB.assetType, StringComparison.Ordinal);
				return typeComparison != 0 ? typeComparison : string.Compare(pA.key, pB.key, StringComparison.Ordinal);
			});

			var cacheFile = new UsageCacheFile
			{
				lastScanTimestamp = m_LastScanTimestamp,
				entries = entries,
				skippedPrefabs = m_CacheSkippedPrefabs,
			};
			try
			{
				var fullPath = GetCacheFullPath();
				if (string.IsNullOrEmpty(fullPath))
					return;

				var json = JsonUtility.ToJson(cacheFile, false);
				File.WriteAllText(fullPath, json);
				var cacheAssetPath = GetCacheAssetPath();
				if (cacheAssetPath.StartsWith("Assets/"))
					AssetDatabase.ImportAsset(cacheAssetPath, ImportAssetOptions.Default);
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[AssetCatalogWindow] Failed to save usage cache: {ex.Message}");
			}
		}

		public void FindUsagesForAllAssets()
		{
			if (m_Catalog == null)
				return;

			var spriteKeys = new HashSet<string>();
			foreach (var entry in m_Catalog.EditorSprites)
				spriteKeys.Add(entry.key);

			var textureKeys = new HashSet<string>();
			foreach (var entry in m_Catalog.EditorTextures)
				textureKeys.Add(entry.key);

			var audioKeys = new HashSet<string>();
			foreach (var entry in m_Catalog.EditorAudioClips)
				audioKeys.Add(entry.key);

			var newCache = new Dictionary<string, UsageCacheEntry>();
			foreach (var key in spriteKeys)
				newCache[GetCacheKey("Sprite", key)] = new UsageCacheEntry { key = key, assetType = "Sprite" };
			foreach (var key in textureKeys)
				newCache[GetCacheKey("Texture2D", key)] = new UsageCacheEntry { key = key, assetType = "Texture2D" };
			foreach (var key in audioKeys)
				newCache[GetCacheKey("AudioClip", key)] = new UsageCacheEntry { key = key, assetType = "AudioClip" };

			var skippedPrefabs = new List<string>();
			var guids = AssetDatabase.FindAssets("t:Prefab");

			try
			{
				for (int i = 0; i < guids.Length; i++)
				{
					var path = AssetDatabase.GUIDToAssetPath(guids[i]);
					EditorUtility.DisplayProgressBar("Finding All Asset Usages", path, (float)i / guids.Length);

					GameObject root = null;
					try
					{
						root = PrefabUtility.LoadPrefabContents(path);
						if (root == null)
							continue;

						var spriteLinkers = EditorHelper.FindComponents<GeneralSpriteLinker>(new[] { root }, pLinker => spriteKeys.Contains(pLinker.Key));
						foreach (var pair in spriteLinkers)
						{
							foreach (var linker in pair.Value)
							{
								var cacheKey = GetCacheKey("Sprite", linker.Key);
								if (newCache.TryGetValue(cacheKey, out var cacheEntry))
									cacheEntry.usages.Add(new UsageResult { prefabPath = path, componentType = nameof(GeneralSpriteLinker) });
							}
						}

						var textureLinkers = EditorHelper.FindComponents<GeneralTextureLinker>(new[] { root }, pLinker => textureKeys.Contains(pLinker.Key));
						foreach (var pair in textureLinkers)
						{
							foreach (var linker in pair.Value)
							{
								var cacheKey = GetCacheKey("Texture2D", linker.Key);
								if (newCache.TryGetValue(cacheKey, out var cacheEntry))
									cacheEntry.usages.Add(new UsageResult { prefabPath = path, componentType = nameof(GeneralTextureLinker) });
							}
						}

						var audioLinkers = EditorHelper.FindComponents<GeneralAudioLinker>(new[] { root }, pLinker => audioKeys.Contains(pLinker.Key));
						foreach (var pair in audioLinkers)
						{
							foreach (var linker in pair.Value)
							{
								var cacheKey = GetCacheKey("AudioClip", linker.Key);
								if (newCache.TryGetValue(cacheKey, out var cacheEntry))
									cacheEntry.usages.Add(new UsageResult { prefabPath = path, componentType = nameof(GeneralAudioLinker) });
							}
						}
					}
					catch (Exception ex)
					{
						skippedPrefabs.Add($"{path} ({ex.Message})");
					}
					finally
					{
						if (root != null)
						{
							try { PrefabUtility.UnloadPrefabContents(root); }
							catch (Exception ex) { skippedPrefabs.Add($"{path} (unload failed: {ex.Message})"); }
						}
					}
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			m_UsageCache = newCache;
			m_CacheSkippedPrefabs = skippedPrefabs;
			m_LastScanTimestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
			SaveUsageCache();
			Repaint();
		}
	}

}
