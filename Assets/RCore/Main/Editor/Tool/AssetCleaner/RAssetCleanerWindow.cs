using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace RCore.Editor.AssetCleaner
{
	public class RAssetCleanerWindow : EditorWindow
	{
		[MenuItem("RCore/Asset Cleaner", priority = RMenu.GROUP_2 + 3)]
		private static void ShowWindow()
		{
			var window = GetWindow<RAssetCleanerWindow>();
			window.titleContent = new GUIContent("Asset Cleaner");
			window.Show();
		}

		[MenuItem("Assets/Asset Cleaner/Scan Leaks", false, 2000)]
		private static void ScanLeaksContextMenu()
		{
			var selectionPaths = RAssetCleanerLeak.GetValidSelection(out _, out _);

			var window = GetWindow<RAssetCleanerWindow>();
			window.titleContent = new GUIContent("Asset Cleaner");
			window.m_tabIndex = 2;
			window.m_leakLastSelection = selectionPaths;
			window.RunLeakScan(selectionPaths);
			window.Show();
			window.Repaint();
		}

		[MenuItem("Assets/Asset Cleaner/Scan Leaks", true)]
		private static bool ScanLeaksContextMenuValidate()
		{
			return RAssetCleanerLeak.GetValidSelection(out _, out _).Count > 0;
		}

		[MenuItem("Assets/Asset Cleaner/Select Assets Used by Addressables", false, 2001)]
		private static void SelectAssetsUsedByAddressables()
		{
			Selection.objects = FindAddressableDependencyAssets(GetSelectedFolders());
		}

		[MenuItem("Assets/Asset Cleaner/Select Assets Used by Addressables", true)]
		private static bool SelectAssetsUsedByAddressablesValidate()
		{
			return GetSelectedFolders().Count > 0;
		}

		private static List<string> GetSelectedFolders()
		{
			var folders = new List<string>();
			foreach (var obj in Selection.objects)
			{
				if (obj == null)
					continue;
				string path = AssetDatabase.GetAssetPath(obj);
				if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
					folders.Add(path);
			}
			return folders;
		}

		private static Object[] FindAddressableDependencyAssets(List<string> pFolders)
		{
			if (pFolders == null || pFolders.Count == 0)
				return new Object[0];

			var scope = new HashSet<string>();
			foreach (var folder in pFolders)
			{
				var guids = AssetDatabase.FindAssets("", new[] { folder });
				foreach (var guid in guids)
				{
					string path = AssetDatabase.GUIDToAssetPath(guid);
					if (!string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path))
						scope.Add(path);
				}
			}
			if (scope.Count == 0)
			{
				EditorUtility.DisplayDialog("Select Assets Used by Addressables",
					"No assets found in the selected folder(s).", "OK");
				return new Object[0];
			}

			var addressablePaths = new List<string>();
#if ADDRESSABLES
			var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
			if (settings != null)
			{
				var total = settings.groups.Count;
				for (int i = 0; i < total; i++)
				{
					var group = settings.groups[i];
					if (group == null)
						continue;
					bool cancelled = EditorUtility.DisplayCancelableProgressBar(
						"Select Assets Used by Addressables",
						"Scanning group " + group.Name,
						(float)i / Mathf.Max(1, total));
					if (cancelled)
						break;
					foreach (var entry in group.entries)
					{
						if (entry == null || string.IsNullOrEmpty(entry.AssetPath))
							continue;
						string guid = entry.guid;
						if (RCore.Editor.AddressableEditorHelper.IncludedInBuild(guid))
							addressablePaths.Add(entry.AssetPath);
					}
				}
				EditorUtility.ClearProgressBar();
			}
#endif

			var matches = new HashSet<string>();
#if ADDRESSABLES
			if (addressablePaths.Count > 0)
			{
				for (int i = 0; i < addressablePaths.Count; i++)
				{
					if (EditorUtility.DisplayCancelableProgressBar(
						"Select Assets Used by Addressables",
						"Resolving dependencies of " + Path.GetFileName(addressablePaths[i]),
						(float)i / addressablePaths.Count))
						break;
					foreach (var dep in AssetDatabase.GetDependencies(addressablePaths[i], true))
					{
						if (!string.IsNullOrEmpty(dep) &&
							dep != addressablePaths[i] &&
							scope.Contains(dep))
							matches.Add(dep);
					}
				}
				EditorUtility.ClearProgressBar();
			}
#endif

			if (matches.Count == 0)
			{
				EditorUtility.DisplayDialog("Select Assets Used by Addressables",
					"No assets in the selected folder(s) are used by any Addressable asset.", "OK");
				return new Object[0];
			}

			var result = new List<Object>();
			foreach (var path in matches)
			{
				var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
				if (asset != null)
					result.Add(asset);
			}
			return result.ToArray();
		}

		private int m_tabIndex;
		private string[] m_tabs = { "Cleaner", "Reference Finder", "Leak Checker", "Settings" };

        private class UsageInfo
        {
            public Object hostObject; // The component or asset holding the reference
            public string propertyPath;
            public string propertyDisplayName;
            public SerializedObject serializedObject;
            public SerializedProperty property;
        }

        private Dictionary<string, bool> m_foldoutStates = new Dictionary<string, bool>();
        private Dictionary<string, List<UsageInfo>> m_usageDetails = new Dictionary<string, List<UsageInfo>>();

        private string GetFriendlyDisplayName(SerializedProperty sp)
        {
            // Unity's default displayName for array elements is just "Element N" with no field name.
            // Derive "fieldName[N]" from propertyPath instead, e.g. "m_popups.Array.data[3]" -> "m_popups[3]".
            string path = sp.propertyPath;
            const string arrayMarker = ".Array.data[";
            int markerIndex = path.LastIndexOf(arrayMarker, System.StringComparison.Ordinal);
            if (markerIndex >= 0 && path.EndsWith("]"))
            {
                string fieldName = path.Substring(0, markerIndex);
                int lastDot = fieldName.LastIndexOf('.');
                if (lastDot >= 0)
                    fieldName = fieldName.Substring(lastDot + 1);
                string index = path.Substring(markerIndex + arrayMarker.Length);
                return $"{fieldName}[{index}";
            }
            return sp.displayName;
        }

        private List<UsageInfo> FindUsageDetails(string referrerPath, Object target)
        {
            var list = new List<UsageInfo>();
            
            // Special handling for Scene files
            if (referrerPath.EndsWith(".unity"))
            {
                var targetScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(referrerPath);
                if (targetScene.IsValid() && targetScene.isLoaded)
                {
                    // Scan the specific loaded scene
                    var roots = targetScene.GetRootGameObjects();
                    var allObjects = new List<Object>();
                    foreach(var root in roots)
                    {
                        var transforms = root.GetComponentsInChildren<Transform>(true);
                        foreach (var t in transforms)
                        {
                            allObjects.Add(t.gameObject);
                            allObjects.AddRange(t.GetComponents<Component>());
                        }
                    }
                    
                    foreach (var obj in allObjects)
                    {
                        if (obj == null) continue;
                        
                        var so = new SerializedObject(obj);
                        var sp = so.GetIterator();
                        while (sp.Next(true))
                        {
                             if (sp.propertyType == SerializedPropertyType.ObjectReference && IsMatch(sp, target))
                             {
                                 list.Add(new UsageInfo
                                 {
                                     hostObject = obj,
                                     propertyPath = sp.propertyPath,
                                     propertyDisplayName = GetFriendlyDisplayName(sp),
                                     serializedObject = so,
                                     property = so.FindProperty(sp.propertyPath)
                                 });
                             }
                        }
                    }
                }
                return list;
            }

            var referrerAssets = AssetDatabase.LoadAllAssetsAtPath(referrerPath);

            foreach (var asset in referrerAssets)
            {
                if (asset == null) continue;
                
                var so = new SerializedObject(asset);
                var sp = so.GetIterator();
                
                // Iterate all properties
                while (sp.Next(true))
                {
                    if (sp.propertyType == SerializedPropertyType.ObjectReference && IsMatch(sp, target))
                    {
                         list.Add(new UsageInfo
                         {
                             hostObject = asset,
                             propertyPath = sp.propertyPath,
                             propertyDisplayName = GetFriendlyDisplayName(sp),
                             serializedObject = so,
                             property = so.FindProperty(sp.propertyPath)
                         });
                    }
                }
            }
            return list;
        }

        private bool IsMatch(SerializedProperty sp, Object target)
        {
             // For Prefab connections, ONLY allow strict matching. 
             // Fuzzy matching (same file) causes every single component to match the Prefab Asset, creating massive noise.
             if (sp.name == "m_CorrespondingSourceObject")
             {
                 return sp.objectReferenceValue == target;
             }
        
             bool match = sp.objectReferenceValue == target;
                        
            if (!match && sp.objectReferenceValue != null && target != null)
            {
                // 1. Texture vs Sprite
                if (target is Texture2D tex && sp.objectReferenceValue is Sprite sprite && sprite.texture == tex)
                    match = true;
                
                // 2. Sprite vs Texture
                else if (target is Sprite spriteTarget && sp.objectReferenceValue is Texture2D texVal && spriteTarget.texture == texVal)
                    match = true;
                    
                // 3. Same Asset File
                if (!match)
                {
                    string targetPath = AssetDatabase.GetAssetPath(target);
                    string refPath = AssetDatabase.GetAssetPath(sp.objectReferenceValue);
                    if (targetPath == refPath && !string.IsNullOrEmpty(targetPath))
                    {
                        match = true;
                    }
                }
            }
            return match;
        }

        private bool TryFindFirstComponentAssetReference(GameObject pGameObject, out Object pAsset)
        {
            pAsset = null;
            if (pGameObject == null)
                return false;

            foreach (var component in pGameObject.GetComponents<Component>())
            {
                if (component == null)
                    continue;

                var serializedObject = new SerializedObject(component);
                var property = serializedObject.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.name == "m_Script")
                        continue;

                    var asset = property.objectReferenceValue;
                    if (asset == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset)))
                        continue;

                    pAsset = asset;
                    return true;
                }
            }

            return false;
        }

		// Cleaner State
		private List<string> m_unusedAssets = new List<string>();
		private List<string> m_filteredAssets = new List<string>();
		private Vector2 m_scrollPos;
		private bool m_scanned;
		
		// Pagination
		private int m_currentPage = 0;
		private int m_itemsPerPage = 50;
		private int m_totalPages = 0;
		
		// Cache for current page to avoid expensive calls every frame
		private struct CacheItem
		{
			public Texture icon;
			public string path;
			public string sizeStr;
		}
		private List<CacheItem> m_pageCacheList = new List<CacheItem>();
		private int m_cachedPage = -1;
		
		// Filters
		private enum AssetType
		{
			Scripts,
			ScriptableObject,
			Prefabs,
			Models,
			Materials,
			Textures,
			Audio,
			Video,
			Others
		}

		private Dictionary<AssetType, bool> m_typeFilters = new Dictionary<AssetType, bool>();
		private Dictionary<AssetType, (int count, long size)> m_typeStats = new Dictionary<AssetType, (int count, long size)>();

		// Reference Finder State
		private Object m_selectedAsset;
		private List<string> m_referencingAssets = new List<string>();
		private List<string> m_referenceFilteredAssets = new List<string>();
		private List<List<string>> m_addressableChains = new List<List<string>>();
		private int m_referenceFilterIndex;
		private Vector2 m_refScrollPos;
		private List<Object> m_history = new List<Object>();
		private int m_historyIndex = -1;
		private bool m_referenceFinderLocked;
		private bool m_autoFindAddressableChain;

		// Leak Checker State
		private List<LeakEntry> m_leakedIn = new List<LeakEntry>();
		private List<LeakEntry> m_leakedOut = new List<LeakEntry>();
		private Vector2 m_leakScrollPos;
		private bool m_leakScanned;
		private bool m_leakedInExpanded = true;
		private bool m_leakedOutExpanded = true;
		private Dictionary<string, bool> m_leakFoldouts = new Dictionary<string, bool>();
		private List<string> m_leakLastSelection = new List<string>();
		private int m_leakInGroupIndex;
		private int m_leakOutGroupIndex;

		// Settings/Style
		private GUIStyle m_boxStyle;


		public static bool IsOpen { get; private set; }

		private void OnEnable()
		{
			IsOpen = true;
			RAssetCleaner.CacheInvalidated += OnCacheInvalidated;
			RAssetCleaner.CacheChanged += OnCacheChanged;
			// Load cache
			var cached = RAssetCleaner.LoadCache();
			if (cached != null)
			{
				m_unusedAssets = cached;
				m_scanned = true;
				CalculateTypeStats();
			}
			
			// Load history?
			Selection.selectionChanged += OnSelectionChange;
			
			// Initialize Filters
			foreach (AssetType type in System.Enum.GetValues(typeof(AssetType)))
			{
				if (!m_typeFilters.ContainsKey(type))
					m_typeFilters[type] = true;
			}
			EditorApplication.RepaintProjectWindow();
			m_autoFindAddressableChain = EditorPrefs.GetBool("RAssetCleaner_AutoFindAddrChain", false);
		}

		private void OnDisable()
		{
			IsOpen = false;
			RAssetCleaner.CacheInvalidated -= OnCacheInvalidated;
			RAssetCleaner.CacheChanged -= OnCacheChanged;
			EditorApplication.delayCall -= RefreshAfterCacheChange;
			Selection.selectionChanged -= OnSelectionChange;
			EditorApplication.RepaintProjectWindow();
		}

		/// <summary>Queues incremental-cache view refresh outside Unity's asset-import callback.</summary>
		private void OnCacheChanged()
		{
			EditorApplication.delayCall -= RefreshAfterCacheChange;
			EditorApplication.delayCall += RefreshAfterCacheChange;
		}

		private void RefreshAfterCacheChange()
		{
			EditorApplication.delayCall -= RefreshAfterCacheChange;
			if (this == null)
				return;

			if (RAssetCleaner.HasUnusedData)
			{
				m_unusedAssets = new List<string>(RAssetCleaner.UnusedAssetsCache);
				CalculateTypeStats();
				m_filteredAssets.Clear();
				m_pageCacheList.Clear();
				m_cachedPage = -1;
				m_currentPage = 0;
			}

			if (!m_referenceFinderLocked && m_selectedAsset != null)
				FindReferences(true);

			// Leak reports derive from dependency edges and cannot survive an incremental graph update.
			m_leakedIn.Clear();
			m_leakedOut.Clear();
			m_leakFoldouts.Clear();
			m_leakScanned = false;
			m_leakInGroupIndex = 0;
			m_leakOutGroupIndex = 0;

			Repaint();
		}

		/// <summary>Cached scan results are gone, so every view derived from them must reset.</summary>
		private void OnCacheInvalidated()
		{
			m_unusedAssets.Clear();
			m_filteredAssets.Clear();
			m_pageCacheList.Clear();
			m_typeStats.Clear();
			m_selectedAsset = null;
			m_referencingAssets.Clear();
			m_addressableChains.Clear();
			m_referenceFinderLocked = false;
			m_referenceFilterIndex = 0;
			m_referenceFilteredAssets = m_referencingAssets;
			m_usageDetails.Clear();
			m_foldoutStates.Clear();
			m_leakedIn.Clear();
			m_leakedOut.Clear();
			m_leakFoldouts.Clear();
			m_leakLastSelection.Clear();
			m_leakScanned = false;
			m_scanned = false;
			m_currentPage = 0;
			m_cachedPage = -1;
			Repaint();
		}

		private void OnSelectionChange()
		{
			// Only update if we are in the Reference Finder tab (index 1)
			// Or if the user expects it to auto-update. Since the original request implies "when I select... it will be auto filled",
			// it usually implies while looking at the tool.
			// If the tool is docked but hidden, we probably shouldn't do expensive FindReferences.
			// But checking if window is focused is hard. 
			// Checking tab index is reasonable.

			if (m_tabIndex == 1 && !m_referenceFinderLocked && Selection.activeObject != null)
			{
				// Check if it's an asset (not scene object) - though checking scene objects is fine too if we support it.
				// AssetDatabase.Contains checks if it's an asset.
				if (Selection.activeObject is GameObject gameObject &&
                        !AssetDatabase.Contains(gameObject) &&
                        RAssetCleanerSettings.Instance.scanFirstComponentAssetReference &&
                        TryFindFirstComponentAssetReference(gameObject, out var asset))
                    {
                        m_selectedAsset = asset;
                        AddToHistory(m_selectedAsset);
                        FindReferences(true);
                        Repaint();
                        return;
                    }

                    if (AssetDatabase.Contains(Selection.activeObject))
				{
					// Avoid refreshing if selecting the same object (unless we want to force refresh)
					if (m_selectedAsset != Selection.activeObject)
					{
						m_selectedAsset = Selection.activeObject;
						AddToHistory(m_selectedAsset);
						FindReferences(true); // Auto mode
						Repaint();
					}
				}
			}
			else if (m_tabIndex == 2)
			{
				Repaint();
			}
		}

		private void OnGUI()
		{
			m_tabIndex = GUILayout.Toolbar(m_tabIndex, m_tabs);

			GUILayout.Space(10);

			switch (m_tabIndex)
			{
				case 0: DrawCleanerTab(); break;
				case 1: DrawReferenceFinderTab(); break;
				case 2: DrawLeakCheckerTab(); break;
				case 3: DrawSettingsTab(); break;
			}
		}

		private void DrawCleanerTab()
		{
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Scan Project", GUILayout.Height(30)))
			{
				m_unusedAssets = RAssetCleaner.FindUnusedAssets(RAssetCleanerSettings.Instance.ignorePaths);
				RAssetCleaner.SaveCache(m_unusedAssets);
				CalculateTypeStats();
				m_currentPage = 0; // Reset to first page
				m_scanned = true;
			}
			if (GUILayout.Button("Reload", GUILayout.Width(70), GUILayout.Height(30)))
			{
				var cached = RAssetCleaner.LoadCache();
				if (cached != null)
				{
					m_unusedAssets = cached;
					m_scanned = true;
					CalculateTypeStats();
					m_currentPage = 0;
				}
			}
			EditorGUILayout.EndHorizontal();

			if (m_scanned)
			{
				GUILayout.Space(10);
				GUILayout.Label($"Found {m_unusedAssets.Count} unused assets. Total Size: {RAssetCleaner.GetTotalSizeFormatted(m_unusedAssets)}");

				DrawTypeFilters();
				
				// Calculate pagination
				m_totalPages = Mathf.CeilToInt((float)m_filteredAssets.Count / m_itemsPerPage);
				if (m_totalPages == 0) m_totalPages = 1;
				
				// Clamp current page
				m_currentPage = Mathf.Clamp(m_currentPage, 0, m_totalPages - 1);
				
				var (startIndex, endIndex) = GetPageRange();

				// Build cache for current page if needed
				if (m_cachedPage != m_currentPage)
				{
					BuildPageCache(startIndex, endIndex);
					m_cachedPage = m_currentPage;
				}
				
				// Header
				EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
				GUILayout.Label("", GUILayout.Width(20)); // Icon space
				GUILayout.Label("Path", GUILayout.ExpandWidth(true));
				GUILayout.Label("Size", GUILayout.Width(60));
				GUILayout.Label("Action", GUILayout.Width(60));
				EditorGUILayout.EndHorizontal();

				m_scrollPos = GUILayout.BeginScrollView(m_scrollPos);

				float rowHeight = 24f;
				int count = endIndex - startIndex;
				
				Rect listRect = GUILayoutUtility.GetRect(0, count * rowHeight, GUILayout.ExpandWidth(true));
				
				if (Event.current.type == EventType.Repaint)
				{
					// Draw background for list area if needed
				}

				for (int i = 0; i < count; i++)
				{
					if (i >= m_pageCacheList.Count) break;
					
					var item = m_pageCacheList[i];
					float y = listRect.y + i * rowHeight;
					Rect rowRect = new Rect(listRect.x, y, listRect.width, rowHeight);
					
					// Zebra Striping
					if (i % 2 == 0) 
						EditorGUI.DrawRect(rowRect, new Color(0, 0, 0, 0.1f));
					
					// Columns
					Rect iconRect = new Rect(rowRect.x, y + 2, 20, 20);
					Rect pathRect = new Rect(rowRect.x + 25, y + 2, rowRect.width - 25 - 130, 20); 
					Rect sizeRect = new Rect(rowRect.width - 125, y + 2, 60, 20);
					Rect btnRect = new Rect(rowRect.width - 60, y + 2, 55, 20);

					if (item.icon != null) GUI.Label(iconRect, item.icon);
					GUI.Label(pathRect, item.path);
					GUI.Label(sizeRect, item.sizeStr);

					if (GUI.Button(btnRect, "Del")) // Shortened for space
					{
						if (EditorUtility.DisplayDialog("Delete Asset", $"Are you sure you want to delete {item.path}?", "Delete", "Cancel"))
						{
							// Deleting from this list keeps the cache correct, so skip the import-driven invalidation.
							bool deleted;
							RAssetCleaner.BeginInternalAssetEdit();
							try
							{
								deleted = AssetDatabase.DeleteAsset(item.path);
							}
							finally
							{
								RAssetCleaner.EndInternalAssetEdit();
							}

							if (!deleted)
							{
								Debug.LogWarning($"Asset Cleaner could not delete {item.path}.");
							}
							else
							{
								RAssetCleaner.ForgetAsset(item.path);
								m_unusedAssets = new List<string>(RAssetCleaner.UnusedAssetsCache);
								RAssetCleaner.SaveCache(m_unusedAssets);
								CalculateTypeStats();
								BuildPageCache(startIndex, endIndex);
							}
						}
					}
					
					// Click to Ping (Selection)
					if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
					{
						var obj = AssetDatabase.LoadAssetAtPath<Object>(item.path);
						Selection.activeObject = obj;
						EditorGUIUtility.PingObject(obj);
						Event.current.Use();
					}
				}
				
				GUILayout.EndScrollView();
				
				DrawPaginationControls();
			}
		}



		private void DrawReferenceFinderTab()
		{
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Scan Project", GUILayout.Height(30)))
			{
				m_unusedAssets = RAssetCleaner.FindUnusedAssets(RAssetCleanerSettings.Instance.ignorePaths);
				RAssetCleaner.SaveCache(m_unusedAssets);
				CalculateTypeStats();
				m_currentPage = 0; // Reset to first page
				m_scanned = true;
			}
			if (GUILayout.Button("Reload", GUILayout.Width(70), GUILayout.Height(30)))
			{
				var cached = RAssetCleaner.LoadCache();
				if (cached != null)
				{
					m_unusedAssets = cached;
					m_scanned = true;
					CalculateTypeStats();
					m_currentPage = 0;
				}
			}
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Find References", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();
			bool wasLocked = m_referenceFinderLocked;
			m_referenceFinderLocked = GUILayout.Toggle(m_referenceFinderLocked, m_referenceFinderLocked ? "Results Locked" : "Lock Results", GUILayout.Width(100));
			EditorGUILayout.EndHorizontal();

			if (wasLocked && !m_referenceFinderLocked)
				OnSelectionChange();

			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginDisabledGroup(m_referenceFinderLocked);
			EditorGUI.BeginChangeCheck();
			m_selectedAsset = EditorGUILayout.ObjectField("Asset", m_selectedAsset, typeof(Object), false);
			if (EditorGUI.EndChangeCheck())
			{
				if (m_selectedAsset != null)
				{
					AddToHistory(m_selectedAsset);
					FindReferences();
				}
			}

			if (GUILayout.Button("Use Selection", GUILayout.Width(100)))
			{
				if (Selection.activeObject != null)
				{
					m_selectedAsset = Selection.activeObject;
					AddToHistory(m_selectedAsset);
					FindReferences();
				}
			}
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();



			GUILayout.Space(10);

			if (m_selectedAsset != null)
			{
				GUILayout.Space(5);
				string selectedPath = AssetDatabase.GetAssetPath(m_selectedAsset);
				string selectedGroupLabel = GetAddressableGroupLabel(selectedPath);
				GUILayout.Label($"Addressable Status: {selectedGroupLabel}");
				GUILayout.Label($"Used by {m_referencingAssets.Count} assets:");
				GUILayout.Space(5);

				EditorGUILayout.BeginHorizontal();
				var newAutoChain = EditorGUILayout.ToggleLeft("Auto-find Addressable Chain", m_autoFindAddressableChain);
				if (newAutoChain != m_autoFindAddressableChain)
				{
					m_autoFindAddressableChain = newAutoChain;
					EditorPrefs.SetBool("RAssetCleaner_AutoFindAddrChain", newAutoChain);
					if (newAutoChain && m_selectedAsset != null && m_addressableChains.Count == 0)
					{
						string p = AssetDatabase.GetAssetPath(m_selectedAsset);
						var chain = RAssetCleaner.FindAddressableReferenceChain(p);
						if (chain.Count > 0)
							m_addressableChains.Add(chain);
					}
				}
				if (!m_autoFindAddressableChain && m_addressableChains.Count == 0 && m_selectedAsset != null)
				{
					if (GUILayout.Button("Find Addressable Chain", GUILayout.Width(170)))
					{
						string p = AssetDatabase.GetAssetPath(m_selectedAsset);
						var chain = RAssetCleaner.FindAddressableReferenceChain(p);
						if (chain.Count > 0)
							m_addressableChains.Add(chain);
					}
				}
				EditorGUILayout.EndHorizontal();

				var visibleChains = m_addressableChains.Where(c => c.Count > 1).ToList();
				if (visibleChains.Count > 0)
				{
					GUILayout.Label("Also used by these ADDRESSABLE assets (transitively):", EditorStyles.boldLabel);
					var chainLabelStyle = new GUIStyle(EditorStyles.label) { richText = true };
					string addrColor = EditorGUIUtility.isProSkin ? "#7FB4FF" : "#1F4FD8";
					string viaColor = EditorGUIUtility.isProSkin ? "#C8C8C8" : "#606060";
					foreach (var chain in visibleChains)
					{
						string addressableAsset = chain[chain.Count - 1];
						string text = $"<color={addrColor}>{addressableAsset}</color>   via   <color={viaColor}>{string.Join(" → ", chain.GetRange(0, chain.Count - 1))}</color>";
						EditorGUILayout.BeginHorizontal();
						GUILayout.Label(text, chainLabelStyle);
						if (GUILayout.Button("Select", GUILayout.Width(55)))
						{
							var addrObj = AssetDatabase.LoadAssetAtPath<Object>(addressableAsset);
							if (addrObj != null) Selection.activeObject = addrObj;
						}
						EditorGUILayout.EndHorizontal();
					}
					GUILayout.Space(5);
				}

				GUILayout.Label("Reference Assets", EditorStyles.boldLabel);
				DrawReferenceFilterToolbar();
				GUILayout.Space(5);

				int filteredCount = m_referenceFilteredAssets.Count;
				string filterName = m_referenceFilterIndex == 0 ? "all" : m_referenceFilterIndex == 1 ? "non-addressable" : "addressable";
				GUILayout.Label($"{filteredCount} referencing {filterName} assets:");
				GUILayout.Space(5);

				// Header
				EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
				GUILayout.Label("", GUILayout.Width(25)); // Foldout
				GUILayout.Label("", GUILayout.Width(20)); // Icon
				GUILayout.Label("Referencing Asset", GUILayout.ExpandWidth(true));
				GUILayout.Label("Type", GUILayout.Width(90));
				GUILayout.Label("Addressable", GUILayout.Width(130));
				GUILayout.Label("Action", GUILayout.Width(60));
				EditorGUILayout.EndHorizontal();

				m_refScrollPos = GUILayout.BeginScrollView(m_refScrollPos);
				
				for (int i = 0; i < m_referenceFilteredAssets.Count; i++)
				{
					var refPath = m_referenceFilteredAssets[i];
					var asset = AssetDatabase.LoadAssetAtPath<Object>(refPath);
					
					if (asset == null) continue;

					// Main Row
					Rect rowRect = EditorGUILayout.GetControlRect(false, 20);
					
					// Zebra Striping
					if (i % 2 == 0) EditorGUI.DrawRect(rowRect, new Color(0, 0, 0, 0.1f));

					// Foldout
					if (!m_foldoutStates.ContainsKey(refPath)) m_foldoutStates[refPath] = false;
					bool isExpanded = m_foldoutStates[refPath];
					
					Rect foldoutRect = new Rect(rowRect.x, rowRect.y, 25, rowRect.height);
					Rect iconRect = new Rect(rowRect.x + 25, rowRect.y, 20, 20);
					Rect pathRect = new Rect(rowRect.x + 50, rowRect.y, rowRect.width - 50 - 65 - 90 - 130, rowRect.height);
					Rect typeRect = new Rect(rowRect.width - 60 - 130 - 90, rowRect.y, 85, rowRect.height);
					Rect addressableRect = new Rect(rowRect.width - 60 - 130, rowRect.y, 125, rowRect.height);
					Rect btnRect = new Rect(rowRect.width - 60, rowRect.y, 55, 18);

					if (GUI.Button(foldoutRect, isExpanded ? "▼" : "▶", EditorStyles.label))
					{
						m_foldoutStates[refPath] = !isExpanded;
						if (!isExpanded && !m_usageDetails.ContainsKey(refPath))
						{
							m_usageDetails[refPath] = FindUsageDetails(refPath, m_selectedAsset);
						}
					}

					var icon = AssetDatabase.GetCachedIcon(refPath);
					if (icon != null) GUI.Label(iconRect, icon);
					GUI.Label(pathRect, refPath);
					GUI.Label(typeRect, asset.GetType().Name);
					GUI.Label(addressableRect, GetAddressableGroupLabel(refPath));

					if (GUI.Button(btnRect, "Select"))
					{
						Selection.activeObject = asset;
						EditorGUIUtility.PingObject(asset);
					}
					
					// Draw Details
					if (isExpanded && m_usageDetails.ContainsKey(refPath))
					{
						var details = m_usageDetails[refPath];
						if (details.Count == 0)
						{
							if (refPath.EndsWith(".unity"))
							{
								var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(refPath);
								if (!scene.IsValid())
								{
									EditorGUILayout.HelpBox("Scene is not loaded. References cannot be inspected.", MessageType.Warning);
									if (GUILayout.Button("Load Scene to Inspect (Additive)", GUILayout.Height(30)))
									{
										UnityEditor.SceneManagement.EditorSceneManager.OpenScene(refPath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
										m_usageDetails[refPath] = FindUsageDetails(refPath, m_selectedAsset);
									}
								}
								else
								{
									EditorGUILayout.HelpBox("Could not find direct property reference.", MessageType.Info);
								}
							}
							else
							{
								EditorGUILayout.HelpBox("Could not find direct property reference.", MessageType.Info);
							}
						}
						else
						{
							foreach (var detail in details)
							{
								EditorGUILayout.BeginHorizontal();
								GUILayout.Space(50); // Indent
								GUILayout.Label($"{detail.hostObject.GetType().Name}.{detail.propertyDisplayName}", GUILayout.Width(200));
								
								// Ensure serialized object is valid
								if (detail.serializedObject != null && detail.serializedObject.targetObject != null)
								{
									detail.serializedObject.Update();
									EditorGUILayout.PropertyField(detail.property, GUIContent.none);
									detail.serializedObject.ApplyModifiedProperties();
								}
								EditorGUILayout.EndHorizontal();
							}
						}
					}
				}
				GUILayout.EndScrollView();
			}
		}

		private void DrawReferenceFilterToolbar()
		{
			var filterLabels = new[] { "All Assets", "Non-Addressable Assets", "Addressable Assets" };
			var nextFilterIndex = GUILayout.Toolbar(m_referenceFilterIndex, filterLabels);
			if (nextFilterIndex == m_referenceFilterIndex)
				return;

			m_referenceFilterIndex = nextFilterIndex;
			UpdateReferenceFilter();
			m_refScrollPos = Vector2.zero;
		}

		private void DrawLeakCheckerTab()
		{
			var selectionPaths = RAssetCleanerLeak.GetValidSelection(out int folderCount, out int prefabCount);

			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginDisabledGroup(selectionPaths.Count == 0);
			if (GUILayout.Button("Scan Leaks", GUILayout.Height(30)))
			{
				m_leakLastSelection = selectionPaths;
				RunLeakScan(m_leakLastSelection);
			}
			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginDisabledGroup(m_leakLastSelection.Count == 0);
			if (GUILayout.Button("Rescan", GUILayout.Width(70), GUILayout.Height(30)))
			{
				RunLeakScan(m_leakLastSelection);
			}
			EditorGUI.EndDisabledGroup();
			if (GUILayout.Button("Rebuild Cache", GUILayout.Width(110), GUILayout.Height(30)))
			{
				m_unusedAssets = RAssetCleaner.FindUnusedAssets(RAssetCleanerSettings.Instance.ignorePaths);
				RAssetCleaner.SaveCache(m_unusedAssets);
				CalculateTypeStats();
				m_currentPage = 0;
				m_scanned = true;
				m_leakedIn.Clear();
				m_leakedOut.Clear();
				m_leakFoldouts.Clear();
				m_leakScanned = false;
				EditorApplication.RepaintProjectWindow();
			}
			EditorGUILayout.EndHorizontal();

			if (selectionPaths.Count == 0)
			{
				EditorGUILayout.HelpBox("Select one or more folders or prefabs in the Project window, then press Scan Leaks.", MessageType.Info);
			}
			else
			{
				GUILayout.Label($"Selection: {folderCount} folder(s), {prefabCount} prefab(s)");
			}

			if (!m_leakScanned)
				return;

			GUILayout.Space(10);
			m_leakScrollPos = GUILayout.BeginScrollView(m_leakScrollPos);

			var leakedInPaths = m_leakedIn.Select(l => l.assetPath).ToList();
			m_leakedInExpanded = EditorGUILayout.Foldout(m_leakedInExpanded,
				$"Leaked In ({m_leakedIn.Count}) - {RAssetCleaner.GetTotalSizeFormatted(leakedInPaths)} - referenced from outside selection", true);
			if (m_leakedInExpanded)
				DrawLeakSection(m_leakedIn, "Referenced by", ref m_leakInGroupIndex);

			GUILayout.Space(10);
			var leakedOutPaths = m_leakedOut.Select(l => l.assetPath).ToList();
			m_leakedOutExpanded = EditorGUILayout.Foldout(m_leakedOutExpanded,
				$"Leaked Out ({m_leakedOut.Count}) - {RAssetCleaner.GetTotalSizeFormatted(leakedOutPaths)} - external assets pulled into selection", true);
			if (m_leakedOutExpanded)
				DrawLeakSection(m_leakedOut, "Pulled in by", ref m_leakOutGroupIndex);

			GUILayout.EndScrollView();
		}

		private void RunLeakScan(List<string> pSelectionPaths)
		{
			if (!RAssetCleaner.HasReferenceData)
				RAssetCleaner.BuildCache();

			var boundary = RAssetCleanerLeak.BuildBoundary(pSelectionPaths);
			var leaks = RAssetCleanerLeak.DetectLeaks(boundary);
			m_leakedIn = leaks.Where(l => l.direction == LeakDirection.LeakedIn).ToList();
			m_leakedOut = leaks.Where(l => l.direction == LeakDirection.LeakedOut).ToList();
			m_leakFoldouts.Clear();
			m_leakInGroupIndex = 0;
			m_leakOutGroupIndex = 0;
			m_leakScanned = true;
		}

		private void DrawLeakSection(List<LeakEntry> pEntries, string pRelatedLabel, ref int pGroupIndex)
		{
			if (pEntries.Count == 0)
			{
				EditorGUILayout.HelpBox("No leaks found.", MessageType.Info);
				return;
			}

			var groups = pEntries.GroupBy(e => GetAssetType(e.assetPath)).OrderBy(g => g.Key.ToString()).ToList();
			string[] tabLabels = groups.Select(g => $"{g.Key} ({g.Count()})").ToArray();
			pGroupIndex = Mathf.Clamp(pGroupIndex, 0, groups.Count - 1);
			pGroupIndex = GUILayout.Toolbar(pGroupIndex, tabLabels);
			GUILayout.Space(5);
			DrawLeakEntries(groups[pGroupIndex].ToList(), pRelatedLabel);
		}

		private void DrawLeakEntries(List<LeakEntry> pEntries, string pRelatedLabel)
		{
			if (pEntries.Count == 0)
			{
				EditorGUILayout.HelpBox("No leaks found.", MessageType.Info);
				return;
			}

			for (int i = 0; i < pEntries.Count; i++)
			{
				var entry = pEntries[i];
				Rect rowRect = EditorGUILayout.GetControlRect(false, 20);

				if (i % 2 == 0)
					EditorGUI.DrawRect(rowRect, new Color(0, 0, 0, 0.1f));

				string foldKey = entry.direction + entry.assetPath;
				if (!m_leakFoldouts.ContainsKey(foldKey))
					m_leakFoldouts[foldKey] = false;
				bool isExpanded = m_leakFoldouts[foldKey];

				Rect foldoutRect = new Rect(rowRect.x, rowRect.y, 25, rowRect.height);
				Rect iconRect = new Rect(rowRect.x + 25, rowRect.y, 20, 20);
				Rect pathRect = new Rect(rowRect.x + 50, rowRect.y, rowRect.width - 50 - 130, rowRect.height);
				Rect sizeRect = new Rect(rowRect.width - 125, rowRect.y, 60, rowRect.height);
				Rect btnRect = new Rect(rowRect.width - 60, rowRect.y, 55, 18);

				if (GUI.Button(foldoutRect, isExpanded ? "▼" : "▶", EditorStyles.label))
					m_leakFoldouts[foldKey] = !isExpanded;

				var icon = AssetDatabase.GetCachedIcon(entry.assetPath);
				if (icon != null) GUI.Label(iconRect, icon);
				GUI.Label(pathRect, entry.assetPath);
				GUI.Label(sizeRect, EditorUtility.FormatBytes(RAssetCleaner.GetAssetSize(entry.assetPath)));

				if (GUI.Button(btnRect, "Select"))
				{
					var obj = AssetDatabase.LoadAssetAtPath<Object>(entry.assetPath);
					Selection.activeObject = obj;
					EditorGUIUtility.PingObject(obj);
				}

				if (isExpanded)
				{
					foreach (var related in entry.relatedPaths)
					{
						EditorGUILayout.BeginHorizontal();
						GUILayout.Space(50);
						GUILayout.Label($"{pRelatedLabel}: {related}");
						if (GUILayout.Button("Select", GUILayout.Width(55)))
						{
							var obj = AssetDatabase.LoadAssetAtPath<Object>(related);
							Selection.activeObject = obj;
							EditorGUIUtility.PingObject(obj);
						}
						EditorGUILayout.EndHorizontal();
					}
				}
			}
		}

		private void AddToHistory(Object obj)
		{
			if (m_historyIndex >= 0 && m_historyIndex < m_history.Count && m_history[m_historyIndex] == obj)
				return;

			// Clear forward history
			if (m_historyIndex < m_history.Count - 1)
			{
				m_history.RemoveRange(m_historyIndex + 1, m_history.Count - (m_historyIndex + 1));
			}

			m_history.Add(obj);
			m_historyIndex = m_history.Count - 1;
		}

		private void FindReferences(bool auto = false)
		{
			// Clear cache to avoid stale sub-details from previous selection
			m_usageDetails.Clear();
			m_foldoutStates.Clear();

			if (m_selectedAsset == null) return;
			string path = AssetDatabase.GetAssetPath(m_selectedAsset);

			var references = new HashSet<string>();

			// Standard Dependency Search
			if (auto)
			{
				if (RAssetCleaner.HasReferenceData)
				{
					references.UnionWith(RAssetCleaner.FindReferences(path, true));
				}
				else
				{
					// Auto-mode but no cache? Do nothing or small local search?
					// Usually auto means fast.
				}
			}
			else
			{
				if (!RAssetCleaner.HasReferenceData && !EditorUtility.DisplayDialog("Cache Missing", "The Reference Graph is not built. Searching without cache is slower. Continue?", "Run Slow Search", "Cancel"))
				{
					// user cancelled
				}
				else
				{
					references.UnionWith(RAssetCleaner.FindReferences(path, false));
				}
			}

			// Deep Search (Guid text scan)
			if (RAssetCleanerSettings.Instance.deepSearch)
			{
				string guid = AssetDatabase.AssetPathToGUID(path);
				if (!string.IsNullOrEmpty(guid))
				{
					var deepRefs = RAssetCleaner.FindReferencesByGuid(guid);
					references.UnionWith(deepRefs);
				}
			}

			m_referencingAssets = references.OrderBy(x => x).ToList();
			UpdateReferenceFilter();

			m_addressableChains.Clear();
			if (m_autoFindAddressableChain)
			{
				var chain = RAssetCleaner.FindAddressableReferenceChain(path);
				if (chain.Count > 0)
					m_addressableChains.Add(chain);
			}
		}

		private void UpdateReferenceFilter()
		{
			switch (m_referenceFilterIndex)
			{
				case 1:
					m_referenceFilteredAssets = m_referencingAssets.Where(x => !IsAddressableAsset(x)).ToList();
					break;
				case 2:
					m_referenceFilteredAssets = m_referencingAssets.Where(IsAddressableAsset).ToList();
					break;
				default:
					m_referenceFilteredAssets = m_referencingAssets;
					break;
			}
		}

		private bool IsAddressableAsset(string path)
		{
#if ADDRESSABLES
			string guid = AssetDatabase.AssetPathToGUID(path);
			if (string.IsNullOrEmpty(guid))
				return false;
			return RCore.Editor.AddressableEditorHelper.IncludedInBuild(guid);
#else
			return false;
#endif
		}

		private string GetAddressableGroupLabel(string path)
		{
#if ADDRESSABLES
			string guid = AssetDatabase.AssetPathToGUID(path);
			if (string.IsNullOrEmpty(guid))
				return "Not Addressable";

			var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
			var entry = settings?.FindAssetEntry(guid, true);
			if (!RCore.Editor.AddressableEditorHelper.IncludedInBuild(guid) || entry?.parentGroup == null)
				return "Not Addressable";
			return entry.parentGroup.Name;
#else
			return "Not Addressable";
#endif
		}

		private void DrawSettingsTab()
		{
			GUILayout.Label("Settings", EditorStyles.boldLabel);
			var settings = RAssetCleanerSettings.Instance;

			EditorGUI.BeginChangeCheck();

			settings.showRedOverlay = EditorGUILayout.Toggle("Red Overlay (Unused)", settings.showRedOverlay);
			settings.unusedColor = EditorGUILayout.ColorField("Overlay Color", settings.unusedColor);
			settings.showSize = EditorGUILayout.Toggle("Show Assets Size", settings.showSize);
			settings.showReferenceCount = EditorGUILayout.Toggle("Show Reference Count", settings.showReferenceCount);

			GUILayout.Space(10);
			GUILayout.Label("Advanced Search", EditorStyles.boldLabel);
			settings.deepSearch = EditorGUILayout.Toggle(new GUIContent("Deep Search (Slow)", "Scan text contents of assets to find hidden or addressable references."), settings.deepSearch);
			EditorGUILayout.HelpBox("Enable this to find indirect references (e.g. Addressables, AssetBundleWraps) by scanning file contents. This process is slower than standard dependency tracking.", MessageType.Info);
			
            settings.scanFirstComponentAssetReference = EditorGUILayout.Toggle(
                new GUIContent(
                    "Reference Finder: Scan First Component Asset",
                    "When Reference Finder is active, selecting a GameObject searches references for its first serialized component asset reference."),
                settings.scanFirstComponentAssetReference);

			if (settings.deepSearch)
			{
				GUILayout.Label("File Extensions to Scan:", EditorStyles.label);
				for (int i = 0; i < settings.deepSearchExtensions.Count; i++)
				{
					EditorGUILayout.BeginHorizontal();
					settings.deepSearchExtensions[i] = EditorGUILayout.TextField(settings.deepSearchExtensions[i]);
					if (GUILayout.Button("-", GUILayout.Width(25)))
					{
						settings.deepSearchExtensions.RemoveAt(i);
						i--;
					}
					EditorGUILayout.EndHorizontal();
				}
				if (GUILayout.Button("Add Extension", GUILayout.Width(120)))
				{
					settings.deepSearchExtensions.Add(".json");
				}
				GUILayout.Space(10);
			}

			GUILayout.Space(10);
			GUILayout.Label("Leak Checker - Ignored Extensions", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Dependencies with these extensions are excluded from leak reports (scripts compile into the build and are not asset leaks).", MessageType.Info);

			for (int i = 0; i < settings.leakIgnoreExtensions.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				settings.leakIgnoreExtensions[i] = EditorGUILayout.TextField(settings.leakIgnoreExtensions[i]);
				if (GUILayout.Button("-", GUILayout.Width(25)))
				{
					settings.leakIgnoreExtensions.RemoveAt(i);
					i--;
				}
				EditorGUILayout.EndHorizontal();
			}
			if (GUILayout.Button("Add Extension", GUILayout.Width(120)))
			{
				settings.leakIgnoreExtensions.Add(".cs");
			}

			GUILayout.Space(10);
			GUILayout.Label("Ignore Paths (Contains)", EditorStyles.boldLabel);

			for (int i = 0; i < settings.ignorePaths.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				settings.ignorePaths[i] = EditorGUILayout.TextField(settings.ignorePaths[i]);
				if (GUILayout.Button("-", GUILayout.Width(25)))
				{
					settings.ignorePaths.RemoveAt(i);
					i--;
				}
				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button("Add Path"))
			{
				settings.ignorePaths.Add("Assets/");
			}

			if (EditorGUI.EndChangeCheck())
			{
				RAssetCleanerSettings.Save();
				EditorApplication.RepaintProjectWindow();
			}
		}
		private void DrawTypeFilters()
		{
            EditorGUILayout.LabelField("Filter by Type:", EditorStyles.boldLabel);
            
            float width = 0;
            float viewWidth = EditorGUIUtility.currentViewWidth - 20; // Margin for scrollbar/padding
            
			EditorGUILayout.BeginHorizontal();
			
			var types = System.Enum.GetValues(typeof(AssetType));
            foreach (AssetType type in types)
            {
				int count = 0;
				long size = 0;
				if (m_typeStats.ContainsKey(type))
				{
					count = m_typeStats[type].count;
					size = m_typeStats[type].size;
				}

				if (count == 0) continue;
				
                var label = new GUIContent($"{type} ({count}) - {EditorUtility.FormatBytes(size)}");
                var style = EditorStyles.miniButton;
                var btnWidth = style.CalcSize(label).x;
                
                if (width + btnWidth > viewWidth)
                {
                    width = 0;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
                
                // Toggle Button Logic: Highlight if active
                var prevColor = GUI.backgroundColor;
                if (m_typeFilters[type])
                    GUI.backgroundColor = new Color(0.7f, 1f, 0.7f); // Light Green for active

                if (m_typeFilters[type] != GUILayout.Toggle(m_typeFilters[type], label, style))
				{
					m_typeFilters[type] = !m_typeFilters[type];
					ApplyFilter();
				}
                
                GUI.backgroundColor = prevColor;
                
                width += btnWidth + 4; // Spacing
            }
			EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
		}

		private bool IsTypeVisible(string path)
		{
			var type = GetAssetType(path);
			return m_typeFilters.ContainsKey(type) && m_typeFilters[type];
		}

		private AssetType GetAssetType(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            switch (ext)
            {
                case ".cs":
                case ".dll":
                case ".asmdef":
                case ".js":
                    return AssetType.Scripts;
                case ".asset":
                    return AssetType.ScriptableObject;
                case ".prefab":
                    return AssetType.Prefabs;
                case ".mat":
                case ".shader":
                case ".shadergraph":
                    return AssetType.Materials;
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".psd":
                case ".tif":
                case ".tiff":
                case ".bmp":
                    return AssetType.Textures;
                case ".fbx":
                case ".obj":
                case ".blend":
                case ".dae":
                case ".3ds":
                case ".dxf":
                    return AssetType.Models;
                case ".mp3":
                case ".wav":
                case ".ogg":
                case ".aiff":
                    return AssetType.Audio;
                case ".mp4":
                case ".mov":
                case ".webm":
                case ".avi":
                    return AssetType.Video;
                default:
                    return AssetType.Others;
            }
        }

	private void CalculateTypeStats()
	{
		m_typeStats.Clear();
		int total = m_unusedAssets.Count;
		
		// Show progress bar for large datasets
		bool showProgress = total > 1000;
		int index = 0;
		
		foreach (var path in m_unusedAssets)
		{
			if (showProgress && index % 500 == 0)
			{
				if (EditorUtility.DisplayCancelableProgressBar("Calculating Type Statistics", 
					$"Processing {index}/{total} assets...", (float)index / total))
				{
					// User cancelled
					EditorUtility.ClearProgressBar();
					m_typeStats.Clear();
					return;
				}
			}
			
			var type = GetAssetType(path);
			if (!m_typeStats.ContainsKey(type))
				m_typeStats[type] = (0, 0);
			
			var (count, size) = m_typeStats[type];
			// GetAssetSize now uses cache, so this is fast
			m_typeStats[type] = (count + 1, size + RAssetCleaner.GetAssetSize(path));
			index++;
		}
		
		if (showProgress)
			EditorUtility.ClearProgressBar();
			
		ApplyFilter();
	}

		private void ApplyFilter()
		{
			m_filteredAssets.Clear();
			foreach (var path in m_unusedAssets)
			{
				if (IsTypeVisible(path))
				{
					m_filteredAssets.Add(path);
				}
			}
			m_currentPage = 0; // Reset to first page when filter changes
			m_cachedPage = -1; // Invalidate cache
		}
		
		private (int startIndex, int endIndex) GetPageRange()
		{
			if (m_filteredAssets.Count == 0)
				return (0, 0);
				
			int startIndex = m_currentPage * m_itemsPerPage;
			int endIndex = Mathf.Min(startIndex + m_itemsPerPage, m_filteredAssets.Count);
			return (startIndex, endIndex);
		}
		
		private void DrawPaginationControls()
		{
			GUILayout.Space(10);
			
			EditorGUILayout.BeginHorizontal();
			
			// Page info
			var (startIndex, endIndex) = GetPageRange();
			int displayPage = m_currentPage + 1; // 1-based for display
			GUILayout.Label($"Page {displayPage} of {m_totalPages} (Showing {endIndex - startIndex} items)", EditorStyles.boldLabel);
			
			GUILayout.FlexibleSpace();
			
			// Items per page selector
			GUILayout.Label("Items per page:", GUILayout.Width(90));
			int[] pageSizeOptions = { 50, 100, 200, 500 };
			int currentIndex = System.Array.IndexOf(pageSizeOptions, m_itemsPerPage);
			if (currentIndex == -1) currentIndex = 1; // Default to 100
			
			int newIndex = EditorGUILayout.Popup(currentIndex, System.Array.ConvertAll(pageSizeOptions, x => x.ToString()), GUILayout.Width(60));
			if (newIndex != currentIndex)
			{
				m_itemsPerPage = pageSizeOptions[newIndex];
				m_currentPage = 0; // Reset to first page when changing page size
		m_cachedPage = -1;
			}
			
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.BeginHorizontal();
			
			// Navigation buttons
			EditorGUI.BeginDisabledGroup(m_currentPage == 0);
			if (GUILayout.Button("First", GUILayout.Width(50)))
			{
				m_currentPage = 0;
			}
			if (GUILayout.Button("Previous", GUILayout.Width(70)))
			{
				m_currentPage--;
			}
			EditorGUI.EndDisabledGroup();
			
			GUILayout.FlexibleSpace();
			
			// Direct page entry
			GUILayout.Label("Go to page:", GUILayout.Width(70));
			string pageInput = EditorGUILayout.TextField(displayPage.ToString(), GUILayout.Width(50));
			if (int.TryParse(pageInput, out int newPage))
			{
				newPage = Mathf.Clamp(newPage - 1, 0, m_totalPages - 1); // Convert to 0-based
				if (newPage != m_currentPage)
				{
					m_currentPage = newPage;
				}
			}
			
			GUILayout.FlexibleSpace();
			
			EditorGUI.BeginDisabledGroup(m_currentPage >= m_totalPages - 1);
			if (GUILayout.Button("Next", GUILayout.Width(50)))
			{
				m_currentPage++;
			}
			if (GUILayout.Button("Last", GUILayout.Width(50)))
			{
				m_currentPage = m_totalPages - 1;
			}
			EditorGUI.EndDisabledGroup();
			
			EditorGUILayout.EndHorizontal();
		}
		
	private void BuildPageCache(int startIndex, int endIndex)
	{
		m_pageCacheList.Clear();
		
		// Pre-allocate to avoid resizing
		int capacity = endIndex - startIndex;
		if (m_pageCacheList.Capacity < capacity)
			m_pageCacheList.Capacity = capacity;

		// Batch process all items for the current page
		for (int i = startIndex; i < endIndex; i++)
		{
			var path = m_filteredAssets[i];
			
			// All these operations are now optimized:
			// - GetCachedIcon is Unity's cached lookup
			// - GetAssetSize uses our SizeCache
			// - FormatBytes is lightweight
			var icon = AssetDatabase.GetCachedIcon(path);
			var size = RAssetCleaner.GetAssetSize(path); // Uses cache
			var formattedSize = EditorUtility.FormatBytes(size);
			
			m_pageCacheList.Add(new CacheItem 
			{
				icon = icon,
				path = path,
				sizeStr = formattedSize
			});
		}
	}
	}
}
