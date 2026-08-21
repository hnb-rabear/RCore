using System;
using System.Collections.Generic;
using RCore.Config;
using RCore.UI;
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
			public bool autoActive;
			public string searchText;
		}

		private sealed class CachedSearchRow
		{
			public string key;
			public string category;
			public UnityEngine.Object asset;
			public bool autoActive;
			public string searchText;
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
		private const float LIST_AUTO_ACTIVE_WIDTH = 80f;
		private const float LIST_SELECT_WIDTH = 60f;
		private const float LIST_HEADER_HEIGHT = 22f;
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
		private const double SEARCH_DEBOUNCE_SECONDS = 0.25d;

		public string Title => "Asset Grid";

		private AssetCatalogWindow m_Window;
		private CatalogAssetType m_AssetType = CatalogAssetType.Sprite;
		private string m_ActiveCategory = "All";
		private Vector2 m_ScrollPos;
		private Vector2 m_UsageScrollPos;
		private Vector2 m_DirectUsageScrollPos;

		private string m_PendingSearch = string.Empty;
		private string m_AppliedSearch = string.Empty;
		private double m_SearchChangeTime;
		private AssetSearchFilter.Query m_AppliedSearchQuery = AssetSearchFilter.ParseQuery(string.Empty);
		private AssetCatalog m_RowCacheCatalog;
		private CatalogAssetType m_RowCacheAssetType;
		private bool m_RowCacheValid;
		private readonly List<CachedSearchRow> m_RowSearchCache = new List<CachedSearchRow>();

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
		private readonly List<string> m_DirectUsageResults = new List<string>();
		private readonly List<string> m_DirectUsageSkippedAssets = new List<string>();
		private UnityEngine.Object m_DirectUsageAsset;
		private bool m_ShowAddressableDirectUsagesOnly;

		private readonly Dictionary<CatalogAssetType, HashSet<string>> m_SelectedKeysByType =
			new Dictionary<CatalogAssetType, HashSet<string>>();
		private readonly List<string> m_BatchReportLines = new List<string>();
		private string m_BatchReportTitle = string.Empty;
		private string m_DropResult = string.Empty;
		private bool m_IsKeyMigrationReport;
		private LinkerRestoreService.LinkerRestoreResult m_LinkRestorePreview;
		private string m_LinkRestorePreviewKey = string.Empty;
		private CatalogAssetType m_LinkRestorePreviewType;
		private bool m_IsLinkRestoreReport;
		private const string LINK_RESTORE_PREVIEW_TITLE = "Restore Preview";
		private Vector2 m_BatchReportScroll;
		private GUIStyle m_HeaderToolbarStyle;

		private enum SortColumn { None, Key, Path, Usages }
		private enum SortDirection { Ascending, Descending }

		private SortColumn m_SortColumn = SortColumn.None;
		private SortDirection m_SortDirection = SortDirection.Ascending;

		private GUIStyle HeaderToolbarStyle
		{
			get
			{
				if (m_HeaderToolbarStyle == null)
				{
					m_HeaderToolbarStyle = new GUIStyle(EditorStyles.toolbar)
					{
						fixedHeight = 0f,
						stretchHeight = true,
					};
				}

				return m_HeaderToolbarStyle;
			}
		}

		private Rect DrawHeaderBackground(float pHeight)
		{
			var headerRect = GUILayoutUtility.GetRect(0f, pHeight, GUILayout.ExpandWidth(true));
			GUI.Box(headerRect, GUIContent.none, HeaderToolbarStyle);
			return headerRect;
		}

		private void DrawHeaderLabel(Rect pRect, string pText)
		{
			GUI.Label(pRect, pText, EditorStyles.miniBoldLabel);
		}

		private void DrawSelectAllCheckbox(Rect pRect, List<ListRow> pVisibleRows)
		{
			if (pVisibleRows.Count == 0)
				return;

			var selectedKeys = GetSelectedKeys(m_AssetType);
			int selectedCount = 0;
			for (int i = 0; i < pVisibleRows.Count; i++)
			{
				if (selectedKeys.Contains(pVisibleRows[i].key))
					selectedCount++;
			}

			bool allSelected = selectedCount == pVisibleRows.Count;
			bool noneSelected = selectedCount == 0;
			bool mixed = !allSelected && !noneSelected;

			if (mixed)
				EditorGUI.showMixedValue = true;

			bool newValue = EditorGUI.Toggle(pRect, allSelected);

			if (mixed)
				EditorGUI.showMixedValue = false;

			if (newValue != allSelected || (mixed && newValue))
			{
				bool targetState = mixed || noneSelected;
				for (int i = 0; i < pVisibleRows.Count; i++)
					SetSelected(pVisibleRows[i].key, targetState);
			}
		}

		private void DrawSortableHeaderLabel(Rect pRect, string pText, SortColumn pColumn)
		{
			string displayText = pText;
			if (m_SortColumn == pColumn)
				displayText += m_SortDirection == SortDirection.Ascending ? " ▲" : " ▼";

			var style = m_SortColumn == pColumn ? EditorStyles.miniBoldLabel : EditorStyles.miniLabel;
			if (GUI.Button(pRect, displayText, style))
			{
				if (m_SortColumn == pColumn)
				{
					m_SortDirection = m_SortDirection == SortDirection.Ascending
						? SortDirection.Descending
						: SortDirection.Ascending;
				}
				else
				{
					m_SortColumn = pColumn;
					m_SortDirection = SortDirection.Ascending;
				}
			}

			EditorGUIUtility.AddCursorRect(pRect, MouseCursor.Link);
		}

		private void SortRows(List<ListRow> pRows)
		{
			if (m_SortColumn == SortColumn.None || pRows.Count <= 1)
				return;

			Comparison<ListRow> comparison;
			switch (m_SortColumn)
			{
				case SortColumn.Key:
					comparison = (pA, pB) => string.Compare(pA.key, pB.key, StringComparison.OrdinalIgnoreCase);
					break;
				case SortColumn.Path:
					comparison = (pA, pB) =>
					{
						var pathA = pA.asset != null ? AssetDatabase.GetAssetPath(pA.asset) : string.Empty;
						var pathB = pB.asset != null ? AssetDatabase.GetAssetPath(pB.asset) : string.Empty;
						return string.Compare(pathA, pathB, StringComparison.OrdinalIgnoreCase);
					};
					break;
				case SortColumn.Usages:
					comparison = (pA, pB) => GetUsageCount(pA.key).CompareTo(GetUsageCount(pB.key));
					break;
				default:
					return;
			}

			if (m_SortDirection == SortDirection.Descending)
			{
				var asc = comparison;
				comparison = (pA, pB) => asc(pB, pA);
			}

			pRows.Sort(comparison);
		}

		public void OnEnable(AssetCatalogWindow pWindow)
		{
			m_Window = pWindow;
			EditorApplication.update += OnEditorUpdate;
		}

		public void OnDisable()
		{
			EditorApplication.update -= OnEditorUpdate;
		}

		private void OnEditorUpdate()
		{
			if (m_PendingSearch == m_AppliedSearch)
				return;
			if (EditorApplication.timeSinceStartup - m_SearchChangeTime < SEARCH_DEBOUNCE_SECONDS)
				return;

			ApplySearch(m_PendingSearch);
		}

		private void ApplySearch(string pSearch)
		{
			m_AppliedSearch = pSearch ?? string.Empty;
			m_AppliedSearchQuery = AssetSearchFilter.ParseQuery(m_AppliedSearch);
			m_ScrollPos = Vector2.zero;
			if (m_Window != null)
				m_Window.Repaint();
		}

		private void ClearSearch()
		{
			m_PendingSearch = string.Empty;
			m_SearchChangeTime = EditorApplication.timeSinceStartup;
			ApplySearch(string.Empty);
		}

		private void InvalidateRowSearchCache()
		{
			m_RowCacheCatalog = null;
			m_RowCacheValid = false;
			m_RowSearchCache.Clear();
		}

		private void EnsureRowSearchCache(AssetCatalog pCatalog)
		{
			if (m_RowCacheValid && m_RowCacheCatalog == pCatalog && m_RowCacheAssetType == m_AssetType)
				return;

			m_RowSearchCache.Clear();
			m_RowCacheCatalog = pCatalog;
			m_RowCacheAssetType = m_AssetType;
			m_RowCacheValid = true;

			switch (m_AssetType)
			{
				case CatalogAssetType.Sprite:
					foreach (var entry in pCatalog.EditorSprites)
						CacheRow(entry.key, entry.category, entry.asset, entry.autoActive);
					break;
				case CatalogAssetType.Texture2D:
					foreach (var entry in pCatalog.EditorTextures)
						CacheRow(entry.key, entry.category, entry.asset, true);
					break;
				case CatalogAssetType.AudioClip:
					foreach (var entry in pCatalog.EditorAudioClips)
						CacheRow(entry.key, entry.category, entry.asset, true);
					break;
			}
		}

		private void CacheRow(string pKey, string pCategory, UnityEngine.Object pAsset, bool pAutoActive)
		{
			m_RowSearchCache.Add(new CachedSearchRow
			{
				key = pKey,
				category = NormalizeCategory(pCategory),
				asset = pAsset,
				autoActive = pAutoActive,
			});
		}

		private string GetCachedSearchText(CachedSearchRow pRow)
		{
			if (pRow.searchText == null)
			{
				var assetPath = pRow.asset != null ? AssetDatabase.GetAssetPath(pRow.asset) : string.Empty;
				pRow.searchText = AssetSearchFilter.Normalize($"{pRow.key ?? string.Empty} {pRow.category} {assetPath}");
			}
			return pRow.searchText;
		}

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
				ClearLinkRestorePreview();
				ClearBatchReport();
				m_DropResult = string.Empty;
				m_SortColumn = SortColumn.None;
				m_SortDirection = SortDirection.Ascending;
				ClearSearch();
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
				ClearLinkRestorePreview();
				ClearBatchReport();
			}
			if (GUILayout.Button("Find All Usages", GUILayout.Width(130), GUILayout.Height(ACTION_BUTTON_HEIGHT)))
			{
				if (!string.IsNullOrEmpty(m_SelectedKey) && GetSavedSelectedAsset(m_Window.Catalog, m_SelectedKey) != m_EditAsset)
				{
					EditorUtility.DisplayDialog("Save Changes Required", "Save or discard asset changes before finding usages.", "OK");
				}
				else
				{
					m_Window.FindUsagesForAllAssets();
					m_Window.RefreshDirectUsageIndex();
					if (!string.IsNullOrEmpty(m_SelectedKey))
						PopulateUsageFromCache(AssetTypeName, m_SelectedKey);
				}
			}
			var canConvertSelectedImageSprites =
				m_AssetType == CatalogAssetType.Sprite && GetSelectedKeys(CatalogAssetType.Sprite).Count > 0;
			var batchConvertContent = new GUIContent(
				"Replace Image Sprites by Sprite Linkers",
				"Scan Addressable prefabs and replace matching Image.sprite references with GeneralSpriteLinker keys.");
			EditorGUI.BeginDisabledGroup(!canConvertSelectedImageSprites);
			if (GUILayout.Button(batchConvertContent, GUILayout.Height(ACTION_BUTTON_HEIGHT)))
				RunBatchSpriteLinkMigration(m_Window.Catalog);
			EditorGUI.EndDisabledGroup();

			var canRemoveSelectedAssets = GetSelectedKeys(m_AssetType).Count > 0;
			EditorGUI.BeginDisabledGroup(!canRemoveSelectedAssets);
			if (GUILayout.Button("Remove Selected Assets", GUILayout.Height(ACTION_BUTTON_HEIGHT)))
				RemoveSelectedAssets(m_Window.Catalog);
			EditorGUI.EndDisabledGroup();

			var canMoveSelectedAssets = GetSelectedKeys(m_AssetType).Count > 0;
			EditorGUI.BeginDisabledGroup(!canMoveSelectedAssets);
			if (GUILayout.Button("Move to Category", GUILayout.Height(ACTION_BUTTON_HEIGHT)))
				ShowMoveToCategoryMenu(m_Window.Catalog);
			EditorGUI.EndDisabledGroup();

			var canPreviewLinkRestore =
				!string.IsNullOrEmpty(m_SelectedKey) &&
				m_EditKey == m_SelectedKey &&
				GetSavedSelectedAsset(m_Window.Catalog, m_SelectedKey) == m_EditAsset &&
				!EditorApplication.isPlayingOrWillChangePlaymode;
			EditorGUI.BeginDisabledGroup(!canPreviewLinkRestore);
			if (GUILayout.Button(
				new GUIContent(
					"Preview Restore & Remove Linkers",
					"Scan every project prefab for this selected key. Preview writes no changes."),
				GUILayout.Height(ACTION_BUTTON_HEIGHT)))
			{
				PreviewLinkRestore(m_Window.Catalog);
			}
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
		}

		private void ShowMoveToCategoryMenu(AssetCatalog pCatalog)
		{
			var menu = new GenericMenu();
			var labels = new HashSet<string>(StringComparer.Ordinal);
			foreach (var category in pCatalog.EditorGetDistinctCategories(m_AssetType))
			{
				var label = NormalizeCategory(category);
				if (!labels.Add(label))
					continue;
				var capturedCategory = category;
				menu.AddItem(new GUIContent(label), false,
					() => MoveSelectedAssets(pCatalog, capturedCategory));
			}
			if (!labels.Contains("Uncategorized"))
			{
				menu.AddItem(new GUIContent("Uncategorized"), false,
					() => MoveSelectedAssets(pCatalog, string.Empty));
			}
			menu.ShowAsContext();
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
				DrawAssetDropZone(pCatalog);
			}

			DrawBatchReport();
			EditorGUILayout.EndVertical();
		}

		private void DrawAssetDropZone(AssetCatalog pCatalog)
		{
			var dropRect = GUILayoutUtility.GetRect(0f, 72f, GUILayout.ExpandWidth(true));
			GUI.Box(dropRect, $"Drag {AssetTypeName} assets here to add them to Uncategorized.");
			HandleAssetDrop(pCatalog, dropRect);

			if (!string.IsNullOrEmpty(m_DropResult))
				EditorGUILayout.HelpBox(m_DropResult, MessageType.Info);
		}

		private List<UnityEngine.Object> ResolveDroppedAssets(UnityEngine.Object[] pDroppedAssets, out int pUnresolvedCount)
		{
			var resolvedAssets = new List<UnityEngine.Object>();
			pUnresolvedCount = 0;

			if (pDroppedAssets == null)
				return resolvedAssets;

			foreach (var asset in pDroppedAssets)
			{
				if (asset == null)
				{
					pUnresolvedCount++;
					continue;
				}

				if (m_AssetType == CatalogAssetType.Sprite && asset is Texture2D texture)
				{
					var path = AssetDatabase.GetAssetPath(texture);
					var spriteCountBefore = resolvedAssets.Count;
					if (!string.IsNullOrEmpty(path))
					{
						var childAssets = AssetDatabase.LoadAllAssetsAtPath(path);
						foreach (var childAsset in childAssets)
						{
							if (childAsset is Sprite sprite)
								resolvedAssets.Add(sprite);
						}
					}

					if (resolvedAssets.Count == spriteCountBefore)
						pUnresolvedCount++;
					continue;
				}

				if (m_AssetType == CatalogAssetType.Sprite && asset is Sprite)
				{
					resolvedAssets.Add(asset);
					continue;
				}

				if (m_AssetType == CatalogAssetType.Texture2D && asset is Texture2D)
				{
					resolvedAssets.Add(asset);
					continue;
				}

				if (m_AssetType == CatalogAssetType.AudioClip && asset is AudioClip)
				{
					resolvedAssets.Add(asset);
					continue;
				}

				pUnresolvedCount++;
			}

			return resolvedAssets;
		}

		private void AddDroppedAsset(AssetCatalog pCatalog, UnityEngine.Object pAsset, string pKey)
		{
			switch (m_AssetType)
			{
				case CatalogAssetType.Sprite:
					pCatalog.EditorAddSprite(pKey, "Uncategorized", pAsset as Sprite);
					break;
				case CatalogAssetType.Texture2D:
					pCatalog.EditorAddTexture(pKey, "Uncategorized", pAsset as Texture2D);
					break;
				case CatalogAssetType.AudioClip:
					pCatalog.EditorAddAudioClip(pKey, "Uncategorized", pAsset as AudioClip);
					break;
			}
		}

		private void HandleAssetDrop(AssetCatalog pCatalog, Rect pDropRect)
		{
			var currentEvent = Event.current;
			if (!pDropRect.Contains(currentEvent.mousePosition))
				return;

			if (currentEvent.type == EventType.DragUpdated)
			{
				var previewAssets = ResolveDroppedAssets(DragAndDrop.objectReferences, out _);
				var hasAddableCandidate = false;
				var seenKeys = new HashSet<string>(StringComparer.Ordinal);
				foreach (var asset in previewAssets)
				{
					if (!string.IsNullOrEmpty(asset.name) &&
						seenKeys.Add(asset.name) &&
						!pCatalog.EditorHasKey(m_AssetType, asset.name))
					{
						hasAddableCandidate = true;
						break;
					}
				}

				DragAndDrop.visualMode = hasAddableCandidate
					? DragAndDropVisualMode.Copy
					: DragAndDropVisualMode.Rejected;
				currentEvent.Use();
				return;
			}

			if (currentEvent.type != EventType.DragPerform)
				return;

			DragAndDrop.AcceptDrag();
			var resolvedAssets = ResolveDroppedAssets(DragAndDrop.objectReferences, out var skippedCount);
			var addedCount = 0;
			var dropKeys = new HashSet<string>(StringComparer.Ordinal);
			foreach (var asset in resolvedAssets)
			{
				if (string.IsNullOrEmpty(asset.name) ||
					!dropKeys.Add(asset.name) || pCatalog.EditorHasKey(m_AssetType, asset.name))
				{
					skippedCount++;
					continue;
				}

				AddDroppedAsset(pCatalog, asset, asset.name);
				addedCount++;
			}

			if (addedCount > 0)
			{
				EditorUtility.SetDirty(pCatalog);
				AssetDatabase.SaveAssets();
				m_Window.InvalidateDirectUsageIndex();
				InvalidateRowSearchCache();
			}

			m_DropResult = $"Added {addedCount}. Skipped {skippedCount}.";
			m_Window.Repaint();
			currentEvent.Use();
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

			if (GetSelectedKeys(CatalogAssetType.Sprite).Count == 0)
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

		private Dictionary<Sprite, SpriteCatalogEntry> BuildSelectedSpriteLookup(AssetCatalog pCatalog, BatchReport pReport)
		{
			var lookup = new Dictionary<Sprite, SpriteCatalogEntry>();
			var selectedKeys = new HashSet<string>(GetSelectedKeys(CatalogAssetType.Sprite), StringComparer.Ordinal);

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
					lookup.Add(entry.asset, entry);
				else
				{
					pReport.imagesSkipped++;
					pReport.lines.Add($"Skipped duplicate Sprite asset for key '{entry.key}'. First selected key stays '{lookup[entry.asset].key}'.");
				}
			}

			return lookup;
		}

		private bool TryConvertPrefab(string pPrefabPath, IReadOnlyDictionary<Sprite, SpriteCatalogEntry> pSelectedSpriteLookup, BatchReport pReport)
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
					if (!pSelectedSpriteLookup.TryGetValue(sprite, out var spriteEntry))
						continue;

					var spriteKey = spriteEntry.key;
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
					linker.AutoActive = spriteEntry.autoActive;
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
						updateCount += UpdateLinkerKeys<GeneralSpriteRendererLinker>(root, pOldKey, pNewKey);
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

		private void ClearLinkRestorePreview()
		{
			m_LinkRestorePreview = null;
			m_LinkRestorePreviewKey = string.Empty;
			m_LinkRestorePreviewType = default;
			m_IsLinkRestoreReport = false;
		}

		private void PreviewLinkRestore(AssetCatalog pCatalog)
		{
			ClearBatchReport();
			ClearLinkRestorePreview();
			m_LinkRestorePreview = LinkerRestoreService.Preview(pCatalog, m_AssetType, m_SelectedKey);
			m_LinkRestorePreviewKey = m_SelectedKey;
			m_LinkRestorePreviewType = m_AssetType;
			m_IsLinkRestoreReport = true;
			SetLinkRestoreReport(LINK_RESTORE_PREVIEW_TITLE, m_LinkRestorePreview);
			m_Window.Repaint();

			// Preview writes nothing and the report renders inline below the edit panel, where it can scroll
			// out of view. Without this dialog, a preview that finds zero candidates or fails validation looks
			// identical to the button doing nothing.
			if (!m_LinkRestorePreview.IsValid)
			{
				EditorUtility.DisplayDialog("Preview Restore Linkers", m_LinkRestorePreview.validationError, "OK");
			}
			else if (m_LinkRestorePreview.cancelled)
			{
				EditorUtility.DisplayDialog("Preview Restore Linkers", "Preview cancelled.", "OK");
			}
			else if (!m_LinkRestorePreview.HasCandidates)
			{
				EditorUtility.DisplayDialog(
					"Preview Restore Linkers",
					$"No General*Linker components found for {m_AssetType} key '{m_SelectedKey}' in {m_LinkRestorePreview.prefabsScanned} scanned prefab(s).",
					"OK");
			}
			else if (EditorUtility.DisplayDialog(
				"Preview Restore Linkers",
				$"Found {m_LinkRestorePreview.candidatesFound} valid linker(s) for {m_AssetType} key '{m_SelectedKey}' " +
				$"in {m_LinkRestorePreview.affectedAssets.Count} prefab(s). Proceed to restore confirmation?",
				"Continue",
				"Not Now"))
			{
				ConfirmAndRunLinkRestore();
			}
		}

		private void SetLinkRestoreReport(string pTitle, LinkerRestoreService.LinkerRestoreResult pResult)
		{
			m_IsLinkRestoreReport = true;
			if (pResult == null)
				return;

			var target = $"{m_LinkRestorePreviewType} key '{m_LinkRestorePreviewKey}'";
			var isPreview = pTitle == LINK_RESTORE_PREVIEW_TITLE;
			m_BatchReportTitle = $"{pTitle} - {target}: {pResult.candidatesFound} valid linker(s) in " +
				$"{pResult.affectedAssets.Count} prefab(s) ({pResult.prefabsScanned} prefab(s) scanned)";
			m_BatchReportLines.Add(isPreview
				? $"Preview target: {target}. Preview writes no project changes."
				: $"Restore target: {target}.");
			if (!string.IsNullOrEmpty(pResult.validationError))
				m_BatchReportLines.Add($"Validation error: {pResult.validationError}");
			m_BatchReportLines.Add($"cancelled: {pResult.cancelled}");
			m_BatchReportLines.Add(isPreview
				? $"Preflight candidate estimate: {pResult.candidatesFound}"
				: $"Candidates found during restore: {pResult.candidatesFound}");
			m_BatchReportLines.Add($"Actual linkers restored and saved: {pResult.linkersRestored}");
			m_BatchReportLines.Add($"Changed prefabs: {pResult.changedAssets.Count} ({pResult.prefabsChanged} prefab(s))");

			if (pResult.affectedAssets.Count > 0)
			{
				m_BatchReportLines.Add("Affected assets:");
				foreach (var path in pResult.affectedAssets)
					m_BatchReportLines.Add($"  {AssetCatalogEditorGui.StripAssetsPrefix(path)}");
			}

			if (pResult.skipped.Count > 0)
			{
				m_BatchReportLines.Add("Skipped:");
				foreach (var issue in pResult.skipped)
					m_BatchReportLines.Add($"  {issue}");
			}

			if (pResult.failures.Count > 0)
			{
				m_BatchReportLines.Add("Failures:");
				foreach (var failure in pResult.failures)
					m_BatchReportLines.Add($"  {failure}");
			}

			m_BatchReportLines.Add("AssetCatalog entry was not deleted.");
		}

		private void ConfirmAndRunLinkRestore()
		{
			var canContinueRestore = m_IsLinkRestoreReport &&
				m_LinkRestorePreview != null &&
				m_LinkRestorePreview.IsValid &&
				!m_LinkRestorePreview.cancelled &&
				m_LinkRestorePreview.HasCandidates &&
				m_LinkRestorePreviewKey == m_SelectedKey &&
				m_LinkRestorePreviewType == m_AssetType;
			if (!canContinueRestore)
				return;

			if (!EditorUtility.DisplayDialog(
				"Restore Linkers — Warning",
				"This writes direct asset references and removes matching General*Linker components from every scanned prefab. " +
				"Batch changes cannot be undone as one operation. AssetCatalog entry will remain. Continue?",
				"Continue",
				"Cancel"))
			{
				return;
			}

			var affectedCount = m_LinkRestorePreview.affectedAssets.Count;
			if (!EditorUtility.DisplayDialog(
				"Restore Linkers — Final Confirmation",
				$"Preflight estimate: restore and remove {m_LinkRestorePreview.candidatesFound} linker(s) for {m_LinkRestorePreviewType} key " +
				$"'{m_LinkRestorePreviewKey}' in {affectedCount} prefab(s). Restore rescans project Assets, " +
				"so the final report states the actual restored count.\n\n" +
				"This writes project assets. AssetCatalog entry will not be deleted.",
				"Restore & Remove",
				"Cancel"))
			{
				return;
			}

			ClearBatchReport();
			var restoreType = m_LinkRestorePreviewType;
			var restoreKey = m_LinkRestorePreviewKey;
			m_LinkRestorePreview = null;
			var result = LinkerRestoreService.Restore(m_Window.Catalog, restoreType, restoreKey);
			SetLinkRestoreReport(GetLinkRestoreOutcomeTitle(result), result);
			m_Window.InvalidateDirectUsageIndex();
			LogLinkRestoreReportToConsole();
			AssetDatabase.Refresh();
			m_Window.Repaint();
		}

		private static string GetLinkRestoreOutcomeTitle(LinkerRestoreService.LinkerRestoreResult pResult)
		{
			if (pResult == null || !pResult.IsValid)
				return "Restore Validation Failed";
			if (pResult.cancelled)
				return pResult.linkersRestored > 0 ? "Restore Cancelled (Partial)" : "Restore Cancelled";
			if (pResult.linkersRestored == 0)
				return "Restore Not Applied";
			return "Restore Complete";
		}

		private void LogLinkRestoreReportToConsole()
		{
			// AssetDatabase.Refresh can trigger a domain reload that clears this in-memory report, so the
			// irreversible batch result is written to Console first as a durable record.
			Debug.Log($"[Asset Catalog] {m_BatchReportTitle}\n{string.Join("\n", m_BatchReportLines)}");
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
			m_Window.InvalidateDirectUsageIndex();
			InvalidateRowSearchCache();
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
			{
				if (m_ActiveCategory != categories[nextCategoryIndex])
				{
					m_SortColumn = SortColumn.None;
					m_SortDirection = SortDirection.Ascending;
				}

				m_ActiveCategory = categories[nextCategoryIndex];
			}

			DrawSearchField();

			var rows = BuildRows(pCatalog, true);
			if (rows.Count == 0)
			{
				if (string.IsNullOrWhiteSpace(m_AppliedSearch))
				{
					EditorGUILayout.HelpBox($"No {AssetTypeName} entries in this category.", MessageType.Info);
					return;
				}

				DrawListHeader(pContentWidth, rows);
				EditorGUILayout.HelpBox($"No {AssetTypeName} entries matching search.", MessageType.Info);
				return;
			}

			SortRows(rows);
			DrawListHeader(pContentWidth, rows);
			var scrollPos = new Vector2(0f, m_ScrollPos.y);
			scrollPos = GUILayout.BeginScrollView(scrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandHeight(true));
			for (int i = 0; i < rows.Count; i++)
				DrawListRow(rows[i], pContentWidth, i);
			GUILayout.EndScrollView();
			m_ScrollPos = new Vector2(0f, scrollPos.y);
		}

		private void DrawSearchField()
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginChangeCheck();
			var nextSearch = EditorGUILayout.TextField("Search", m_PendingSearch);
			if (EditorGUI.EndChangeCheck())
			{
				m_PendingSearch = nextSearch;
				m_SearchChangeTime = EditorApplication.timeSinceStartup;
				if (string.IsNullOrEmpty(m_PendingSearch))
					ApplySearch(string.Empty);
			}

			if (GUILayout.Button("x", GUILayout.Width(22f)))
				ClearSearch();
			EditorGUILayout.EndHorizontal();
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
			EnsureRowSearchCache(pCatalog);
			var rows = new List<ListRow>();
			foreach (var cachedRow in m_RowSearchCache)
			{
				if (pFilterActiveCategory && m_ActiveCategory != "All" && cachedRow.category != m_ActiveCategory)
					continue;

				var searchText = pFilterActiveCategory ? GetCachedSearchText(cachedRow) : null;
				if (pFilterActiveCategory && !AssetSearchFilter.Matches(searchText, m_AppliedSearchQuery))
					continue;

				rows.Add(new ListRow
				{
					key = cachedRow.key,
					category = cachedRow.category,
					asset = cachedRow.asset,
					autoActive = cachedRow.autoActive,
					searchText = searchText,
				});
			}
			return rows;
		}

		private void DrawListHeader(float pContentWidth, List<ListRow> pVisibleRows)
		{
			var headerRect = DrawHeaderBackground(LIST_HEADER_HEIGHT);
			var x = headerRect.x;

			var checkboxRect = new Rect(x + 4f, headerRect.y + (headerRect.height - 16f) * 0.5f, 16f, 16f);
			DrawSelectAllCheckbox(checkboxRect, pVisibleRows);
			x += LIST_CHECKBOX_WIDTH;
			x += LIST_ICON_WIDTH;

			var labelRect = new Rect(x, headerRect.y, LIST_KEY_WIDTH, headerRect.height);
			DrawSortableHeaderLabel(labelRect, "Key", SortColumn.Key);
			x += LIST_KEY_WIDTH;

			labelRect = new Rect(x, headerRect.y, GetPathWidth(pContentWidth), headerRect.height);
			DrawSortableHeaderLabel(labelRect, "Path", SortColumn.Path);
			x += labelRect.width;

			labelRect = new Rect(x, headerRect.y, LIST_USAGE_WIDTH, headerRect.height);
			DrawSortableHeaderLabel(labelRect, "Usages", SortColumn.Usages);
			x += LIST_USAGE_WIDTH;

			if (m_AssetType == CatalogAssetType.Sprite)
			{
				labelRect = new Rect(x, headerRect.y, LIST_AUTO_ACTIVE_WIDTH, headerRect.height);
				DrawHeaderLabel(labelRect, "Auto Active");
				x += LIST_AUTO_ACTIVE_WIDTH;
			}

			labelRect = new Rect(x, headerRect.y, LIST_SELECT_WIDTH, headerRect.height);
			DrawHeaderLabel(labelRect, "Action");
		}

		private void DrawUsageHeader()
		{
			var headerRect = DrawHeaderBackground(LIST_HEADER_HEIGHT);
			var x = headerRect.x + USAGE_ICON_WIDTH + 4f;

			var prefabWidth = headerRect.width - USAGE_ICON_WIDTH - 4f - USAGE_COMPONENT_WIDTH - 4f - USAGE_SELECT_WIDTH;
			var labelRect = new Rect(x, headerRect.y, prefabWidth, headerRect.height);
			DrawHeaderLabel(labelRect, "Prefab");
			x += prefabWidth + 4f;

			labelRect = new Rect(x, headerRect.y, USAGE_COMPONENT_WIDTH, headerRect.height);
			DrawHeaderLabel(labelRect, "Component");
			x += USAGE_COMPONENT_WIDTH + 4f;

			labelRect = new Rect(x, headerRect.y, USAGE_SELECT_WIDTH, headerRect.height);
			DrawHeaderLabel(labelRect, "Action");
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

			Rect autoActiveRect = default;
			if (m_AssetType == CatalogAssetType.Sprite)
			{
				autoActiveRect = new Rect(x, y, LIST_AUTO_ACTIVE_WIDTH, rowHeight);
				x += LIST_AUTO_ACTIVE_WIDTH;
			}

			var selectRect = new Rect(x, y, LIST_SELECT_WIDTH, rowHeight);

			var selected = IsSelected(pRow.key);
			var nextSelected = GUI.Toggle(checkboxRect, selected, GUIContent.none);
			if (nextSelected != selected)
				SetSelected(pRow.key, nextSelected);

			DrawListIcon(iconRect, pRow.asset);
			GUI.Label(keyRect, pRow.key, EditorStyles.miniLabel);
			var path = pRow.asset != null ? AssetDatabase.GetAssetPath(pRow.asset) : string.Empty;
			GUI.Label(pathRect, AssetCatalogEditorGui.StripAssetsPrefix(path), EditorStyles.miniLabel);

			if (pRow.asset != null)
			{
				EditorGUIUtility.AddCursorRect(iconRect, MouseCursor.Link);
				EditorGUIUtility.AddCursorRect(keyRect, MouseCursor.Link);
				EditorGUIUtility.AddCursorRect(pathRect, MouseCursor.Link);
				var e = Event.current;
				if (e.type == EventType.MouseDown && e.button == 0 &&
					(iconRect.Contains(e.mousePosition) || keyRect.Contains(e.mousePosition) || pathRect.Contains(e.mousePosition)))
				{
					EditorGUIUtility.PingObject(pRow.asset);
					e.Use();
				}
			}
			GUI.Label(usageRect, GetUsageCount(pRow.key).ToString(), EditorStyles.miniLabel);

			if (m_AssetType == CatalogAssetType.Sprite)
			{
				var nextAutoActive = EditorGUI.Toggle(autoActiveRect, pRow.autoActive);
				if (nextAutoActive != pRow.autoActive)
				{
					m_Window.Catalog.EditorUpdateSpriteAutoActive(pRow.key, nextAutoActive);
					InvalidateRowSearchCache();
					m_Window.Repaint();
				}
			}

			if (GUI.Button(selectRect, "Select"))
				SelectEntry(pRow.key, pRow.category, pRow.asset);
		}

		private void DrawListIcon(Rect pRect, UnityEngine.Object pAsset)
		{
			if (pAsset == null)
				return;

			if (pAsset is Sprite sprite && TryDrawSpriteIcon(pRect, sprite))
				return;

			var preview = AssetPreview.GetAssetPreview(pAsset);
			if (preview == null)
				preview = AssetPreview.GetMiniThumbnail(pAsset);
			if (preview != null)
				GUI.DrawTexture(pRect, preview, ScaleMode.ScaleToFit);
		}

		private static bool TryDrawSpriteIcon(Rect pRect, Sprite pSprite)
		{
			if (pSprite == null || pSprite.texture == null ||
				(pSprite.packed && pSprite.packingMode == SpritePackingMode.Tight))
				return false;

			var textureRect = pSprite.textureRect;
			if (textureRect.width <= 0f || textureRect.height <= 0f)
				return false;

			var scale = Mathf.Min(pRect.width / textureRect.width, pRect.height / textureRect.height);
			var spriteRect = new Rect(
				pRect.x + (pRect.width - textureRect.width * scale) * 0.5f,
				pRect.y + (pRect.height - textureRect.height * scale) * 0.5f,
				textureRect.width * scale,
				textureRect.height * scale);
			var texCoords = new Rect(
				textureRect.x / pSprite.texture.width,
				textureRect.y / pSprite.texture.height,
				textureRect.width / pSprite.texture.width,
				textureRect.height / pSprite.texture.height);
			GUI.DrawTextureWithTexCoords(spriteRect, pSprite.texture, texCoords, true);
			return true;
		}

		private int GetUsageCount(string pKey)
		{
			var cacheKey = AssetCatalogWindow.GetCacheKey(AssetTypeName, pKey);
			if (!m_Window.UsageCache.TryGetValue(cacheKey, out var entry) || entry.usages == null)
				return 0;
			return entry.usages.Count;
		}

		private HashSet<string> GetSelectedKeys(CatalogAssetType pType)
		{
			if (!m_SelectedKeysByType.TryGetValue(pType, out var keys))
			{
				keys = new HashSet<string>(StringComparer.Ordinal);
				m_SelectedKeysByType.Add(pType, keys);
			}
			return keys;
		}

		private bool IsSelected(string pKey)
		{
			return !string.IsNullOrEmpty(pKey) && GetSelectedKeys(m_AssetType).Contains(pKey);
		}

		private void SetSelected(string pKey, bool pSelected)
		{
			if (string.IsNullOrEmpty(pKey))
				return;

			var keys = GetSelectedKeys(m_AssetType);
			if (pSelected)
				keys.Add(pKey);
			else
				keys.Remove(pKey);
		}

		private void ClearBatchReport()
		{
			m_IsKeyMigrationReport = false;
			m_IsLinkRestoreReport = false;
			m_BatchReportTitle = string.Empty;
			m_BatchReportLines.Clear();
			m_BatchReportScroll = Vector2.zero;
		}

		private void RemapKeySelection(string pOldKey, string pNewKey)
		{
			if (string.IsNullOrEmpty(pOldKey) || string.IsNullOrEmpty(pNewKey))
				return;

			var selectedKeys = GetSelectedKeys(m_AssetType);
			if (selectedKeys.Remove(pOldKey))
				selectedKeys.Add(pNewKey);
		}

		private float GetPathWidth(float pContentWidth)
		{
			var autoActiveWidth = m_AssetType == CatalogAssetType.Sprite ? LIST_AUTO_ACTIVE_WIDTH : 0f;
			return Mathf.Max(120f, pContentWidth - LIST_CHECKBOX_WIDTH - LIST_ICON_WIDTH - LIST_KEY_WIDTH - LIST_USAGE_WIDTH - autoActiveWidth - LIST_SELECT_WIDTH - 40f);
		}

		private void SelectEntry(string pKey, string pCategory, UnityEngine.Object pAsset)
		{
			// A preview belongs to one key and asset type. Selecting a different target must clear stored
			// preview state even when another batch report has replaced its display. Search, category, and sort
			// state stay untouched. A visible restore report clears with its stale target.
			var isDifferentRestoreTarget =
				m_LinkRestorePreviewKey != pKey || m_LinkRestorePreviewType != m_AssetType;
			if ((m_LinkRestorePreview != null || m_IsLinkRestoreReport) && isDifferentRestoreTarget)
			{
				if (m_IsLinkRestoreReport)
					ClearBatchReport();
				ClearLinkRestorePreview();
			}

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
				ClearLinkRestorePreview();
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

			ClearLinkRestorePreview();
			var previousAsset = GetSavedSelectedAsset(pCatalog, m_SelectedKey);
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

			InvalidateRowSearchCache();
			EditorUtility.SetDirty(pCatalog);
			AssetDatabase.SaveAssets();
			if (previousAsset != m_EditAsset)
				m_Window.InvalidateDirectUsageIndex();
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

		private void RemoveSelectedAssets(AssetCatalog pCatalog)
		{
			var selectedKeys = new List<string>(GetSelectedKeys(m_AssetType));
			selectedKeys.Sort(StringComparer.Ordinal);
			if (pCatalog == null || selectedKeys.Count == 0)
				return;

			ClearLinkRestorePreview();
			ClearBatchReport();
			var usageEntries = m_Window.FindUsagesForSelectedKeys(AssetTypeName, selectedKeys, out var skippedPrefabs);
			var usageCounts = new Dictionary<string, int>(StringComparer.Ordinal);
			var referencedKeys = new List<string>();
			var totalUsages = 0;
			foreach (var key in selectedKeys)
			{
				var cacheKey = AssetCatalogWindow.GetCacheKey(AssetTypeName, key);
				var count = usageEntries.TryGetValue(cacheKey, out var entry) && entry.usages != null
					? entry.usages.Count
					: 0;
				usageCounts.Add(key, count);
				totalUsages += count;
				if (count > 0)
					referencedKeys.Add(key);
			}

			var dialogMessage = BuildRemovalConfirmationMessage(selectedKeys, usageCounts, referencedKeys, totalUsages);
			if (!EditorUtility.DisplayDialog("Remove Selected Assets", dialogMessage, "Continue", "Cancel"))
				return;

			foreach (var key in selectedKeys)
			{
				pCatalog.EditorDeleteEntry(m_AssetType, key);
				m_Window.UsageCache.Remove(AssetCatalogWindow.GetCacheKey(AssetTypeName, key));
			}
			m_Window.SaveUsageCache();

			EditorUtility.SetDirty(pCatalog);
			AssetDatabase.SaveAssets();
			GetSelectedKeys(m_AssetType).Clear();
			InvalidateRowSearchCache();
			m_SelectedKey = string.Empty;
			ClearUsageResults();
			m_Window.InvalidateDirectUsageIndex();
			SetRemovalReport(selectedKeys, usageCounts, referencedKeys, skippedPrefabs);
			m_Window.Repaint();
		}

		private string BuildRemovalConfirmationMessage(
			IReadOnlyList<string> pKeys,
			IReadOnlyDictionary<string, int> pUsageCounts,
			IReadOnlyCollection<string> pReferencedKeys,
			int pTotalUsages)
		{
			var message = $"Remove {pKeys.Count} selected {AssetTypeName} asset(s)?\n\n{string.Join("\n", pKeys)}";
			if (pReferencedKeys.Count > 0)
			{
				message += $"\n\n{pReferencedKeys.Count} key(s) are still referenced by {pTotalUsages} linker(s)/prefab(s). " +
					"Removing catalog entries does not modify or remove those linkers. " +
					"Those keys will become unresolved.";
			}
			return message;
		}

		private void SetRemovalReport(
			IReadOnlyList<string> pKeys,
			IReadOnlyDictionary<string, int> pUsageCounts,
			IReadOnlyCollection<string> pReferencedKeys,
			IReadOnlyCollection<string> pSkippedPrefabs)
		{
			m_BatchReportTitle = $"Remove Selected Assets: {pKeys.Count} asset(s) removed";
			foreach (var key in pKeys)
				m_BatchReportLines.Add($"{AssetTypeName} '{key}': {pUsageCounts[key]} usage(s) before removal.");

			if (pReferencedKeys.Count > 0)
				m_BatchReportLines.Add($"{pReferencedKeys.Count} referenced key(s) are now unresolved; linkers were not modified.");
			if (pSkippedPrefabs.Count > 0)
				m_BatchReportLines.Add($"Usage scan skipped {pSkippedPrefabs.Count} prefab file(s).");
		}

		private void MoveSelectedAssets(AssetCatalog pCatalog, string pTargetCategory)
		{
			var selectedKeys = new List<string>(GetSelectedKeys(m_AssetType));
			selectedKeys.Sort(StringComparer.Ordinal);
			if (pCatalog == null || selectedKeys.Count == 0)
				return;

			ClearLinkRestorePreview();
			ClearBatchReport();

			var selectedSet = new HashSet<string>(selectedKeys, StringComparer.Ordinal);
			var sourceCategories = new Dictionary<string, string>(StringComparer.Ordinal);
			switch (m_AssetType)
			{
				case CatalogAssetType.Sprite:
					foreach (var entry in pCatalog.EditorSprites)
					{
						if (selectedSet.Contains(entry.key))
							sourceCategories[entry.key] = entry.category;
					}
					break;
				case CatalogAssetType.Texture2D:
					foreach (var entry in pCatalog.EditorTextures)
					{
						if (selectedSet.Contains(entry.key))
							sourceCategories[entry.key] = entry.category;
					}
					break;
				case CatalogAssetType.AudioClip:
					foreach (var entry in pCatalog.EditorAudioClips)
					{
						if (selectedSet.Contains(entry.key))
							sourceCategories[entry.key] = entry.category;
					}
					break;
			}

			var targetLabel = NormalizeCategory(pTargetCategory);
			var dialogMessage = BuildMoveConfirmationMessage(selectedKeys, sourceCategories, targetLabel);
			if (!EditorUtility.DisplayDialog("Move Selected Assets", dialogMessage, "Continue", "Cancel"))
				return;

			int movedCount = pCatalog.EditorSetCategory(m_AssetType, selectedKeys, pTargetCategory);

			EditorUtility.SetDirty(pCatalog);
			AssetDatabase.SaveAssets();

			m_SelectedKey = string.Empty;
			ClearUsageResults();
			InvalidateRowSearchCache();
			SetMoveReport(selectedKeys, sourceCategories, targetLabel, movedCount);
			m_Window.Repaint();
		}

		private string BuildMoveConfirmationMessage(
			IReadOnlyList<string> pKeys,
			IReadOnlyDictionary<string, string> pSourceCategories,
			string pTargetLabel)
		{
			var sourceLabels = new HashSet<string>(StringComparer.Ordinal);
			foreach (var key in pKeys)
			{
				if (pSourceCategories.TryGetValue(key, out var category))
					sourceLabels.Add(NormalizeCategory(category));
				else
					sourceLabels.Add(NormalizeCategory(string.Empty));
			}
			var sortedSources = new List<string>(sourceLabels);
			sortedSources.Sort(StringComparer.Ordinal);
			return $"Move {pKeys.Count} selected {AssetTypeName} asset(s) from {string.Join(", ", sortedSources)} to '{pTargetLabel}'?";
		}

		private void SetMoveReport(
			IReadOnlyList<string> pKeys,
			IReadOnlyDictionary<string, string> pSourceCategories,
			string pTargetLabel,
			int pMovedCount)
		{
			int skippedCount = pKeys.Count - pMovedCount;
			m_BatchReportTitle = $"Move Selected Assets: {pMovedCount} {AssetTypeName} asset(s) moved to {pTargetLabel}";
			foreach (var key in pKeys)
			{
				if (pSourceCategories.TryGetValue(key, out var sourceCategory))
					m_BatchReportLines.Add($"{AssetTypeName} '{key}': '{NormalizeCategory(sourceCategory)}' -> '{pTargetLabel}'.");
				else
					m_BatchReportLines.Add($"{AssetTypeName} '{key}': skipped (key not found in catalog).");
			}
			if (skippedCount > 0)
				m_BatchReportLines.Add($"{skippedCount} selected key(s) were not found and were skipped.");
		}

		private void DeleteSelectedEntry(AssetCatalog pCatalog)
		{
			if (!EditorHelper.ConfirmPopup($"Are you sure you want to delete '{m_SelectedKey}'?", "Yes", "No"))
				return;

			var deletedKey = m_SelectedKey;
			SetSelected(deletedKey, false);
			ClearLinkRestorePreview();
			ClearBatchReport();
			pCatalog.EditorDeleteEntry(m_AssetType, deletedKey);
			InvalidateRowSearchCache();
			m_SelectedKey = string.Empty;
			ClearUsageResults();
			RemoveCacheEntry(AssetTypeName, deletedKey);
			EditorUtility.SetDirty(pCatalog);
			AssetDatabase.SaveAssets();
			m_Window.InvalidateDirectUsageIndex();
			m_Window.Repaint();
		}

		private void ClearUsageResults()
		{
			m_UsageKey = string.Empty;
			m_UsageResults.Clear();
			m_UsageSkippedAssets.Clear();
			m_DirectUsageResults.Clear();
			m_DirectUsageSkippedAssets.Clear();
			m_DirectUsageAsset = null;
		}

		private void PopulateUsageFromCache(string pAssetType, string pKey)
		{
			m_UsageResults.Clear();
			m_UsageSkippedAssets.Clear();
			m_UsageKey = pKey;

			var cacheKey = AssetCatalogWindow.GetCacheKey(pAssetType, pKey);
			if (m_Window.UsageCache.TryGetValue(cacheKey, out var entry))
			{
				m_UsageResults.AddRange(entry.usages ?? new List<AssetCatalogWindow.UsageResult>());
				m_UsageSkippedAssets.AddRange(m_Window.CacheSkippedPrefabs ?? new List<string>());
				m_UsageSkippedAssets.AddRange(entry.skippedPrefabs ?? new List<string>());
			}
			PopulateDirectUsagesFromCache();
		}

		private void PopulateDirectUsagesFromCache()
		{
			m_DirectUsageResults.Clear();
			m_DirectUsageSkippedAssets.Clear();
			m_DirectUsageAsset = null;
			if (m_EditAsset == null)
				return;

			if (!m_Window.TryGetDirectUsages(m_EditAsset, AssetTypeName, out var assetPaths, out var skippedAssets) &&
				!m_Window.FindDirectUsages(m_EditAsset, AssetTypeName, out assetPaths, out skippedAssets))
				return;

			m_DirectUsageResults.AddRange(assetPaths);
			m_DirectUsageSkippedAssets.AddRange(skippedAssets);
			m_DirectUsageAsset = m_EditAsset;
		}

		private void RemoveCacheEntry(string pAssetType, string pKey)
		{
			var cacheKey = AssetCatalogWindow.GetCacheKey(pAssetType, pKey);
			if (m_Window.UsageCache.Remove(cacheKey))
				m_Window.SaveUsageCache();
		}

		private UnityEngine.Object GetSavedSelectedAsset(AssetCatalog pCatalog, string pKey)
		{
			if (pCatalog == null)
				return null;

			switch (m_AssetType)
			{
				case CatalogAssetType.Sprite:
					foreach (var entry in pCatalog.EditorSprites)
						if (entry.key == pKey)
							return entry.asset;
					break;
				case CatalogAssetType.Texture2D:
					foreach (var entry in pCatalog.EditorTextures)
						if (entry.key == pKey)
							return entry.asset;
					break;
				case CatalogAssetType.AudioClip:
					foreach (var entry in pCatalog.EditorAudioClips)
						if (entry.key == pKey)
							return entry.asset;
					break;
			}
			return null;
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
			if (GetSavedSelectedAsset(m_Window.Catalog, key) != m_EditAsset)
			{
				EditorUtility.DisplayDialog("Save Changes Required", "Save or discard asset changes before finding usages.", "OK");
				return;
			}

			var assetType = AssetTypeName;
			var cacheKey = AssetCatalogWindow.GetCacheKey(assetType, key);
			if (!m_Window.HasCompleteUsageIndex || !m_Window.UsageCache.ContainsKey(cacheKey))
				m_Window.FindUsagesForAllAssets();

			PopulateUsageFromCache(assetType, key);
			m_DirectUsageResults.Clear();
			m_DirectUsageSkippedAssets.Clear();
			if (m_EditAsset == null)
			{
				EditorUtility.DisplayDialog("Find Direct Asset References", "Selected catalog asset is missing.", "OK");
			}
			else if (m_Window.FindDirectUsages(m_EditAsset, assetType, out var directUsages, out var directSkippedAssets))
			{
				m_DirectUsageResults.AddRange(directUsages);
				m_DirectUsageSkippedAssets.AddRange(directSkippedAssets);
				m_DirectUsageAsset = m_EditAsset;
			}
			else
			{
				EditorUtility.DisplayDialog("Find Direct Asset References", "Cannot resolve selected asset GUID and local file ID.", "OK");
			}
			m_Window.Repaint();
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
				DrawUsageHeader();
				for (int i = 0; i < m_UsageResults.Count; i++)
					DrawUsageResultRow(m_UsageResults[i], i);
				EditorGUILayout.EndScrollView();
			}

			if (m_UsageSkippedAssets.Count > 0)
			{
				GUILayout.Space(8);
				EditorGUILayout.HelpBox($"Skipped {m_UsageSkippedAssets.Count} prefab(s):\n- {string.Join("\n- ", m_UsageSkippedAssets.ConvertAll(AssetCatalogEditorGui.StripAssetsPrefix))}", MessageType.Warning);
			}

			EditorGUILayout.EndVertical();
			DrawDirectUsageResults();
		}

		private void DrawDirectUsageResults()
		{
			if (m_EditAsset == null)
				return;
			if (m_DirectUsageAsset != null && m_DirectUsageAsset != m_EditAsset)
			{
				m_DirectUsageResults.Clear();
				m_DirectUsageSkippedAssets.Clear();
				m_DirectUsageAsset = null;
			}

			GUILayout.Space(8);
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField($"Direct Asset References for '{AssetCatalogEditorGui.StripAssetsPrefix(AssetDatabase.GetAssetPath(m_EditAsset))}'", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Display", GUILayout.Width(52f));
			m_ShowAddressableDirectUsagesOnly = GUILayout.Toolbar(m_ShowAddressableDirectUsagesOnly ? 1 : 0, new[] { "All Assets", "Addressable Assets Only" }, GUILayout.Height(20f)) == 1;
			EditorGUILayout.EndHorizontal();

			var displayPaths = GetDisplayedDirectUsagePaths();
			if (displayPaths.Count == 0)
			{
				var message = m_ShowAddressableDirectUsagesOnly
					? "No direct references found in Addressable assets."
					: "No direct asset references found. Click Find Usages to scan project assets.";
				EditorGUILayout.HelpBox(message, MessageType.Info);
			}
			else
			{
				EditorGUILayout.HelpBox($"Found {displayPaths.Count} direct reference(s).", MessageType.Info);
				m_DirectUsageScrollPos = EditorGUILayout.BeginScrollView(m_DirectUsageScrollPos, GUILayout.MinHeight(140f), GUILayout.ExpandHeight(true));
				DrawDirectUsageHeader();
				for (var i = 0; i < displayPaths.Count; i++)
					DrawDirectUsageResultRow(displayPaths[i], i);
				EditorGUILayout.EndScrollView();
			}

			if (m_DirectUsageSkippedAssets.Count > 0)
				EditorGUILayout.HelpBox($"Skipped {m_DirectUsageSkippedAssets.Count} asset(s):\n- {string.Join("\n- ", m_DirectUsageSkippedAssets.ConvertAll(AssetCatalogEditorGui.StripAssetsPrefix))}", MessageType.Warning);

			EditorGUILayout.EndVertical();
		}

		private List<string> GetDisplayedDirectUsagePaths()
		{
			if (!m_ShowAddressableDirectUsagesOnly)
				return m_DirectUsageResults;

			var result = new List<string>();
			foreach (var path in m_DirectUsageResults)
			{
				var guid = AssetDatabase.AssetPathToGUID(path);
				if (!string.IsNullOrEmpty(guid) && AddressableEditorHelper.IncludedInBuild(guid))
					result.Add(path);
			}
			return result;
		}

		private void DrawDirectUsageHeader()
		{
			var headerRect = DrawHeaderBackground(LIST_HEADER_HEIGHT);
			var pathRect = new Rect(headerRect.x + USAGE_ICON_WIDTH + 4f, headerRect.y, headerRect.width - USAGE_ICON_WIDTH - 4f - USAGE_SELECT_WIDTH, headerRect.height);
			DrawHeaderLabel(pathRect, "Asset");
			DrawHeaderLabel(new Rect(pathRect.xMax, headerRect.y, USAGE_SELECT_WIDTH, headerRect.height), "Action");
		}

		private void DrawDirectUsageResultRow(string pAssetPath, int pIndex)
		{
			var rowRect = EditorGUILayout.GetControlRect(false, USAGE_ROW_HEIGHT);
			if (pIndex % 2 == 0)
				EditorGUI.DrawRect(rowRect, new Color(0f, 0f, 0f, 0.1f));

			var iconRect = new Rect(rowRect.x, rowRect.y + 1f, USAGE_ICON_WIDTH, USAGE_ROW_HEIGHT - 2f);
			var actionRect = new Rect(rowRect.xMax - USAGE_SELECT_WIDTH, rowRect.y + 2f, USAGE_SELECT_WIDTH, USAGE_ROW_HEIGHT - 4f);
			var pathRect = new Rect(iconRect.xMax + 4f, rowRect.y + 2f, actionRect.x - iconRect.xMax - 8f, USAGE_ROW_HEIGHT - 4f);
			var icon = AssetDatabase.GetCachedIcon(pAssetPath);
			if (icon != null)
				GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
			GUI.Label(pathRect, AssetCatalogEditorGui.StripAssetsPrefix(pAssetPath), EditorStyles.miniLabel);
			if (GUI.Button(actionRect, "Select"))
			{
				var asset = AssetDatabase.LoadMainAssetAtPath(pAssetPath);
				if (asset != null)
				{
					Selection.activeObject = asset;
					EditorGUIUtility.PingObject(asset);
				}
			}
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
			GUI.Label(pathRect, AssetCatalogEditorGui.StripAssetsPrefix(pResult.prefabPath), EditorStyles.miniLabel);
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

			if (m_IsLinkRestoreReport)
			{
				var canContinueRestore = m_LinkRestorePreview != null &&
					m_LinkRestorePreview.IsValid &&
					!m_LinkRestorePreview.cancelled &&
					m_LinkRestorePreview.HasCandidates &&
					m_LinkRestorePreviewKey == m_SelectedKey &&
					m_LinkRestorePreviewType == m_AssetType;

				using (new EditorGUI.DisabledScope(!canContinueRestore))
				{
					if (GUILayout.Button("Continue Restore", GUILayout.Height(24f)))
						ConfirmAndRunLinkRestore();
				}
			}

			if (m_BatchReportLines.Count == 0)
			{
				var message = m_IsLinkRestoreReport
					? "No changed, skipped, or failed asset details. AssetCatalog entry was not deleted."
					: m_IsKeyMigrationReport
						? "No skipped prefab or migration error details."
						: "No skipped prefab or linker conflict details.";
				EditorGUILayout.HelpBox(message, MessageType.Info);
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
