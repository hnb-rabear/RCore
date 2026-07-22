using System;
using System.Collections.Generic;
using RCore.Config;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	public class AssetCatalogPickerWindow : EditorWindow
	{
		private CatalogAssetType m_Type;
		private Action<string> m_OnPicked;
		private string m_Search = "";
		private Vector2 m_ScrollPos;

		private class PickerRow
		{
			public CatalogAssetType type;
			public string key;
			public string category;
			public string searchTextLower;
			public UnityEngine.Object asset;
			public string assetPath;
			public Texture preview;
			public List<string> prefabUsages;
			public List<string> sceneUsages;
		}

		private readonly List<PickerRow> m_Rows = new List<PickerRow>();
		private readonly List<PickerRow> m_FilteredRows = new List<PickerRow>();
		private bool m_RowsDirty = true;
		private CatalogAssetType m_CachedType;

		public static void Open(CatalogAssetType type, Action<string> onPicked)
		{
			var window = CreateInstance<AssetCatalogPickerWindow>();
			window.titleContent = new GUIContent($"Pick {type}");
			window.m_Type = type;
			window.m_RowsDirty = true;
			window.m_OnPicked = onPicked;
			window.ShowUtility();
		}

		private void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginChangeCheck();
			m_Search = EditorGUILayout.TextField("Search", m_Search);
			bool searchChanged = EditorGUI.EndChangeCheck();
			if (GUILayout.Button("Find Usages", GUILayout.Width(100)))
				ScanAllUsages();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space(4);

			var catalog = AssetCatalog.Instance;
			if (catalog == null)
			{
				EditorGUILayout.HelpBox("No AssetCatalog found. Create one via Assets > Create > RCore > Config > Asset Catalog and place it in a Resources folder.", MessageType.Warning);
				return;
			}

			if (m_RowsDirty || m_CachedType != m_Type)
				RebuildRows(catalog);
			else if (searchChanged)
				RebuildFilteredRows();

			m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
			foreach (var row in m_FilteredRows)
				DrawRow(row);
			EditorGUILayout.EndScrollView();
		}

		private void RebuildRows(AssetCatalog pCatalog)
		{
			m_Rows.Clear();
			m_CachedType = m_Type;
			pCatalog.EditorEnsureUsageCache();

			switch (m_Type)
			{
				case CatalogAssetType.Sprite:
					foreach (var entry in pCatalog.EditorSprites)
						AddRow(CatalogAssetType.Sprite, entry.key, entry.category, entry.asset, entry.prefabUsages, entry.sceneUsages);
					break;
				case CatalogAssetType.Texture2D:
					foreach (var entry in pCatalog.EditorTextures)
						AddRow(CatalogAssetType.Texture2D, entry.key, entry.category, entry.asset, entry.prefabUsages, entry.sceneUsages);
					break;
				case CatalogAssetType.AudioClip:
					foreach (var entry in pCatalog.EditorAudioClips)
						AddRow(CatalogAssetType.AudioClip, entry.key, entry.category, entry.asset, entry.prefabUsages, entry.sceneUsages);
					break;
			}

			m_RowsDirty = false;
			RebuildFilteredRows();
		}

		private void AddRow(CatalogAssetType pType, string pKey, string pCategory, UnityEngine.Object pAsset, List<string> pPrefabUsages, List<string> pSceneUsages)
		{
			var key = pKey ?? string.Empty;
			var category = pCategory ?? string.Empty;
			var assetPath = pAsset != null ? AssetDatabase.GetAssetPath(pAsset) : string.Empty;

			var prefabUsages = pPrefabUsages ?? new List<string>();
			var sceneUsages = pSceneUsages ?? new List<string>();

			m_Rows.Add(new PickerRow
			{
				type = pType,
				key = key,
				category = category,
				searchTextLower = $"{key} {category}".ToLowerInvariant(),
				asset = pAsset,
				assetPath = assetPath,
				prefabUsages = prefabUsages,
				sceneUsages = sceneUsages,
			});
		}

		private void RebuildFilteredRows()
		{
			m_FilteredRows.Clear();
			var search = string.IsNullOrEmpty(m_Search) ? string.Empty : m_Search.ToLowerInvariant();
			foreach (var row in m_Rows)
			{
				if (string.IsNullOrEmpty(search) || row.searchTextLower.Contains(search))
					m_FilteredRows.Add(row);
			}
		}

		private void DrawRow(PickerRow pRow)
		{
			EditorGUILayout.BeginHorizontal("box");
			var preview = GetPreview(pRow);
			if (preview != null)
				GUILayout.Label(preview, GUILayout.Width(32), GUILayout.Height(32));
			else if (pRow.type != CatalogAssetType.AudioClip)
				GUILayout.Space(36);

			EditorGUILayout.BeginVertical();
			GUILayout.Label(pRow.key, EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(pRow.category, EditorStyles.miniLabel);
			if (!string.IsNullOrEmpty(pRow.assetPath))
				GUILayout.Label(pRow.assetPath, EditorStyles.miniLabel);
			EditorGUILayout.EndHorizontal();

			DrawUsageSummary(pRow);

			EditorGUILayout.EndVertical();
			if (pRow.asset != null)
			{
				if (GUILayout.Button("Ping", GUILayout.Width(50)))
					EditorGUIUtility.PingObject(pRow.asset);
			}
			if (GUILayout.Button("Select", GUILayout.Width(60)))
				Pick(pRow.key);
			EditorGUILayout.EndHorizontal();
		}

		private Texture GetPreview(PickerRow pRow)
		{
			if (pRow.asset == null)
				return null;
			if (pRow.preview != null)
				return pRow.preview;

			pRow.preview = pRow.type == CatalogAssetType.AudioClip
				? AssetDatabase.GetCachedIcon(pRow.assetPath)
				: AssetPreview.GetAssetPreview(pRow.asset);
			return pRow.preview;
		}

		private void DrawUsageSummary(PickerRow pRow)
		{
			int prefabCount = pRow.prefabUsages != null ? pRow.prefabUsages.Count : 0;
			int sceneCount = pRow.sceneUsages != null ? pRow.sceneUsages.Count : 0;
			if (prefabCount == 0 && sceneCount == 0)
				return;

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label($"Usages: {prefabCount} prefabs, {sceneCount} scenes", EditorStyles.miniLabel);
			if (GUILayout.Button("Show", EditorStyles.miniButton, GUILayout.Width(50)))
				ShowUsagesPopup(pRow.key, pRow.prefabUsages, pRow.sceneUsages);
			EditorGUILayout.EndHorizontal();
		}

		private void Pick(string key)
		{
			m_OnPicked?.Invoke(key);
			Close();
		}

		private static Dictionary<string, List<SpriteCatalogEntry>> BuildSpriteEntryMap(IReadOnlyList<SpriteCatalogEntry> pEntries)
		{
			var map = new Dictionary<string, List<SpriteCatalogEntry>>(StringComparer.Ordinal);
			foreach (var entry in pEntries)
			{
				if (string.IsNullOrEmpty(entry.key))
					continue;
				if (!map.TryGetValue(entry.key, out var list))
				{
					list = new List<SpriteCatalogEntry>();
					map.Add(entry.key, list);
				}
				list.Add(entry);
			}
			return map;
		}

		private static Dictionary<string, List<TextureCatalogEntry>> BuildTextureEntryMap(IReadOnlyList<TextureCatalogEntry> pEntries)
		{
			var map = new Dictionary<string, List<TextureCatalogEntry>>(StringComparer.Ordinal);
			foreach (var entry in pEntries)
			{
				if (string.IsNullOrEmpty(entry.key))
					continue;
				if (!map.TryGetValue(entry.key, out var list))
				{
					list = new List<TextureCatalogEntry>();
					map.Add(entry.key, list);
				}
				list.Add(entry);
			}
			return map;
		}

		private static Dictionary<string, List<AudioCatalogEntry>> BuildAudioEntryMap(IReadOnlyList<AudioCatalogEntry> pEntries)
		{
			var map = new Dictionary<string, List<AudioCatalogEntry>>(StringComparer.Ordinal);
			foreach (var entry in pEntries)
			{
				if (string.IsNullOrEmpty(entry.key))
					continue;
				if (!map.TryGetValue(entry.key, out var list))
				{
					list = new List<AudioCatalogEntry>();
					map.Add(entry.key, list);
				}
				list.Add(entry);
			}
			return map;
		}

		private static void AddUsage(List<string> pUsages, string pPath)
		{
			if (pUsages == null)
				return;
			if (!pUsages.Contains(pPath))
				pUsages.Add(pPath);
		}

		private void ScanAllUsages()
		{
			var catalog = AssetCatalog.Instance;
			if (catalog == null) return;

			catalog.EditorClearUsageCache();

			var spriteEntryMap = BuildSpriteEntryMap(catalog.EditorSprites);
			var textureEntryMap = BuildTextureEntryMap(catalog.EditorTextures);
			var audioEntryMap = BuildAudioEntryMap(catalog.EditorAudioClips);

			var guids = AssetDatabase.FindAssets("t:Prefab");
			for (int i = 0; i < guids.Length; i++)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[i]);
				EditorUtility.DisplayProgressBar("Scanning Usages", path, guids.Length == 0 ? 1f : (float)i / guids.Length);
				GameObject root = null;
				try
				{
					root = PrefabUtility.LoadPrefabContents(path);
					if (root == null) continue;

					var spriteLinkers = root.GetComponentsInChildren<GeneralSpriteLinker>(true);
					foreach (var linker in spriteLinkers)
					{
						if (string.IsNullOrEmpty(linker.Key)) continue;
						if (!spriteEntryMap.TryGetValue(linker.Key, out var entries)) continue;
						foreach (var entry in entries)
							AddUsage(entry.prefabUsages, path);
					}

					var textureLinkers = root.GetComponentsInChildren<GeneralTextureLinker>(true);
					foreach (var linker in textureLinkers)
					{
						if (string.IsNullOrEmpty(linker.Key)) continue;
						if (!textureEntryMap.TryGetValue(linker.Key, out var entries)) continue;
						foreach (var entry in entries)
							AddUsage(entry.prefabUsages, path);
					}

					var audioLinkers = root.GetComponentsInChildren<GeneralAudioLinker>(true);
					foreach (var linker in audioLinkers)
					{
						if (string.IsNullOrEmpty(linker.Key)) continue;
						if (!audioEntryMap.TryGetValue(linker.Key, out var entries)) continue;
						foreach (var entry in entries)
							AddUsage(entry.prefabUsages, path);
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"Error scanning prefab {path}: {ex}");
				}
				finally
				{
					if (root != null)
						PrefabUtility.UnloadPrefabContents(root);
				}
			}

			EditorUtility.ClearProgressBar();
			m_RowsDirty = true;
			Repaint();
		}

		private void ShowUsagesPopup(string key, List<string> prefabUsages, List<string> sceneUsages)
		{
			var text = $"Usages for '{key}':\n\nPrefabs:\n";
			text += string.Join("\n", prefabUsages);
			text += "\n\nScenes:\n";
			text += string.Join("\n", sceneUsages);
			EditorUtility.DisplayDialog("Usages", text, "OK");
		}
	}
}