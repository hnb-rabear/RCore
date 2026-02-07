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

		private int m_tabIndex;
		private string[] m_tabs = { "Cleaner", "Reference Finder", "Settings" };

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
                                     propertyDisplayName = sp.displayName,
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
                             propertyDisplayName = sp.displayName,
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
		private Vector2 m_refScrollPos;
		private List<Object> m_history = new List<Object>();
		private int m_historyIndex = -1;

		// Settings/Style
		private GUIStyle m_boxStyle;


		public static bool IsOpen { get; private set; }

		private void OnEnable()
		{
			IsOpen = true;
			// Load history?
			Selection.selectionChanged += OnSelectionChange;
			
			// Initialize Filters
			foreach (AssetType type in System.Enum.GetValues(typeof(AssetType)))
			{
				if (!m_typeFilters.ContainsKey(type))
					m_typeFilters[type] = true;
			}
			EditorApplication.RepaintProjectWindow();
		}

		private void OnDisable()
		{
			IsOpen = false;
			Selection.selectionChanged -= OnSelectionChange;
			EditorApplication.RepaintProjectWindow();
		}

		private void OnSelectionChange()
		{
			// Only update if we are in the Reference Finder tab (index 1)
			// Or if the user expects it to auto-update. Since the original request implies "when I select... it will be auto filled",
			// it usually implies while looking at the tool.
			// If the tool is docked but hidden, we probably shouldn't do expensive FindReferences.
			// But checking if window is focused is hard. 
			// Checking tab index is reasonable.

			if (m_tabIndex == 1 && Selection.activeObject != null)
			{
				// Check if it's an asset (not scene object) - though checking scene objects is fine too if we support it.
				// AssetDatabase.Contains checks if it's an asset.
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
		}

		private void OnGUI()
		{
			m_tabIndex = GUILayout.Toolbar(m_tabIndex, m_tabs);

			GUILayout.Space(10);

			switch (m_tabIndex)
			{
				case 0: DrawCleanerTab(); break;
				case 1: DrawReferenceFinderTab(); break;
				case 2: DrawSettingsTab(); break;
			}
		}

		private void DrawCleanerTab()
		{
			GUILayout.Label("Project Cleaner", EditorStyles.boldLabel);

			if (GUILayout.Button("Scan Project", GUILayout.Height(30)))
			{
				m_unusedAssets = RAssetCleaner.FindUnusedAssets(RAssetCleanerSettings.Instance.ignorePaths);
				CalculateTypeStats();
				m_currentPage = 0; // Reset to first page
				m_scanned = true;
			}

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

				// Build cache for current page if needed (only once per page change)
				if (m_cachedPage != m_currentPage)
				{
					BuildPageCache(startIndex, endIndex);
					m_cachedPage = m_currentPage;
				}

				m_scrollPos = GUILayout.BeginScrollView(m_scrollPos);

				float rowHeight = 20f;
				int count = endIndex - startIndex;
				
				// Reserve space for all items (single layout call)
				Rect listRect = GUILayoutUtility.GetRect(0, count * rowHeight, GUILayout.ExpandWidth(true));
				
				if (Event.current.type == EventType.Repaint)
				{
					// Draw background for list area? Optional.
				}

				// Draw loop with minimal overhead
				for (int i = 0; i < count; i++)
				{
					if (i >= m_pageCacheList.Count) break; // Should not happen
					
					var item = m_pageCacheList[i];
					
					// Calculate rows relative to listRect
					float y = listRect.y + i * rowHeight;
					
					// Inline rect calculations for speed
					Rect iconRect = new Rect(listRect.x, y, 20, 20);
					Rect pathRect = new Rect(listRect.x + 25, y, listRect.width - 135, 20); // Dynamic width
					Rect sizeRect = new Rect(pathRect.xMax + 5, y, 60, 20);
					Rect btnRect = new Rect(sizeRect.xMax + 5, y, 45, 20);

					// Pure GUI calls - fastest possible rendering
					if (item.icon != null) GUI.Label(iconRect, item.icon);
					GUI.Label(pathRect, item.path); // Non-interactive label is much faster than TextField
					GUI.Label(sizeRect, item.sizeStr);

					if (GUI.Button(btnRect, "Ping"))
					{
						var obj = AssetDatabase.LoadAssetAtPath<Object>(item.path);
						Selection.activeObject = obj;
						EditorGUIUtility.PingObject(obj);
					}
				}
				
				GUILayout.EndScrollView();
				
				DrawPaginationControls();
			}
		}



		private void DrawReferenceFinderTab()
		{
			GUILayout.Label("Find References", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
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
			EditorGUILayout.EndHorizontal();



			GUILayout.Space(10);

			if (m_selectedAsset != null)
			{
				GUILayout.Label($"Used by {m_referencingAssets.Count} assets:");

				m_refScrollPos = GUILayout.BeginScrollView(m_refScrollPos);
				
				for (int i = 0; i < m_referencingAssets.Count; i++)
				{
					var refPath = m_referencingAssets[i];
					var asset = AssetDatabase.LoadAssetAtPath<Object>(refPath);
					
					if (asset == null) continue;

					EditorGUILayout.BeginHorizontal();
					
					// Foldout
					if (!m_foldoutStates.ContainsKey(refPath)) m_foldoutStates[refPath] = false;
					bool folded = m_foldoutStates[refPath];
					
					// Icon & Path
					var icon = AssetDatabase.GetCachedIcon(refPath);
					GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
					if (GUILayout.Button(folded ? "▼" : "▶", GUILayout.Width(25)))
					{
						m_foldoutStates[refPath] = !folded;
						if (!folded && !m_usageDetails.ContainsKey(refPath))
						{
							// Lazy load details
							m_usageDetails[refPath] = FindUsageDetails(refPath, m_selectedAsset);
						}
					}
					EditorGUILayout.LabelField(refPath);
					
					if (GUILayout.Button("Ping", GUILayout.Width(45)))
					{
						Selection.activeObject = asset;
						EditorGUIUtility.PingObject(asset);
					}
					EditorGUILayout.EndHorizontal();
					
					// Draw Details
					if (m_foldoutStates[refPath] && m_usageDetails.ContainsKey(refPath))
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
										// Refresh details now that it's loaded
										m_usageDetails[refPath] = FindUsageDetails(refPath, m_selectedAsset);
									}
								}
								else
								{
									EditorGUILayout.HelpBox("Could not find direct property reference. It might be in a strictly serialized list or hidden.", MessageType.Info);
								}
							}
							else
							{
								EditorGUILayout.HelpBox("Could not find direct property reference. It might be in a strictly serialized list or hidden.", MessageType.Info);
							}
						}
						else
						{
							foreach (var detail in details)
							{
								EditorGUILayout.BeginHorizontal();
								GUILayout.Space(30);
								GUILayout.Label($"{detail.hostObject.GetType().Name}.{detail.propertyDisplayName}", GUILayout.Width(200));
								
								// Ensure serialized object is valid
								if (detail.serializedObject != null && detail.serializedObject.targetObject != null)
								{
									detail.serializedObject.Update();
									// Use PropertyField to handle types (Sprite vs Texture) automatically
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
				if (RAssetCleaner.ReferenceCache.Count > 0)
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
				if (RAssetCleaner.ReferenceCache.Count == 0 && !EditorUtility.DisplayDialog("Cache Missing", "The Reference Graph is not built. Searching without cache is slower. Continue?", "Run Slow Search", "Cancel"))
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
		}

		private void DrawSettingsTab()
		{
			GUILayout.Label("Settings", EditorStyles.boldLabel);
			var settings = RAssetCleanerSettings.Instance;

			settings.showRedOverlay = EditorGUILayout.Toggle("Red Overlay (Unused)", settings.showRedOverlay);
			settings.unusedColor = EditorGUILayout.ColorField("Overlay Color", settings.unusedColor);
			settings.showSize = EditorGUILayout.Toggle("Show Assets Size", settings.showSize);

			GUILayout.Space(10);
			GUILayout.Label("Advanced Search", EditorStyles.boldLabel);
			settings.deepSearch = EditorGUILayout.Toggle(new GUIContent("Deep Search (Slow)", "Scan text contents of assets to find hidden or addressable references."), settings.deepSearch);
			EditorGUILayout.HelpBox("Enable this to find indirect references (e.g. Addressables, AssetBundleWraps) by scanning file contents. This process is slower than standard dependency tracking.", MessageType.Info);
			
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

			if (GUI.changed)
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
			foreach (var path in m_unusedAssets)
			{
				var type = GetAssetType(path);
				if (!m_typeStats.ContainsKey(type))
					m_typeStats[type] = (0, 0);
				
				var (count, size) = m_typeStats[type];
				m_typeStats[type] = (count + 1, size + RAssetCleaner.GetAssetSize(path));
			}
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
			if (m_pageCacheList.Capacity < (endIndex - startIndex))
				m_pageCacheList.Capacity = (endIndex - startIndex);

			for (int i = startIndex; i < endIndex; i++)
			{
				var path = m_filteredAssets[i];
				var icon = AssetDatabase.GetCachedIcon(path);
				var size = RAssetCleaner.GetAssetSize(path);
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