using System;
using System.Collections.Generic;
using RCore.Config;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace RCore.Editor
{
	public class AssetGridPanel : IAssetCatalogPanel
	{
		private struct ListRow
		{
			public string key;
			public string category;
			public UnityEngine.Object asset;
		}

		private sealed class BatchReport
		{
			public int prefabsScanned;
			public int prefabsChanged;
			public int imagesMatched;
			public int imagesConverted;
			public int imagesSkipped;
			public readonly List<string> lines = new List<string>();
		}

		private sealed class KeyMigrationReport
		{
			public int prefabsScanned;
			public int prefabsChanged;
			public int linkersUpdated;
			public readonly List<string> lines = new List<string>();
		}

		private const float LIST_ROW_HEIGHT = 30f;
		private const float LIST_ICON_SIZE = 24f;
		private const float LIST_ICON_WIDTH = 30f;
		private const float LIST_KEY_WIDTH = 150f;
		private const float LIST_USAGE_WIDTH = 60f;
		private const float LIST_SELECT_WIDTH = 60f;
		private const float LIST_HEADER_HEIGHT = 18f;
		private const float LIST_CHECKBOX_WIDTH = 24f;
		private const float LEFT_PANE_MIN_WIDTH = 420f;
		private const float RIGHT_PANE_MIN_WIDTH = 320f;
		private const float PANE_GAP = 8f;
		private const float RIGHT_EDGE_PADDING = 6f;
		private const float ACTION_BUTTON_HEIGHT = 30f;
		private const float USAGE_ROW_HEIGHT = 22f;
		private const float USAGE_ICON_WIDTH = 22f;
		private const float USAGE_SELECT_WIDTH = 60f;
		private const float USAGE_COMPONENT_WIDTH = 140f;

		public string Title => "Asset Grid";

		private AssetCatalogWindow m_Window;
		private CatalogAssetType m_AssetType = CatalogAssetType.Sprite;
		private string m_ActiveCategory = "All";
		private Vector2 m_ScrollPos;
		private Vector2 m_UsageScrollPos;

		private string m_SelectedKey = string.Empty;
		private string m_EditKey = string.Empty;
		private string m_EditCategory = string.Empty;
		private UnityEngine.Object m_EditAsset;

		private bool m_ShowAddNew = false;
		private string m_NewKey = string.Empty;
		private string m_NewCategory = "Uncategorized";
		private UnityEngine.Object m_NewAsset;

		private string m_UsageKey = string.Empty;
		private readonly List<AssetCatalogWindow.UsageResult> m_UsageResults = new List<AssetCatalogWindow.UsageResult>();
		private readonly List<string> m_UsageSkippedAssets = new List<string>();

		private readonly HashSet<string> m_SelectedSpriteKeys = new HashSet<string>(StringComparer.Ordinal);
		private readonly List<string> m_BatchReportLines = new List<string>();
		private string m_BatchReportTitle = string.Empty;
		private bool m_IsKeyMigrationReport;
		private Vector2 m_BatchReportScroll;

		public void OnEnable(AssetCatalogWindow pWindow)
		{
			m_Window = pWindow;
		}

		public void OnDisable() { }

		public void OnGUI(Rect pRect)
		{
			var catalog = m_Window.Catalog;
			if (catalog == null)
				return;

			GUILayout.BeginArea(pRect);

			DrawAssetTypeToolbar();
			DrawActionButtons();
			GUILayout.Space(6f);

			var assetListPaneWidth = GetAssetListPaneWidth(pRect.width);
			var detailsPaneWidth = Mathf.Max(RIGHT_PANE_MIN_WIDTH, pRect.width - assetListPaneWidth - PANE_GAP - RIGHT_EDGE_PADDING);
			EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			DrawAssetListPane(catalog, assetListPaneWidth);
			GUILayout.Space(PANE_GAP);
			DrawDetailsPane(catalog, detailsPaneWidth);
			GUILayout.Space(RIGHT_EDGE_PADDING);
			EditorGUILayout.EndHorizontal();

			GUILayout.EndArea();
		}

		private string AssetTypeName
		{
			get
			{
				switch (m_AssetType)
				{
					case CatalogAssetType.Sprite: return "Sprite";
					case CatalogAssetType.Texture2D: return "Texture2D";
					case CatalogAssetType.AudioClip: return "AudioClip";
					default: return "Sprite";
				}
			}
		}

		private void DrawAssetTypeToolbar()
		{
			var names = new[] { "Sprite", "Texture2D", "AudioClip" };
			var newType = (CatalogAssetType)GUILayout.Toolbar((int)m_AssetType, names);
			if (newType != m_AssetType)
			{
				m_AssetType = newType;
				m_ActiveCategory = "All";
				m_SelectedKey = string.Empty;
				m_EditAsset = null;
				m_NewAsset = null;
				m_ShowAddNew = false;
				ClearUsageResults();
				ClearBatchReport();
			}
		}

		private void DrawActionButtons()
		{
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Add New", GUILayout.Width(90), GUILayout.Height(ACTION_BUTTON_HEIGHT)))
			{
				m_ShowAddNew = !m_ShowAddNew;
				m_SelectedKey = string.Empty;
				ClearUsageResults();
				ClearBatchReport();
			}
			if (GUILayout.Button("Find All Usages", GUILayout.Width(130), GUILayout.Height(ACTION_BUTTON_HEIGHT)))
			{
				m_Window.FindUsagesForAllAssets();
				if (!string.IsNullOrEmpty(m_SelectedKey))
					PopulateUsageFromCache(AssetTypeName, m_SelectedKey);
			}
			var canConvertSelectedImageSprites = m_AssetType == CatalogAssetType.Sprite && m_SelectedSpriteKeys.Count > 0;
			var batchConvertContent = new GUIContent(
				"Replace Image Sprites by Sprite Linkers",
				"Scan Addressable prefabs and replace matching Image.sprite references with GeneralSpriteLinker keys.");
			EditorGUI.BeginDisabledGroup(!canConvertSelectedImageSprites);
			if (GUILayout.Button(batchConvertContent, GUILayout.Height(ACTION_BUTTON_HEIGHT)))
				RunBatchSpriteLinkMigration(m_Window.Catalog);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
		}

		private float GetAssetListPaneWidth(float pContentWidth)
		{
			var maxPaneWidth = Mathf.Max(200f, pContentWidth - RIGHT_PANE_MIN_WIDTH - PANE_GAP);
			var minPaneWidth = Mathf.Min(LEFT_PANE_MIN_WIDTH, maxPaneWidth);
			return Mathf.Clamp((pContentWidth - PANE_GAP) * 0.58f, minPaneWidth, maxPaneWidth);
		}

		private void DrawAssetListPane(AssetCatalog pCatalog, float pPaneWidth)
		{
			EditorGUILayout.BeginVertical("box", GUILayout.Width(pPaneWidth), GUILayout.ExpandHeight(true));
			EditorGUILayout.LabelField($"{AssetTypeName} Catalog", EditorStyles.boldLabel);
			DrawAssetList(pCatalog, pPaneWidth - 12f);
			EditorGUILayout.EndVertical();
		}

		private void DrawDetailsPane(AssetCatalog pCatalog, float pPaneWidth)
		{
			EditorGUILayout.BeginVertical("box", GUILayout.Width(pPaneWidth), GUILayout.ExpandHeight(true));

			if (m_ShowAddNew)
			{
				DrawAddPanel(pCatalog);
			}
			else if (!string.IsNullOrEmpty(m_SelectedKey))
			{
				DrawEditPanel(pCatalog);
			}
			else
			{
				EditorGUILayout.HelpBox("Select an asset row or click Add New.", MessageType.Info);
			}

			DrawBatchReport();
			EditorGUILayout.EndVertical();
		}

		private void RunBatchSpriteLinkMigration(AssetCatalog pCatalog)
		{
			ClearBatchReport();

			if (pCatalog == null)
			{
				m_BatchReportTitle = "Batch Sprite Conversion";
				m_BatchReportLines.Add("No AssetCatalog loaded.");
				return;
			}

			if (m_AssetType != CatalogAssetType.Sprite)
			{
				m_BatchReportTitle = "Batch Sprite Conversion";
				m_BatchReportLines.Add("Switch to Sprite tab before converting selected Image sprites.");
				return;
			}

			if (m_SelectedSpriteKeys.Count == 0)
			{
				m_BatchReportTitle = "Batch Sprite Conversion";
				m_BatchReportLines.Add("No Sprite rows selected.");
				return;
			}

			var settings = AddressableAssetSettingsDefaultObject.Settings;
			if (settings == null)
			{
				m_BatchReportTitle = "Batch Sprite Conversion";
				m_BatchReportLines.Add("No Addressables settings found.");
				return;
			}

			var report = new BatchReport();
			var lookup = BuildSelectedSpriteLookup(pCatalog, report);
			if (lookup.Count == 0)
			{
				m_BatchReportTitle = "Batch Sprite Conversion";
				m_BatchReportLines.AddRange(report.lines);
				if (m_BatchReportLines.Count == 0)
					m_BatchReportLines.Add("No selected Sprite rows have valid Sprite assets.");
				return;
			}

			try
			{
				foreach (var entry in EnumerateAddressablePrefabEntries(settings))
				{
					report.prefabsScanned++;
					EditorUtility.DisplayProgressBar("Convert Selected Image Sprites", entry.AssetPath, Mathf.Max(0.01f, report.prefabsScanned / 100f));
					TryConvertPrefab(entry.AssetPath, lookup, report);
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			AssetDatabase.SaveAssets();
			m_BatchReportTitle = $"Batch Sprite Conversion: {report.imagesConverted} image(s) converted in {report.prefabsChanged}/{report.prefabsScanned} prefab(s)";
			m_BatchReportLines.AddRange(report.lines);
			m_Window.Repaint();
		}

		private static IEnumerable<AddressableAssetEntry> EnumerateAddressablePrefabEntries(AddressableAssetSettings pSettings)
		{
			if (pSettings == null)
				yield break;

			var entries = new List<AddressableAssetEntry>();
			pSettings.GetAllAssets(entries, false, null, null);
			foreach (var entry in entries)
			{
				if (entry == null)
					continue;
				if (string.IsNullOrEmpty(entry.AssetPath))
					continue;
				if (!entry.AssetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
					continue;

				yield return entry;
			}
		}

		private Dictionary<Sprite, string> BuildSelectedSpriteLookup(AssetCatalog pCatalog, BatchReport pReport)
		{
			var lookup = new Dictionary<Sprite, string>();
			var selectedKeys = new HashSet<string>(m_SelectedSpriteKeys, StringComparer.Ordinal);

			foreach (var entry in pCatalog.EditorSprites)
			{
				if (entry == null || !selectedKeys.Contains(entry.key))
					continue;
				if (entry.asset == null)
				{
					pReport.imagesSkipped++;
					pReport.lines.Add($"Skipped selected key '{entry.key}': Sprite asset is missing.");
					continue;
				}

				if (!lookup.ContainsKey(entry.asset))
					lookup.Add(entry.asset, entry.key);
				else
				{
					pReport.imagesSkipped++;
					pReport.lines.Add($"Skipped duplicate Sprite asset for key '{entry.key}'. First selected key stays '{lookup[entry.asset]}'.");
				}
			}

			return lookup;
		}

		private bool TryConvertPrefab(string pPrefabPath, IReadOnlyDictionary<Sprite, string> pSelectedSpriteLookup, BatchReport pReport)
		{
			GameObject root = null;
			var changed = false;

			try
			{
				root = PrefabUtility.LoadPrefabContents(pPrefabPath);
				if (root == null)
				{
					pReport.imagesSkipped++;
					pReport.lines.Add($"Skipped {pPrefabPath}: prefab load failed.");
					return false;
				}

				var images = root.GetComponentsInChildren<Image>(true);
				foreach (var image in images)
				{
					if (image == null || image.sprite == null)
						continue;

					var sprite = image.sprite;
					if (!pSelectedSpriteLookup.TryGetValue(sprite, out var spriteKey))
						continue;

					pReport.imagesMatched++;
					var existingLinker = image.GetComponent<GeneralSpriteLinker>();
					if (existingLinker != null && existingLinker.Key == spriteKey)
					{
						pReport.lines.Add($"Skipped {pPrefabPath}: '{spriteKey}' already linked on {image.gameObject.name}.");
						continue;
					}

					if (existingLinker != null && existingLinker.Key != spriteKey)
					{
						pReport.imagesSkipped++;
						pReport.lines.Add($"Skipped {pPrefabPath}: {image.gameObject.name} already has linker key '{existingLinker.Key}', wanted '{spriteKey}'.");
						continue;
					}

					var linker = image.gameObject.AddComponent<GeneralSpriteLinker>();
					linker.Key = spriteKey;
					image.sprite = null;
					image.overrideSprite = sprite;
					EditorUtility.SetDirty(image);
					EditorUtility.SetDirty(linker);
					pReport.imagesConverted++;
					changed = true;
				}

				if (changed)
				{
					PrefabUtility.SaveAsPrefabAsset(root, pPrefabPath, out var success);
					if (success)
					{
						pReport.prefabsChanged++;
					}
					else
					{
						pReport.imagesSkipped++;
						pReport.lines.Add($"Skipped {pPrefabPath}: prefab save failed.");
					}
				}

				return changed;
			}
			catch (Exception ex)
			{
				pReport.imagesSkipped++;
				pReport.lines.Add($"Skipped {pPrefabPath}: {ex.Message}");
				return false;
			}
			finally
			{
				if (root != null)
				{
					try { PrefabUtility.UnloadPrefabContents(root); }
					catch (Exception ex) { pReport.lines.Add($"Skipped {pPrefabPath}: unload failed: {ex.Message}"); }
				}
			}
		}

		private KeyMigrationReport RunPrefabKeyMigration(CatalogAssetType pType, string pOldKey, string pNewKey)
		{
			var report = new KeyMigrationReport();
			var guids = AssetDatabase.FindAssets("t:Prefab");

			try
			{
				for (int i = 0; i < guids.Length; i++)
				{
					var path = AssetDatabase.GUIDToAssetPath(guids[i]);
					report.prefabsScanned++;
					EditorUtility.DisplayProgressBar("Migrating AssetCatalog Keys", path, (float)i / guids.Length);
					TryMigratePrefabKey(path, pType, pOldKey, pNewKey, report);
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			return report;
		}

		private bool TryMigratePrefabKey(string pPrefabPath, CatalogAssetType pType, string pOldKey, string pNewKey, KeyMigrationReport pReport)
		{
			GameObject root = null;

			try
			{
				root = PrefabUtility.LoadPrefabContents(pPrefabPath);
				if (root == null)
				{
					pReport.lines.Add($"Warning {pPrefabPath}: prefab load failed.");
					return false;
				}

				int updateCount = 0;
				switch (pType)
				{
					case CatalogAssetType.Sprite:
						updateCount = UpdateLinkerKeys<GeneralSpriteLinker>(root, pOldKey, pNewKey);
						break;
					case CatalogAssetType.Texture2D:
						updateCount = UpdateLinkerKeys<GeneralTextureLinker>(root, pOldKey, pNewKey);
						break;
					case CatalogAssetType.AudioClip:
						updateCount = UpdateLinkerKeys<GeneralAudioLinker>(root, pOldKey, pNewKey);
						break;
				}

				if (updateCount <= 0)
					return false;

				PrefabUtility.SaveAsPrefabAsset(root, pPrefabPath, out var success);
				if (!success)
				{
					pReport.lines.Add($"Warning {pPrefabPath}: prefab save failed after {updateCount} linker(s) updated in memory.");
					return false;
				}

				pReport.prefabsChanged++;
				pReport.linkersUpdated += updateCount;
				return true;
			}
			catch (Exception ex)
			{
				pReport.lines.Add($"Warning {pPrefabPath}: {ex.Message}");
				return false;
			}
			finally
			{
				if (root != null)
				{
					try { PrefabUtility.UnloadPrefabContents(root); }
					catch (Exception ex) { pReport.lines.Add($"Warning {pPrefabPath}: unload failed: {ex.Message}"); }
				}
			}
		}

		private int UpdateLinkerKeys<T>(GameObject pRoot, string pOldKey, string pNewKey) where T : Component
		{
			var components = pRoot.GetComponentsInChildren<T>(true);
			int count = 0;

			foreach (var component in components)
			{
				if (component == null)
					continue;

				var serializedObject = new SerializedObject(component);
				var keyProperty = serializedObject.FindProperty("m_Key");
				if (keyProperty == null || keyProperty.stringValue != pOldKey)
					continue;

				keyProperty.stringValue = pNewKey;
				serializedObject.ApplyModifiedPropertiesWithoutUndo();
				EditorUtility.SetDirty(component);
				count++;
			}

			return count;
		}

		private void SetKeyMigrationReport(KeyMigrationReport pReport)
		{
			ClearBatchReport();
			m_IsKeyMigrationReport = true;
			if (pReport == null)
				return;

			m_BatchReportTitle = $"Key Migration: {pReport.linkersUpdated} linker(s) updated in {pReport.prefabsChanged}/{pReport.prefabsScanned} prefab(s)";
			m_BatchReportLines.AddRange(pReport.lines);
		}

		private void DrawAddPanel(AssetCatalog pCatalog)
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField($"Add New {AssetTypeName}", EditorStyles.boldLabel);
			m_NewKey = EditorHelper.TextField(m_NewKey, "Key", 100);
			m_NewCategory = EditorHelper.TextField(m_NewCategory, "Category", 100);

			switch (m_AssetType)
			{
				case CatalogAssetType.Sprite:
					m_NewAsset = (Sprite)EditorHelper.ObjectField<Sprite>(m_NewAsset as Sprite, "Asset", 100);
					break;
				case CatalogAssetType.Texture2D:
					m_NewAsset = (Texture2D)EditorHelper.ObjectField<Texture2D>(m_NewAsset as Texture2D, "Asset", 100);
					break;
				case CatalogAssetType.AudioClip:
					m_NewAsset = (AudioClip)EditorHelper.ObjectField<AudioClip>(m_NewAsset as AudioClip, "Asset", 100);
					break;
			}

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Create", GUILayout.Width(80), GUILayout.Height(22)))
				CreateEntry(pCatalog);
			if (GUILayout.Button("Cancel", GUILayout.Width(80), GUILayout.Height(22)))
				m_ShowAddNew = false;
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
		}

		private void CreateEntry(AssetCatalog pCatalog)
		{
			if (string.IsNullOrEmpty(m_NewKey))
			{
				EditorUtility.DisplayDialog("Error", "Key cannot be empty.", "OK");
				return;
			}
			if (pCatalog.EditorHasKey(m_AssetType, m_NewKey))
			{
				EditorUtility.DisplayDialog("Error", "Key already exists.", "OK");
				return;
			}

			switch (m_AssetType)
			{
				case CatalogAssetType.Sprite:
					pCatalog.EditorAddSprite(m_NewKey, m_NewCategory, m_NewAsset as Sprite);
					break;
				case CatalogAssetType.Texture2D:
					pCatalog.EditorAddTexture(m_NewKey, m_NewCategory, m_NewAsset as Texture2D);
					break;
				case CatalogAssetType.AudioClip:
					pCatalog.EditorAddAudioClip(m_NewKey, m_NewCategory, m_NewAsset as AudioClip);
					break;
			}

			m_ShowAddNew = false;
			m_NewKey = string.Empty;
			m_NewCategory = "Uncategorized";
			m_NewAsset = null;
			EditorUtility.SetDirty(pCatalog);
			AssetDatabase.SaveAssets();
			m_Window.Repaint();
		}

		private void DrawAssetList(AssetCatalog pCatalog, float pContentWidth)
		{
			pContentWidth = Mathf.Max(1f, pContentWidth);

			var categoryCounts = BuildCategoryCounts(pCatalog);
			var categories = new List<string> { "All" };
			foreach (var category in pCatalog.EditorGetDistinctCategories(m_AssetType))
			{
				var normalizedCategory = NormalizeCategory(category);
				if (!categories.Contains(normalizedCategory))
					categories.Add(normalizedCategory);
			}
			if (!categories.Contains(m_ActiveCategory))
				m_ActiveCategory = "All";

			var selectedCategoryIndex = Mathf.Max(0, categories.IndexOf(m_ActiveCategory));
			var categoryLabels = new string[categories.Count];
			for (int i = 0; i < categories.Count; i++)
			{
				var countValue = categoryCounts.TryGetValue(categories[i], out var value) ? value : 0;
				categoryLabels[i] = $"{categories[i]} ({countValue})";
			}
			var nextCategoryIndex = GUILayout.Toolbar(selectedCategoryIndex, categoryLabels);
			if (nextCategoryIndex >= 0 && nextCategoryIndex < categories.Count)
				m_ActiveCategory = categories[nextCategoryIndex];

			var rows = BuildRows(pCatalog, true);
			if (rows.Count == 0)
			{
				EditorGUILayout.HelpBox($"No {AssetTypeName} entries in this category.", MessageType.Info);
				return;
			}

			DrawListHeader(pContentWidth);
			var scrollPos = new Vector2(0f, m_ScrollPos.y);
			scrollPos = GUILayout.BeginScrollView(scrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandHeight(true));
			for (int i = 0; i < rows.Count; i++)
				DrawListRow(rows[i], pContentWidth, i);
			GUILayout.EndScrollView();
			m_ScrollPos = new Vector2(0f, scrollPos.y);
		}

		private Dictionary<string, int> BuildCategoryCounts(AssetCatalog pCatalog)
		{
			var counts = new Dictionary<string, int>(StringComparer.Ordinal) { { "All", 0 } };
			foreach (var row in BuildRows(pCatalog, false))
			{
				counts["All"]++;
				if (!counts.ContainsKey(row.category))
					counts[row.category] = 0;
				counts[row.category]++;
			}
			return counts;
		}

		private string NormalizeCategory(string pCategory)
		{
			return string.IsNullOrEmpty(pCategory) ? "Uncategorized" : pCategory;
		}

		private List<ListRow> BuildRows(AssetCatalog pCatalog, bool pFilterActiveCategory)
		{
			var rows = new List<ListRow>();
			switch (m_AssetType)
			{
				case CatalogAssetType.Sprite:
					foreach (var entry in pCatalog.EditorSprites)
						AddRow(rows, entry.key, entry.category, entry.asset, pFilterActiveCategory);
					break;
				case CatalogAssetType.Texture2D:
					foreach (var entry in pCatalog.EditorTextures)
						AddRow(rows, entry.key, entry.category, entry.asset, pFilterActiveCategory);
					break;
				case CatalogAssetType.AudioClip:
					foreach (var entry in pCatalog.EditorAudioClips)
						AddRow(rows, entry.key, entry.category, entry.asset, pFilterActiveCategory);
					break;
			}
			return rows;
		}

		private void AddRow(List<ListRow> pRows, string pKey, string pCategory, UnityEngine.Object pAsset, bool pFilterActiveCategory)
		{
			var category = NormalizeCategory(pCategory);
			if (pFilterActiveCategory && m_ActiveCategory != "All" && category != m_ActiveCategory)
				return;
			pRows.Add(new ListRow { key = pKey, category = category, asset = pAsset });
		}

		private void DrawListHeader(float pContentWidth)
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(LIST_HEADER_HEIGHT));
			GUILayout.Space(LIST_CHECKBOX_WIDTH);
			GUILayout.Space(LIST_ICON_WIDTH);
			GUILayout.Label("Key", EditorStyles.miniBoldLabel, GUILayout.Width(LIST_KEY_WIDTH));
			GUILayout.Label("Path", EditorStyles.miniBoldLabel, GUILayout.Width(GetPathWidth(pContentWidth)));
			GUILayout.Label("Usages", EditorStyles.miniBoldLabel, GUILayout.Width(LIST_USAGE_WIDTH));
			GUILayout.Label("Action", EditorStyles.miniBoldLabel, GUILayout.Width(LIST_SELECT_WIDTH));
			EditorGUILayout.EndHorizontal();
		}

		private void DrawListRow(ListRow pRow, float pContentWidth, int pIndex)
		{
			var rowRect = EditorGUILayout.GetControlRect(false, LIST_ROW_HEIGHT);
			if (m_SelectedKey == pRow.key)
				EditorGUI.DrawRect(rowRect, new Color(0.18f, 0.38f, 0.58f, 0.35f));
			else if (pIndex % 2 == 0)
				EditorGUI.DrawRect(rowRect, new Color(0f, 0f, 0f, 0.1f));

			var y = rowRect.y + 3f;
			var rowHeight = LIST_ROW_HEIGHT - 6f;
			var x = rowRect.x;
			var checkboxRect = new Rect(x, y, LIST_CHECKBOX_WIDTH, rowHeight);
			x += LIST_CHECKBOX_WIDTH;
			var iconRect = new Rect(x + 3f, rowRect.y + 3f, LIST_ICON_SIZE, LIST_ICON_SIZE);
			x += LIST_ICON_WIDTH;
			var keyRect = new Rect(x, y, LIST_KEY_WIDTH, rowHeight);
			x += LIST_KEY_WIDTH;
			var pathWidth = GetPathWidth(pContentWidth);
			var pathRect = new Rect(x, y, pathWidth, rowHeight);
			x += pathWidth;
			var usageRect = new Rect(x, y, LIST_USAGE_WIDTH, rowHeight);
			x += LIST_USAGE_WIDTH;
			var selectRect = new Rect(x, y, LIST_SELECT_WIDTH, rowHeight);

			if (m_AssetType == CatalogAssetType.Sprite)
			{
				var selected = IsSpriteSelected(pRow.key);
				var nextSelected = GUI.Toggle(checkboxRect, selected, GUIContent.none);
				if (nextSelected != selected)
					SetSpriteSelected(pRow.key, nextSelected);
			}

			DrawListIcon(iconRect, pRow.asset);
			EditorGUI.SelectableLabel(keyRect, pRow.key, EditorStyles.miniLabel);
			var path = pRow.asset != null ? AssetDatabase.GetAssetPath(pRow.asset) : string.Empty;
			EditorGUI.SelectableLabel(pathRect, path, EditorStyles.miniLabel);
			GUI.Label(usageRect, GetUsageCount(pRow.key).ToString(), EditorStyles.miniLabel);
			if (GUI.Button(selectRect, "Select"))
				SelectEntry(pRow.key, pRow.category, pRow.asset);
		}

		private void DrawListIcon(Rect pRect, UnityEngine.Object pAsset)
		{
			if (pAsset == null)
				return;

			var preview = AssetPreview.GetAssetPreview(pAsset);
			if (preview == null)
				preview = AssetPreview.GetMiniThumbnail(pAsset);
			if (preview != null)
				GUI.DrawTexture(pRect, preview, ScaleMode.ScaleToFit);
		}

		private int GetUsageCount(string pKey)
		{
			var cacheKey = AssetCatalogWindow.GetCacheKey(AssetTypeName, pKey);
			if (!m_Window.UsageCache.TryGetValue(cacheKey, out var entry) || entry.usages == null)
				return 0;
			return entry.usages.Count;
		}

		private bool IsSpriteSelected(string pKey)
		{
			return !string.IsNullOrEmpty(pKey) && m_SelectedSpriteKeys.Contains(pKey);
		}

		private void SetSpriteSelected(string pKey, bool pSelected)
		{
			if (string.IsNullOrEmpty(pKey))
				return;

			if (pSelected)
				m_SelectedSpriteKeys.Add(pKey);
			else
				m_SelectedSpriteKeys.Remove(pKey);
		}

		private void ClearBatchReport()
		{
			m_IsKeyMigrationReport = false;
			m_BatchReportTitle = string.Empty;
			m_BatchReportLines.Clear();
			m_BatchReportScroll = Vector2.zero;
		}

		private void RemapKeySelection(string pOldKey, string pNewKey)
		{
			if (string.IsNullOrEmpty(pOldKey) || string.IsNullOrEmpty(pNewKey))
				return;

			if (m_SelectedSpriteKeys.Remove(pOldKey))
				m_SelectedSpriteKeys.Add(pNewKey);
		}

		private float GetPathWidth(float pContentWidth)
		{
			return Mathf.Max(120f, pContentWidth - LIST_CHECKBOX_WIDTH - LIST_ICON_WIDTH - LIST_KEY_WIDTH - LIST_USAGE_WIDTH - LIST_SELECT_WIDTH - 40f);
		}

		private void SelectEntry(string pKey, string pCategory, UnityEngine.Object pAsset)
		{
			m_ShowAddNew = false;
			m_SelectedKey = pKey;
			m_EditKey = pKey;
			m_EditCategory = pCategory;
			m_EditAsset = pAsset;
			PopulateUsageFromCache(AssetTypeName, pKey);
		}

		private void DrawEditPanel(AssetCatalog pCatalog)
		{
			EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
			EditorGUILayout.LabelField($"Selected {AssetTypeName}", EditorStyles.boldLabel);
			m_EditKey = EditorHelper.TextField(m_EditKey, "Key", 100);
			m_EditCategory = EditorHelper.TextField(m_EditCategory, "Category", 100);

			switch (m_AssetType)
			{
				case CatalogAssetType.Sprite:
					m_EditAsset = (Sprite)EditorHelper.ObjectField<Sprite>(m_EditAsset as Sprite, "Asset", 100);
					break;
				case CatalogAssetType.Texture2D:
					m_EditAsset = (Texture2D)EditorHelper.ObjectField<Texture2D>(m_EditAsset as Texture2D, "Asset", 100);
					break;
				case CatalogAssetType.AudioClip:
					m_EditAsset = (AudioClip)EditorHelper.ObjectField<AudioClip>(m_EditAsset as AudioClip, "Asset", 100);
					break;
			}

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Save Changes", GUILayout.Width(100), GUILayout.Height(22)))
				SaveEntryChanges(pCatalog);
			if (GUILayout.Button("Delete", GUILayout.Width(80), GUILayout.Height(22)))
				DeleteSelectedEntry(pCatalog);
			if (GUILayout.Button("Deselect", GUILayout.Width(80), GUILayout.Height(22)))
			{
				m_SelectedKey = string.Empty;
				ClearUsageResults();
			}
			if (GUILayout.Button("Find Usages", GUILayout.Width(100), GUILayout.Height(22)))
				FindUsagesForSelectedAsset();
			EditorGUILayout.EndHorizontal();

			DrawUsageResults();
			EditorGUILayout.EndVertical();
		}

		private bool ValidateRenameBeforeConfirmation(AssetCatalog pCatalog, string pOldKey, string pNewKey, out string pError)
		{
			pError = null;

			if (pCatalog == null)
			{
				pError = "No AssetCatalog loaded.";
				return false;
			}

			if (string.IsNullOrEmpty(pOldKey))
			{
				pError = "Old key is empty.";
				return false;
			}

			if (string.IsNullOrEmpty(pNewKey))
			{
				pError = "New key is empty.";
				return false;
			}

			if (!pCatalog.EditorHasKey(m_AssetType, pOldKey))
			{
				pError = $"Old key '{pOldKey}' not found in AssetCatalog.";
				return false;
			}

			if (pCatalog.EditorHasKey(m_AssetType, pNewKey))
			{
				pError = $"New key '{pNewKey}' already exists in AssetCatalog.";
				return false;
			}

			return true;
		}

		private void SaveEntryChanges(AssetCatalog pCatalog)
		{
			if (string.IsNullOrEmpty(m_EditKey))
			{
				EditorUtility.DisplayDialog("Error", "Key cannot be empty.", "OK");
				return;
			}

			bool keyChanged = m_EditKey != m_SelectedKey;
			var oldKey = m_SelectedKey;
			KeyMigrationReport migrationReport = null;

			if (keyChanged)
			{
				if (!ValidateRenameBeforeConfirmation(pCatalog, oldKey, m_EditKey, out var validationError))
				{
					EditorUtility.DisplayDialog("Error", $"Rename failed: {validationError}", "OK");
					return;
				}

				var confirmed = EditorUtility.DisplayDialog(
					"Update Prefab Linker Keys",
					$"Rename '{oldKey}' to '{m_EditKey}' and update matching {AssetTypeName} linker keys in all project prefabs?",
					"Yes",
					"No");

				if (!confirmed)
					return;

				if (pCatalog.EditorTryRenameKey(m_AssetType, oldKey, m_EditKey, out string err))
				{
					m_SelectedKey = m_EditKey;
					RemapKeySelection(oldKey, m_SelectedKey);
				}
				else
				{
					EditorUtility.DisplayDialog("Error", $"Rename failed: {err}", "OK");
					return;
				}
			}

			switch (m_AssetType)
			{
				case CatalogAssetType.Sprite:
					pCatalog.EditorUpdateSpriteCategory(m_SelectedKey, m_EditCategory);
					pCatalog.EditorUpdateSpriteAsset(m_SelectedKey, m_EditAsset as Sprite);
					break;
				case CatalogAssetType.Texture2D:
					pCatalog.EditorUpdateTextureCategory(m_SelectedKey, m_EditCategory);
					pCatalog.EditorUpdateTextureAsset(m_SelectedKey, m_EditAsset as Texture2D);
					break;
				case CatalogAssetType.AudioClip:
					pCatalog.EditorUpdateAudioClipCategory(m_SelectedKey, m_EditCategory);
					pCatalog.EditorUpdateAudioClipAsset(m_SelectedKey, m_EditAsset as AudioClip);
					break;
			}

			if (keyChanged)
				migrationReport = RunPrefabKeyMigration(m_AssetType, oldKey, m_SelectedKey);

			EditorUtility.SetDirty(pCatalog);
			AssetDatabase.SaveAssets();
			if (keyChanged)
			{
				var oldCacheKey = AssetCatalogWindow.GetCacheKey(AssetTypeName, oldKey);
				var newCacheKey = AssetCatalogWindow.GetCacheKey(AssetTypeName, m_SelectedKey);
				var hadCacheEntry = m_Window.UsageCache.TryGetValue(oldCacheKey, out var cacheEntry);
				if (hadCacheEntry)
				{
					m_Window.UsageCache.Remove(oldCacheKey);
					cacheEntry.key = m_SelectedKey;
					m_Window.UsageCache[newCacheKey] = cacheEntry;
					m_Window.SaveUsageCache();
					PopulateUsageFromCache(AssetTypeName, m_SelectedKey);
				}
				else
				{
					ClearUsageResults();
				}

				SetKeyMigrationReport(migrationReport);
			}
			m_Window.Repaint();
		}

		private void DeleteSelectedEntry(AssetCatalog pCatalog)
		{
			if (!EditorHelper.ConfirmPopup($"Are you sure you want to delete '{m_SelectedKey}'?", "Yes", "No"))
				return;

			var deletedKey = m_SelectedKey;
			SetSpriteSelected(deletedKey, false);
			ClearBatchReport();
			pCatalog.EditorDeleteEntry(m_AssetType, deletedKey);
			m_SelectedKey = string.Empty;
			ClearUsageResults();
			RemoveCacheEntry(AssetTypeName, deletedKey);
			EditorUtility.SetDirty(pCatalog);
			AssetDatabase.SaveAssets();
			m_Window.Repaint();
		}

		private void ClearUsageResults()
		{
			m_UsageKey = string.Empty;
			m_UsageResults.Clear();
			m_UsageSkippedAssets.Clear();
		}

		private void PopulateUsageFromCache(string pAssetType, string pKey)
		{
			m_UsageResults.Clear();
			m_UsageSkippedAssets.Clear();
			m_UsageKey = string.Empty;

			var cacheKey = AssetCatalogWindow.GetCacheKey(pAssetType, pKey);
			if (!m_Window.UsageCache.TryGetValue(cacheKey, out var entry))
				return;

			m_UsageKey = pKey;
			m_UsageResults.AddRange(entry.usages ?? new List<AssetCatalogWindow.UsageResult>());
			m_UsageSkippedAssets.AddRange(entry.skippedPrefabs ?? new List<string>());
		}

		private void RemoveCacheEntry(string pAssetType, string pKey)
		{
			var cacheKey = AssetCatalogWindow.GetCacheKey(pAssetType, pKey);
			if (m_Window.UsageCache.Remove(cacheKey))
				m_Window.SaveUsageCache();
		}

		private void FindUsagesForSelectedAsset()
		{
			ClearUsageResults();

			var key = m_SelectedKey;
			if (string.IsNullOrEmpty(key))
			{
				EditorUtility.DisplayDialog("Error", "No entry selected.", "OK");
				return;
			}
			if (m_EditKey != m_SelectedKey)
			{
				EditorUtility.DisplayDialog("Save Changes Required", "Save or discard key changes before finding usages.", "OK");
				return;
			}

			m_UsageKey = key;
			var guids = AssetDatabase.FindAssets("t:Prefab");
			try
			{
				for (int i = 0; i < guids.Length; i++)
				{
					var path = AssetDatabase.GUIDToAssetPath(guids[i]);
					EditorUtility.DisplayProgressBar("Finding Asset Usages", path, (float)i / guids.Length);
					GameObject root = null;
					int resultCountBeforeScan = m_UsageResults.Count;
					try
					{
						root = PrefabUtility.LoadPrefabContents(path);
						if (root == null)
							continue;

						switch (m_AssetType)
						{
							case CatalogAssetType.Sprite:
								ScanLinkerUsages<GeneralSpriteLinker>(root, path, key, nameof(GeneralSpriteLinker));
								break;
							case CatalogAssetType.Texture2D:
								ScanLinkerUsages<GeneralTextureLinker>(root, path, key, nameof(GeneralTextureLinker));
								break;
							case CatalogAssetType.AudioClip:
								ScanLinkerUsages<GeneralAudioLinker>(root, path, key, nameof(GeneralAudioLinker));
								break;
						}
					}
					catch (Exception ex)
					{
						if (m_UsageResults.Count > resultCountBeforeScan)
							m_UsageResults.RemoveRange(resultCountBeforeScan, m_UsageResults.Count - resultCountBeforeScan);
						m_UsageSkippedAssets.Add($"{path} ({ex.Message})");
					}
					finally
					{
						if (root != null)
						{
							try { PrefabUtility.UnloadPrefabContents(root); }
							catch (Exception ex) { m_UsageSkippedAssets.Add($"{path} (unload failed: {ex.Message})"); }
						}
					}
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			var typeName = AssetTypeName;
			var cacheKey = AssetCatalogWindow.GetCacheKey(typeName, key);
			m_Window.UsageCache[cacheKey] = new AssetCatalogWindow.UsageCacheEntry
			{
				key = key,
				assetType = typeName,
				usages = new List<AssetCatalogWindow.UsageResult>(m_UsageResults),
				skippedPrefabs = new List<string>(m_UsageSkippedAssets),
			};
			m_Window.LastScanTimestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
			m_Window.SaveUsageCache();
			m_Window.Repaint();
		}

		private void ScanLinkerUsages<T>(GameObject pRoot, string pPrefabPath, string pUsageKey, string pComponentType) where T : Component
		{
			var components = EditorHelper.FindComponents<T>(new[] { pRoot }, pComponent =>
			{
				var so = new SerializedObject(pComponent);
				var keyProp = so.FindProperty("m_Key");
				return keyProp != null && keyProp.stringValue == pUsageKey;
			});
			foreach (var pair in components.Values)
			{
				foreach (var component in pair)
					m_UsageResults.Add(new AssetCatalogWindow.UsageResult { prefabPath = pPrefabPath, componentType = pComponentType });
			}
		}

		private void DrawUsageResults()
		{
			if (string.IsNullOrEmpty(m_UsageKey))
				return;

			GUILayout.Space(8);
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField($"Linker Usages for '{m_UsageKey}'", EditorStyles.boldLabel);

			if (m_UsageResults.Count == 0)
			{
				EditorGUILayout.HelpBox($"No prefab usages found for '{m_UsageKey}'.", MessageType.Info);
			}
			else
			{
				EditorGUILayout.HelpBox($"Found {m_UsageResults.Count} linker usage(s) in prefab assets for '{m_UsageKey}'.", MessageType.Info);
				m_UsageScrollPos = EditorGUILayout.BeginScrollView(m_UsageScrollPos, GUILayout.MinHeight(140f), GUILayout.ExpandHeight(true));
				EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(LIST_HEADER_HEIGHT));
				GUILayout.Space(USAGE_ICON_WIDTH);
				GUILayout.Label("Prefab", EditorStyles.miniBoldLabel);
				GUILayout.Label("Component", EditorStyles.miniBoldLabel, GUILayout.Width(USAGE_COMPONENT_WIDTH));
				GUILayout.Label("Action", EditorStyles.miniBoldLabel, GUILayout.Width(USAGE_SELECT_WIDTH));
				EditorGUILayout.EndHorizontal();
				for (int i = 0; i < m_UsageResults.Count; i++)
					DrawUsageResultRow(m_UsageResults[i], i);
				EditorGUILayout.EndScrollView();
			}

			if (m_UsageSkippedAssets.Count > 0)
			{
				GUILayout.Space(8);
				EditorGUILayout.HelpBox($"Skipped {m_UsageSkippedAssets.Count} prefab(s):\n- {string.Join("\n- ", m_UsageSkippedAssets)}", MessageType.Warning);
			}

			EditorGUILayout.EndVertical();
		}

		private void DrawUsageResultRow(AssetCatalogWindow.UsageResult pResult, int pIndex)
		{
			var rowRect = EditorGUILayout.GetControlRect(false, USAGE_ROW_HEIGHT);
			if (pIndex % 2 == 0)
				EditorGUI.DrawRect(rowRect, new Color(0f, 0f, 0f, 0.1f));

			var iconRect = new Rect(rowRect.x, rowRect.y + 1f, USAGE_ICON_WIDTH, USAGE_ROW_HEIGHT - 2f);
			var actionRect = new Rect(rowRect.xMax - USAGE_SELECT_WIDTH, rowRect.y + 2f, USAGE_SELECT_WIDTH, USAGE_ROW_HEIGHT - 4f);
			var componentRect = new Rect(actionRect.x - USAGE_COMPONENT_WIDTH - 4f, rowRect.y + 2f, USAGE_COMPONENT_WIDTH, USAGE_ROW_HEIGHT - 4f);
			var pathRect = new Rect(iconRect.xMax + 4f, rowRect.y + 2f, componentRect.x - iconRect.xMax - 8f, USAGE_ROW_HEIGHT - 4f);

			var icon = AssetDatabase.GetCachedIcon(pResult.prefabPath);
			if (icon != null)
				GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
			GUI.Label(pathRect, pResult.prefabPath, EditorStyles.miniLabel);
			GUI.Label(componentRect, pResult.componentType, EditorStyles.miniLabel);

			if (GUI.Button(actionRect, "Select"))
			{
				var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pResult.prefabPath);
				if (obj != null)
				{
					Selection.activeObject = obj;
					EditorGUIUtility.PingObject(obj);
				}
			}
		}

		private void DrawBatchReport()
		{
			if (string.IsNullOrEmpty(m_BatchReportTitle) && m_BatchReportLines.Count == 0)
				return;

			GUILayout.Space(8);
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField(m_BatchReportTitle, EditorStyles.boldLabel);

			if (m_BatchReportLines.Count == 0)
			{
				if (m_IsKeyMigrationReport)
					EditorGUILayout.HelpBox("No skipped prefab or migration error details.", MessageType.Info);
				else
					EditorGUILayout.HelpBox("No skipped prefab or linker conflict details.", MessageType.Info);
			}
			else
			{
				m_BatchReportScroll = EditorGUILayout.BeginScrollView(m_BatchReportScroll, GUILayout.Height(140));
				foreach (var line in m_BatchReportLines)
					EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
				EditorGUILayout.EndScrollView();
			}

			EditorGUILayout.EndVertical();
		}
	}
}
