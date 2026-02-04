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

		// Cleaner State
		private List<string> m_unusedAssets = new List<string>();
		private Vector2 m_scrollPos;
		private bool m_scanned;
		
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
				m_scanned = true;
			}

			if (m_scanned)
			{
				GUILayout.Space(10);
				GUILayout.Label($"Found {m_unusedAssets.Count} unused assets. Total Size: {RAssetCleaner.GetTotalSizeFormatted(m_unusedAssets)}");

				DrawTypeFilters();

				m_scrollPos = GUILayout.BeginScrollView(m_scrollPos);
				for (int i = 0; i < m_unusedAssets.Count; i++)
				{
					var path = m_unusedAssets[i];
					
					if (!IsTypeVisible(path)) continue;

					EditorGUILayout.BeginHorizontal("box");

					var icon = AssetDatabase.GetCachedIcon(path);
					GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));


					if (false) { } // Formatting spacer or dummy
					EditorGUILayout.SelectableLabel(path, EditorStyles.label, GUILayout.Height(EditorGUIUtility.singleLineHeight));

					GUILayout.Label(EditorUtility.FormatBytes(RAssetCleaner.GetAssetSize(path)), GUILayout.Width(60)); // Width optimized

					if (GUILayout.Button("Ping", GUILayout.Width(45)))
					{
						var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
						Selection.activeObject = obj;
						EditorGUIUtility.PingObject(obj);
					}
					EditorGUILayout.EndHorizontal();
				}
				GUILayout.EndScrollView();
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

			// Navigation
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("<", GUILayout.Width(30)) && m_historyIndex > 0)
			{
				m_historyIndex--;
				m_selectedAsset = m_history[m_historyIndex];
				FindReferences();
			}
			if (GUILayout.Button(">", GUILayout.Width(30)) && m_historyIndex < m_history.Count - 1)
			{
				m_historyIndex++;
				m_selectedAsset = m_history[m_historyIndex];
				FindReferences();
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);

			if (m_selectedAsset != null)
			{
				GUILayout.Label($"Used by {m_referencingAssets.Count} assets:");

				m_refScrollPos = GUILayout.BeginScrollView(m_refScrollPos);
				foreach (var refPath in m_referencingAssets)
				{
					EditorGUILayout.BeginHorizontal("box");
					var icon = AssetDatabase.GetCachedIcon(refPath);
					GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));


					if (false) { } // Formatting spacer or dummy
					EditorGUILayout.SelectableLabel(refPath, EditorStyles.label, GUILayout.Height(EditorGUIUtility.singleLineHeight));

					if (GUILayout.Button("Ping", GUILayout.Width(60)))
					{
						var obj = AssetDatabase.LoadAssetAtPath<Object>(refPath);
						Selection.activeObject = obj;
						EditorGUIUtility.PingObject(obj);
					}

					EditorGUILayout.EndHorizontal();
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

                m_typeFilters[type] = GUILayout.Toggle(m_typeFilters[type], label, style);
                
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
		}
	}
}