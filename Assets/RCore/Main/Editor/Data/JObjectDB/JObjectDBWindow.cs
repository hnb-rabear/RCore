/**
 * Author HNB-RaBear - 2024
 * JObjectDB Editor Window v2 — Visual Tree View with Inline Editing
 **/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RCore.Data.JObject;
using UnityEditor;
using UnityEngine;
using NJObject = Newtonsoft.Json.Linq.JObject;
using NJArray = Newtonsoft.Json.Linq.JArray;

namespace RCore.Editor.Data.JObject
{
	/// <summary>
	/// A comprehensive editor window for viewing and editing JObjectDB data.
	/// Features: Tree View, Inline Editing, Search/Filter, Collection Tabs,
	/// Type-aware Display (timestamps, color-coding), Copy per Field.
	/// </summary>
	public class JObjectDBWindow : EditorWindow
	{
		//==========================================================================
		// Constants
		//==========================================================================

		private const float MIN_LEFT_PANEL_WIDTH = 140f;
		private const float MAX_LEFT_PANEL_WIDTH = 300f;
		private const float DEFAULT_LEFT_PANEL_WIDTH = 180f;
		private const float INDENT_WIDTH = 16f;
		private const float COPY_BUTTON_WIDTH = 24f;
		private const float SPLITTER_WIDTH = 4f;
		private const int ARRAY_PAGE_SIZE = 20;
		private const float SEARCH_DEBOUNCE_TIME = 0.3f;

		// Color coding for value types
		private static readonly Color COLOR_INT = new(0.4f, 0.85f, 0.95f);    // Cyan
		private static readonly Color COLOR_FLOAT = new(0.4f, 0.85f, 0.95f);  // Cyan
		private static readonly Color COLOR_STRING = new(0.55f, 0.9f, 0.55f); // Green
		private static readonly Color COLOR_BOOL = new(1f, 0.75f, 0.35f);     // Orange
		private static readonly Color COLOR_NULL = new(0.6f, 0.6f, 0.6f);     // Grey
		private static readonly Color COLOR_TIMESTAMP = new(0.8f, 0.7f, 1f);  // Light purple
		private static readonly Color COLOR_SELECTED = new(0.24f, 0.48f, 0.9f, 0.3f);
		private static readonly Color COLOR_DIRTY = new(1f, 0.85f, 0.2f);     // Yellow
		private static readonly Color COLOR_SEARCH_MATCH = new(1f, 0.95f, 0.3f, 0.2f); // Yellow highlight
		private static readonly Color COLOR_DIFF_ADDED = new(0.2f, 0.8f, 0.3f, 0.25f);   // Green
		private static readonly Color COLOR_DIFF_CHANGED = new(1f, 0.7f, 0.2f, 0.25f);   // Orange
		private static readonly Color COLOR_DIFF_REMOVED = new(1f, 0.3f, 0.3f, 0.25f);   // Red

		// Timestamp field name patterns (case-insensitive suffix matching)
		private static readonly string[] TIMESTAMP_SUFFIXES = { "at", "time", "active", "timestamp" };

		// EditorPrefs keys
		private const string PREF_SELECTED_KEY = "JObjectDB_SelectedKey";
		private const string PREF_PANEL_WIDTH = "JObjectDB_PanelWidth";
		private const string PREF_EXPANDED_PATHS = "JObjectDB_ExpandedPaths";

		// Auto-backup
		private const int MAX_AUTO_BACKUPS = 5;

		//==========================================================================
		// State
		//==========================================================================

		private Dictionary<string, string> m_rawData;          // key → raw JSON from PlayerPrefs
		private Dictionary<string, JToken> m_parsedData;       // key → parsed JToken tree
		private List<string> m_sortedKeys;                     // sorted collection keys
		private string m_selectedKey;                          // currently selected collection
		private HashSet<string> m_dirtyKeys = new();           // collections with unsaved changes
		private HashSet<string> m_expandedPaths = new();       // expanded tree nodes (dot-notation)
		private Dictionary<string, int> m_arrayPages = new();  // array path → current page

		// Search (deep)
		private string m_searchQuery = "";
		private string m_appliedSearchQuery = "";
		private double m_searchLastTypedTime;
		private HashSet<string> m_searchMatchedPaths;           // exact paths that match search query
		private HashSet<string> m_searchAncestorPaths;          // parent paths to auto-expand/show

		// Diff
		private bool m_diffEnabled;
		private JToken m_diffBaseToken;                         // baseline data for comparison
		private HashSet<string> m_diffChangedPaths = new();     // paths where values differ
		private HashSet<string> m_diffAddedPaths = new();       // paths only in current
		private HashSet<string> m_diffRemovedPaths = new();     // paths only in baseline
		private HashSet<string> m_diffAncestorPaths = new();    // parents of any diff path (precomputed)

		// Presets
		private string[] m_presetNames;
		private int m_selectedPresetIndex = -1;

		// Layout
		private float m_leftPanelWidth = DEFAULT_LEFT_PANEL_WIDTH;
		private bool m_isResizingSplitter;
		private Vector2 m_leftScrollPos;
		private Vector2 m_treeScrollPos;

		// Feedback
		private string m_copyFeedbackPath;
		private double m_copyFeedbackTime;
		private string m_statusMessage;
		private double m_statusMessageTime;

		// Cached Icons
		private GUIContent m_iconSave;
		private GUIContent m_iconRefresh;
		private GUIContent m_iconBackup;
		private GUIContent m_iconRestore;
		private GUIContent m_iconImport;
		private GUIContent m_iconDelete;
		private GUIContent m_iconCopy;
		private GUIContent m_iconCopyDone;
		private GUIContent m_iconEdit;
		private GUIContent m_iconApply;
		private GUIContent m_iconLoaded;
		private GUIContent m_iconUnloaded;
		private GUIContent m_iconDirty;

		//==========================================================================
		// Window Management
		//==========================================================================

		[MenuItem("RCore/JObject Database/JObjectDB Editor %&n", priority = 100)]
		public static void ShowWindow()
		{
			var window = GetWindow<JObjectDBWindow>("JObject Database", true);
			window.minSize = new Vector2(600, 400);
			window.Show();
		}

		private void OnEnable()
		{
			InitIcons();
			LoadUIState();
			RefreshData();
			RefreshPresetList();
			EditorApplication.playModeStateChanged += OnPlayModeChanged;
		}

		private void OnDisable()
		{
			SaveUIState();
			EditorApplication.playModeStateChanged -= OnPlayModeChanged;
		}

		private void InitIcons()
		{
			m_iconSave = LoadIcon("SaveAs");
			m_iconRefresh = LoadIcon("Refresh");
			m_iconBackup = LoadIcon("d_SaveAs@2x");
			m_iconRestore = LoadIcon("d_Refresh@2x");
			m_iconImport = LoadIcon("Download-Available") ?? LoadIcon("Import") ?? LoadIcon("Collab.FileAdded");
			m_iconDelete = LoadIcon("TreeEditor.Trash");
			m_iconCopy = LoadIcon("Clipboard");
			m_iconCopyDone = LoadIcon("TestPassed");
			m_iconEdit = LoadIcon("d_editicon.sml");
			m_iconApply = LoadIcon("SaveAs");
			m_iconLoaded = LoadIcon("GreenLight");
			m_iconUnloaded = LoadIcon("Unlinked");
			m_iconDirty = LoadIcon("OrangeLight");
		}

		private static GUIContent LoadIcon(string iconName)
		{
			try
			{
				var content = EditorGUIUtility.IconContent(iconName);
				if (content != null && content.image != null)
					return content;
			}
			catch { /* Icon not found */ }

			try
			{
				var content = EditorGUIUtility.IconContent("d_" + iconName);
				if (content != null && content.image != null)
					return content;
			}
			catch { /* Dark theme icon not found */ }

			return null;
		}

		private void OnFocus()
		{
			// Auto-refresh when window regains focus
			if (!m_isResizingSplitter)
				RefreshData();
		}

		//==========================================================================
		// Data Management
		//==========================================================================

		private void RefreshData()
		{
			m_rawData = JObjectDB.GetAllData();
			m_parsedData = new Dictionary<string, JToken>();
			m_sortedKeys = m_rawData.Keys.OrderBy(k => k).ToList();

			foreach (var pair in m_rawData)
			{
				try
				{
					m_parsedData[pair.Key] = JToken.Parse(pair.Value);
				}
				catch (Exception ex)
				{
					Debug.LogError($"[JObjectDB] Failed to parse JSON for key '{pair.Key}': {ex.Message}");
				}
			}

			// Auto-select first collection if none selected
			if (string.IsNullOrEmpty(m_selectedKey) && m_sortedKeys.Count > 0)
				m_selectedKey = m_sortedKeys[0];

			// Clear dirty state on refresh
			m_dirtyKeys.Clear();
			Repaint();
		}

		private void SaveCollection(string key)
		{
			if (!m_parsedData.TryGetValue(key, out var token))
				return;

			string json = token.ToString(Formatting.None);
			PlayerPrefs.SetString(key, json);
			m_rawData[key] = json;

			// Sync runtime data if playing
			if (Application.isPlaying)
			{
				var collection = JObjectDB.GetCollection(key);
				if (collection != null)
				{
					collection.Load(json);
					SetStatus($"✓ Saved & synced '{key}' to runtime");
				}
				else
				{
					SetStatus($"✓ Saved '{key}' (not loaded in runtime)");
				}
			}
			else
			{
				SetStatus($"✓ Saved '{key}'");
			}

			m_dirtyKeys.Remove(key);
			Repaint();
		}

		private void SaveAllDirty()
		{
			foreach (string key in m_dirtyKeys.ToList())
				SaveCollection(key);
		}

		private void MarkDirty(string collectionKey)
		{
			m_dirtyKeys.Add(collectionKey);
		}

		private void SetStatus(string message)
		{
			m_statusMessage = message;
			m_statusMessageTime = EditorApplication.timeSinceStartup;
		}

		//==========================================================================
		// Main GUI
		//==========================================================================

		private void OnGUI()
		{
			if (m_rawData == null)
				RefreshData();

			DrawToolbar();

			EditorGUILayout.BeginHorizontal();
			{
				DrawLeftPanel();
				DrawSplitter();
				DrawRightPanel();
			}
			EditorGUILayout.EndHorizontal();

			DrawStatusBar();

			// Handle search debounce
			if (m_searchQuery != m_appliedSearchQuery
			    && EditorApplication.timeSinceStartup - m_searchLastTypedTime > SEARCH_DEBOUNCE_TIME)
			{
				m_appliedSearchQuery = m_searchQuery;
				RebuildSearchCache();
				Repaint();
			}
		}

		//==========================================================================
		// Toolbar
		//==========================================================================

		private void DrawToolbar()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

			// Search field
			EditorGUI.BeginChangeCheck();
			m_searchQuery = EditorGUILayout.TextField(m_searchQuery, EditorStyles.toolbarSearchField, GUILayout.MinWidth(150));
			if (EditorGUI.EndChangeCheck())
				m_searchLastTypedTime = EditorApplication.timeSinceStartup;

			if (!string.IsNullOrEmpty(m_searchQuery))
			{
				if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(22)))
				{
					m_searchQuery = "";
					m_appliedSearchQuery = "";
					m_searchMatchedPaths = null;
					m_searchAncestorPaths = null;
					GUI.FocusControl(null);
				}
			}

			GUILayout.FlexibleSpace();

			// Action buttons with icons
			GUI.enabled = m_dirtyKeys.Count > 0;
			if (GUILayout.Button(new GUIContent($" Save ({m_dirtyKeys.Count})", m_iconSave?.image), EditorStyles.toolbarButton, GUILayout.Width(80)))
				SaveAllDirty();
			GUI.enabled = true;

			if (GUILayout.Button(new GUIContent(" Reload", m_iconRefresh?.image), EditorStyles.toolbarButton, GUILayout.Width(65)))
				RefreshData();

			if (GUILayout.Button(new GUIContent(" Backup", m_iconBackup?.image), EditorStyles.toolbarButton, GUILayout.Width(68)))
				JObjectDB.Backup(openDirectory: true);

			if (GUILayout.Button(new GUIContent(" Import", m_iconImport?.image), EditorStyles.toolbarButton, GUILayout.Width(70)))
			{
				string savesDir = Application.dataPath.Replace("Assets", "Saves");
				string path = EditorUtility.OpenFilePanel("Import Save Data", savesDir, "json,txt");
				if (!string.IsNullOrEmpty(path))
					ImportFromFile(path);
			}

			GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
			if (GUILayout.Button(new GUIContent(" Delete", m_iconDelete?.image), EditorStyles.toolbarButton, GUILayout.Width(65)))
			{
				if (EditorUtility.DisplayDialog("Confirm", "Delete ALL JObjectDB data from PlayerPrefs?", "Delete", "Cancel"))
				{
					JObjectDB.DeleteAll();
					RefreshData();
				}
			}
			GUI.backgroundColor = Color.white;

			EditorGUILayout.EndHorizontal();
		}

		//==========================================================================
		// Left Panel — Collection List
		//==========================================================================

		private void DrawLeftPanel()
		{
			EditorGUILayout.BeginVertical(GUILayout.Width(m_leftPanelWidth));
			EditorGUILayout.LabelField($"Collections ({m_sortedKeys?.Count ?? 0})", EditorStyles.boldLabel);

			m_leftScrollPos = EditorGUILayout.BeginScrollView(m_leftScrollPos);

			if (m_sortedKeys != null)
			{
				foreach (string key in m_sortedKeys)
				{
					bool isSelected = key == m_selectedKey;
					bool isDirty = m_dirtyKeys.Contains(key);
					bool isLoaded = Application.isPlaying && JObjectDB.collections.ContainsKey(key);

					// Background highlight for selected
					var rect = EditorGUILayout.BeginHorizontal();
					if (isSelected)
						EditorGUI.DrawRect(rect, COLOR_SELECTED);

					// Left color bar indicator (no icons)
					var barRect = new Rect(rect.x, rect.y + 2, 3, rect.height - 4);
					if (isDirty)
						EditorGUI.DrawRect(barRect, COLOR_DIRTY);
					else if (isLoaded)
						EditorGUI.DrawRect(barRect, new Color(0.3f, 0.85f, 0.4f));
					
					GUILayout.Space(8);

					// Collection name
					var labelStyle = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
					if (GUILayout.Button(key, labelStyle))
					{
						m_selectedKey = key;
						m_treeScrollPos = Vector2.zero;
						if (m_diffEnabled) ClearDiff();
					}

					EditorGUILayout.EndHorizontal();
				}
			}

			EditorGUILayout.EndScrollView();

			// Preset section
			DrawPresetSection();

			EditorGUILayout.EndVertical();
		}

		//==========================================================================
		// Splitter
		//==========================================================================

		private void DrawSplitter()
		{
			var splitterRect = EditorGUILayout.BeginVertical(GUILayout.Width(SPLITTER_WIDTH));
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();

			EditorGUI.DrawRect(splitterRect, new Color(0.15f, 0.15f, 0.15f));
			EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

			if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
			{
				m_isResizingSplitter = true;
				Event.current.Use();
			}

			if (m_isResizingSplitter)
			{
				if (Event.current.type == EventType.MouseDrag)
				{
					m_leftPanelWidth = Mathf.Clamp(Event.current.mousePosition.x, MIN_LEFT_PANEL_WIDTH, MAX_LEFT_PANEL_WIDTH);
					Event.current.Use();
					Repaint();
				}
				if (Event.current.type == EventType.MouseUp)
				{
					m_isResizingSplitter = false;
					Event.current.Use();
				}
			}
		}

		//==========================================================================
		// Right Panel — Tree View
		//==========================================================================

		private void DrawRightPanel()
		{
			EditorGUILayout.BeginVertical();

			if (string.IsNullOrEmpty(m_selectedKey) || !m_parsedData.ContainsKey(m_selectedKey))
			{
				EditorGUILayout.HelpBox("Select a collection from the left panel.", MessageType.Info);
				EditorGUILayout.EndVertical();
				return;
			}

			// Header
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(m_selectedKey, EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			if (m_dirtyKeys.Contains(m_selectedKey))
			{
				GUI.backgroundColor = COLOR_DIRTY;
				if (GUILayout.Button(new GUIContent(" Apply", m_iconApply?.image), GUILayout.Width(75)))
					SaveCollection(m_selectedKey);
				GUI.backgroundColor = Color.white;
			}

			if (GUILayout.Button(new GUIContent(" Copy JSON", m_iconCopy?.image), GUILayout.Width(95)))
			{
				string json = m_parsedData[m_selectedKey].ToString(Formatting.Indented);
				EditorGUIUtility.systemCopyBuffer = json;
				SetStatus("✓ JSON copied to clipboard");
			}

			// Diff toggle
			if (m_diffEnabled)
			{
				GUI.backgroundColor = COLOR_DIFF_CHANGED;
				if (GUILayout.Button("Clear Diff", GUILayout.Width(75)))
					ClearDiff();
				GUI.backgroundColor = Color.white;
			}
			else
			{
				if (GUILayout.Button("Compare", GUILayout.Width(70)))
				{
					string savesDir = Application.dataPath.Replace("Assets", "Saves");
					string diffPath = EditorUtility.OpenFilePanel("Select file to compare with", savesDir, "json,txt");
					if (!string.IsNullOrEmpty(diffPath))
						StartDiff(diffPath);
				}
			}

			if (GUILayout.Button(new GUIContent(" Edit Raw", m_iconEdit?.image), GUILayout.Width(85)))
			{
				string json = m_parsedData[m_selectedKey].ToString(Formatting.Indented);
				TextEditorWindow.ShowWindow(json, result =>
				{
					try
					{
						var parsed = JToken.Parse(result);
						m_parsedData[m_selectedKey] = parsed;
						MarkDirty(m_selectedKey);
						Repaint();
					}
					catch (Exception ex)
					{
						Debug.LogError($"Invalid JSON: {ex.Message}");
					}
				});
			}

			EditorGUILayout.EndHorizontal();

			// Play-mode warning
			if (Application.isPlaying && m_dirtyKeys.Contains(m_selectedKey))
			{
				EditorGUILayout.HelpBox(
					"⚠ Play Mode: cached values (timers, counters) may not refresh after Apply. Consider pausing the game.",
					MessageType.Warning);
			}

			// Diff legend
			if (m_diffEnabled)
			{
				EditorGUILayout.BeginHorizontal();
				DrawColorLegend(COLOR_DIFF_CHANGED, "Changed");
				DrawColorLegend(COLOR_DIFF_ADDED, "New");
				DrawColorLegend(COLOR_DIFF_REMOVED, "Removed");
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();
			}

			// Tree view
			m_treeScrollPos = EditorGUILayout.BeginScrollView(m_treeScrollPos);
			{
				var token = m_parsedData[m_selectedKey];
				DrawTreeNode(m_selectedKey, "", token, 0);

				// Show removed fields at the end
				if (m_diffEnabled && m_diffRemovedPaths.Count > 0)
				{
					GUILayout.Space(8);
					var prevColor = GUI.contentColor;
					GUI.contentColor = new Color(1f, 0.4f, 0.4f);
					EditorGUILayout.LabelField("── Removed Fields ──", EditorStyles.boldLabel);
					foreach (string removedPath in m_diffRemovedPaths.OrderBy(p => p))
					{
						var rect = EditorGUILayout.GetControlRect();
						EditorGUI.DrawRect(rect, COLOR_DIFF_REMOVED);
						EditorGUI.LabelField(rect, $"  ✕ {removedPath}");
					}
					GUI.contentColor = prevColor;
				}
			}
			EditorGUILayout.EndScrollView();

			EditorGUILayout.EndVertical();
		}

		//==========================================================================
		// Tree Node Rendering (Recursive)
		//==========================================================================

		private void DrawTreeNode(string collectionKey, string path, JToken token, int depth)
		{
			if (token == null) return;

			switch (token.Type)
			{
				case JTokenType.Object:
					DrawObjectNode(collectionKey, path, (NJObject)token, depth);
					break;

				case JTokenType.Array:
					DrawArrayNode(collectionKey, path, (NJArray)token, depth);
					break;

				default:
					// Leaf values are drawn by their parent
					break;
			}
		}

		private void DrawObjectNode(string collectionKey, string path, NJObject obj, int depth)
		{
			foreach (var property in obj.Properties())
			{
				string fieldPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";

				// Search filtering
				if (!IsVisibleInSearch(fieldPath, property.Value))
					continue;

				if (property.Value.Type == JTokenType.Object || property.Value.Type == JTokenType.Array)
				{
					DrawFoldoutContainer(collectionKey, fieldPath, property.Name, property.Value, depth);
				}
				else
				{
					DrawValueField(collectionKey, fieldPath, property.Name, property, depth);
				}
			}
		}

		private void DrawArrayNode(string collectionKey, string path, NJArray array, int depth)
		{
			bool isSearchActive = m_searchMatchedPaths != null;
			int totalItems = array.Count;
			
			// When search is active, skip pagination — show all matching items
			int startIndex, endIndex;
			if (isSearchActive)
			{
				startIndex = 0;
				endIndex = totalItems;
			}
			else
			{
				string pageKey = path;
				if (!m_arrayPages.TryGetValue(pageKey, out int currentPage))
					currentPage = 0;

				startIndex = currentPage * ARRAY_PAGE_SIZE;
				endIndex = Mathf.Min(startIndex + ARRAY_PAGE_SIZE, totalItems);
			}

			for (int i = startIndex; i < endIndex; i++)
			{
				string elementPath = $"{path}[{i}]";
				var element = array[i];

				if (!IsVisibleInSearch(elementPath, element))
					continue;

				if (element.Type == JTokenType.Object || element.Type == JTokenType.Array)
				{
					DrawFoldoutContainer(collectionKey, elementPath, $"[{i}]", element, depth);
				}
				else
				{
					DrawValueField(collectionKey, elementPath, $"[{i}]", null, depth, element, i, array);
				}
			}

			// Pagination controls — only when NOT searching
			if (!isSearchActive && totalItems > ARRAY_PAGE_SIZE)
			{
				string pageKey = path;
				if (!m_arrayPages.TryGetValue(pageKey, out int currentPage))
					currentPage = 0;
					
				EditorGUI.indentLevel = depth + 1;
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(depth * INDENT_WIDTH);

				int totalPages = Mathf.CeilToInt((float)totalItems / ARRAY_PAGE_SIZE);
				GUI.enabled = currentPage > 0;
				if (GUILayout.Button("◀", GUILayout.Width(30)))
				{
					m_arrayPages[pageKey] = currentPage - 1;
					Repaint();
				}
				GUI.enabled = true;

				GUILayout.Label($"Page {currentPage + 1}/{totalPages} ({totalItems} items)", EditorStyles.centeredGreyMiniLabel);

				GUI.enabled = currentPage < totalPages - 1;
				if (GUILayout.Button("▶", GUILayout.Width(30)))
				{
					m_arrayPages[pageKey] = currentPage + 1;
					Repaint();
				}
				GUI.enabled = true;

				EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel = 0;
			}
		}

		private void DrawFoldoutContainer(string collectionKey, string path, string label, JToken token, int depth)
		{
			bool wasExpanded = m_expandedPaths.Contains(path);
			// Auto-expand when search matches this node or a descendant
			if (m_searchMatchedPaths != null && (m_searchMatchedPaths.Contains(path)
			    || (m_searchAncestorPaths != null && m_searchAncestorPaths.Contains(path))))
				wasExpanded = true;

			var foldoutRect = EditorGUILayout.BeginHorizontal();
			if (IsSearchMatch(path))
				EditorGUI.DrawRect(foldoutRect, COLOR_SEARCH_MATCH);
			else if (m_diffEnabled && HasDiffChildren(path))
				EditorGUI.DrawRect(foldoutRect, new Color(1f, 0.7f, 0.2f, 0.1f));
			GUILayout.Space(depth * INDENT_WIDTH);

			// Count info
			string countLabel;
			bool isArray = token.Type == JTokenType.Array;
			if (isArray)
				countLabel = $"[{((NJArray)token).Count} items]";
			else
				countLabel = $"{{{((NJObject)token).Count} fields}}";

			bool isExpanded = EditorGUILayout.Foldout(wasExpanded, $"{label}  {countLabel}", true);

			// Array: Add item button
			if (isArray)
			{
				if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(16)))
				{
					var arr = (NJArray)token;
					// Infer type from last element, default to int 0
					JToken newItem = arr.Count > 0 ? arr.Last.DeepClone() : new JValue(0);
					arr.Add(newItem);
					MarkDirty(collectionKey);
				}
			}

			// Copy button
			DrawCopyButton(path, token.ToString(Formatting.None));

			EditorGUILayout.EndHorizontal();

			if (isExpanded != wasExpanded)
			{
				if (isExpanded)
					m_expandedPaths.Add(path);
				else
					m_expandedPaths.Remove(path);
			}

			if (isExpanded)
				DrawTreeNode(collectionKey, path, token, depth + 1);
		}

		//==========================================================================
		// Value Field Rendering + Inline Editing
		//==========================================================================

		private void DrawValueField(string collectionKey, string path, string label, JProperty property, int depth,
			JToken directToken = null, int arrayIndex = -1, NJArray parentArray = null)
		{
			var token = property?.Value ?? directToken;
			if (token == null) return;

				// Get full-width rect for this row
			float indent = depth * INDENT_WIDTH;
			float labelWidth = Mathf.Max(120, 200 - indent);
			bool isArrayElement = parentArray != null && arrayIndex >= 0;
			float buttonsWidth = COPY_BUTTON_WIDTH + (isArrayElement ? COPY_BUTTON_WIDTH + 2 : 0);
			
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(indent);
			var rowRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
			EditorGUILayout.EndHorizontal();
			
			// Split rect into: [Label] [Value] [DeleteBtn?] [CopyButton]
			var labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowRect.height);
			var copyRect = new Rect(rowRect.xMax - COPY_BUTTON_WIDTH, rowRect.y, COPY_BUTTON_WIDTH, rowRect.height);
			Rect deleteRect = default;
			if (isArrayElement)
				deleteRect = new Rect(copyRect.x - COPY_BUTTON_WIDTH - 2, rowRect.y, COPY_BUTTON_WIDTH, rowRect.height);
			var valueRect = new Rect(labelRect.xMax + 2, rowRect.y, rowRect.xMax - buttonsWidth - labelRect.xMax - 4, rowRect.height);

			// Search highlight + Diff highlight
			if (IsSearchMatch(path))
				EditorGUI.DrawRect(rowRect, COLOR_SEARCH_MATCH);
			else if (m_diffEnabled && m_diffChangedPaths.Contains(path))
				EditorGUI.DrawRect(rowRect, COLOR_DIFF_CHANGED);
			else if (m_diffEnabled && m_diffAddedPaths.Contains(path))
				EditorGUI.DrawRect(rowRect, COLOR_DIFF_ADDED);

			// Label
			EditorGUI.LabelField(labelRect, label);

			// Value with type-specific color
			var prevColor = GUI.contentColor;

			EditorGUI.BeginChangeCheck();
			JToken newValue = null;

			switch (token.Type)
			{
				case JTokenType.Integer:
					GUI.contentColor = COLOR_INT;
					long intVal = token.Value<long>();
					long newIntVal = EditorGUI.LongField(valueRect, intVal);
					if (EditorGUI.EndChangeCheck())
						newValue = new JValue(newIntVal);
					break;

				case JTokenType.Float:
					GUI.contentColor = COLOR_FLOAT;
					double floatVal = token.Value<double>();
					double newFloatVal = EditorGUI.DoubleField(valueRect, floatVal);
					if (EditorGUI.EndChangeCheck())
						newValue = new JValue(newFloatVal);
					break;

				case JTokenType.String:
					GUI.contentColor = COLOR_STRING;
					string strVal = token.Value<string>() ?? "";
					string newStrVal = EditorGUI.TextField(valueRect, strVal);
					if (EditorGUI.EndChangeCheck())
						newValue = new JValue(newStrVal);
					break;

				case JTokenType.Boolean:
					GUI.contentColor = COLOR_BOOL;
					bool boolVal = token.Value<bool>();
					bool newBoolVal = EditorGUI.Toggle(valueRect, boolVal);
					if (EditorGUI.EndChangeCheck())
						newValue = new JValue(newBoolVal);
					break;

				case JTokenType.Null:
					GUI.contentColor = COLOR_NULL;
					EditorGUI.LabelField(valueRect, "(null)");
					EditorGUI.EndChangeCheck();
					break;

				default:
					EditorGUI.LabelField(valueRect, token.ToString());
					EditorGUI.EndChangeCheck();
					break;
			}

			GUI.contentColor = prevColor;

			// Apply change
			if (newValue != null)
			{
				if (property != null)
					property.Value = newValue;
				else if (parentArray != null && arrayIndex >= 0)
					parentArray[arrayIndex] = newValue;

				MarkDirty(collectionKey);
			}

			// Copy button — absolute positioned, always visible
			DrawCopyButton(copyRect, path, token.ToString());

			// Delete button for array elements
			if (isArrayElement)
			{
				GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
				if (GUI.Button(deleteRect, "✕"))
				{
					parentArray.RemoveAt(arrayIndex);
					MarkDirty(collectionKey);
					GUIUtility.ExitGUI(); // Prevent layout errors after modifying collection
				}
				GUI.backgroundColor = Color.white;
			}

			// Timestamp display
			if (token.Type == JTokenType.Integer && IsTimestampField(label))
			{
				long ts = token.Value<long>();
				if (ts > 1000000000 && ts < 9999999999) // reasonable Unix timestamp range
				{
					var prevContentColor = GUI.contentColor;
					GUI.contentColor = COLOR_TIMESTAMP;
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(depth * INDENT_WIDTH + 20);
					try
					{
						var dt = DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime;
						EditorGUILayout.LabelField($"📅 {dt:yyyy-MM-dd HH:mm:ss}", EditorStyles.miniLabel);
					}
					catch
					{
						EditorGUILayout.LabelField("📅 (invalid timestamp)", EditorStyles.miniLabel);
					}
					EditorGUILayout.EndHorizontal();
					GUI.contentColor = prevContentColor;
				}
			}
		}

		//==========================================================================
		// Copy Button
		//==========================================================================

		private void DrawCopyButton(string path, string value)
		{
			bool justCopied = m_copyFeedbackPath == path
			                  && EditorApplication.timeSinceStartup - m_copyFeedbackTime < 1.0;

			if (justCopied)
				GUI.backgroundColor = Color.green;

			var icon = justCopied ? m_iconCopyDone : m_iconCopy;
			var content = icon != null ? icon : new GUIContent(justCopied ? "✓" : "C");
			if (GUILayout.Button(content, GUILayout.Width(COPY_BUTTON_WIDTH), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
			{
				EditorGUIUtility.systemCopyBuffer = value;
				m_copyFeedbackPath = path;
				m_copyFeedbackTime = EditorApplication.timeSinceStartup;
			}

			if (justCopied)
				GUI.backgroundColor = Color.white;
		}
		
		private void DrawCopyButton(Rect rect, string path, string value)
		{
			bool justCopied = m_copyFeedbackPath == path
			                  && EditorApplication.timeSinceStartup - m_copyFeedbackTime < 1.0;

			if (justCopied)
				GUI.backgroundColor = Color.green;

			var icon = justCopied ? m_iconCopyDone : m_iconCopy;
			var content = icon != null ? icon : new GUIContent(justCopied ? "✓" : "C");
			if (GUI.Button(rect, content))
			{
				EditorGUIUtility.systemCopyBuffer = value;
				m_copyFeedbackPath = path;
				m_copyFeedbackTime = EditorApplication.timeSinceStartup;
			}

			if (justCopied)
				GUI.backgroundColor = Color.white;
		}

		//==========================================================================
		// Search
		//==========================================================================

		private void RebuildSearchCache()
		{
			if (string.IsNullOrEmpty(m_appliedSearchQuery))
			{
				m_searchMatchedPaths = null;
				m_searchAncestorPaths = null;
				return;
			}

			m_searchMatchedPaths = new HashSet<string>();
			m_searchAncestorPaths = new HashSet<string>();

			if (string.IsNullOrEmpty(m_selectedKey) || !m_parsedData.TryGetValue(m_selectedKey, out var token))
				return;

			string query = m_appliedSearchQuery.ToLowerInvariant();
			SearchRecursive(token, "", query);
		}

		/// <summary>
		/// Recursively walks the JToken tree, collecting paths whose field names match the query.
		/// Also collects all ancestor paths so they can be shown/auto-expanded.
		/// </summary>
		private void SearchRecursive(JToken token, string path, string query)
		{
			if (token.Type == JTokenType.Object)
			{
				foreach (var prop in ((NJObject)token).Properties())
				{
					string fieldPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
					if (prop.Name.ToLowerInvariant().Contains(query))
					{
						m_searchMatchedPaths.Add(fieldPath);
						AddAncestorPaths(fieldPath);
					}
					SearchRecursive(prop.Value, fieldPath, query);
				}
			}
			else if (token.Type == JTokenType.Array)
			{
				var arr = (NJArray)token;
				for (int i = 0; i < arr.Count; i++)
				{
					string elementPath = $"{path}[{i}]";
					SearchRecursive(arr[i], elementPath, query);
				}
			}
		}

		private void AddAncestorPaths(string path)
		{
			// Walk up the path adding all ancestors: "a.b.c" → "a.b", "a"
			for (int i = path.Length - 1; i >= 0; i--)
			{
				if (path[i] == '.' || path[i] == '[')
				{
					string ancestor = path.Substring(0, i);
					if (!m_searchAncestorPaths.Add(ancestor))
						break; // Already added this and all its ancestors
				}
			}
		}

		private bool IsVisibleInSearch(string path, JToken token)
		{
			if (m_searchMatchedPaths == null)
				return true; // No search active

			// This path itself is a match
			if (m_searchMatchedPaths.Contains(path))
				return true;

			// This path is an ancestor of a match (parent of a matched field)
			if (m_searchAncestorPaths.Contains(path))
				return true;

			// This path is a descendant of a matched container
			// e.g. search "currencies" matches container → children "currencies.coins" should be visible
			for (int i = 0; i < path.Length; i++)
			{
				if (path[i] == '.' || path[i] == '[')
				{
					if (m_searchMatchedPaths.Contains(path.Substring(0, i)))
						return true;
				}
			}

			return false;
		}

		private bool IsSearchMatch(string path)
		{
			return m_searchMatchedPaths != null && m_searchMatchedPaths.Contains(path);
		}

		//==========================================================================
		// Status Bar
		//==========================================================================

		private void DrawStatusBar()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

			// Status message with fade-out
			if (!string.IsNullOrEmpty(m_statusMessage))
			{
				double elapsed = EditorApplication.timeSinceStartup - m_statusMessageTime;
				if (elapsed < 3.0)
				{
					float alpha = elapsed < 2.0f ? 1f : 1f - (float)(elapsed - 2.0) / 1f;
					var prevColor = GUI.contentColor;
					GUI.contentColor = new Color(1, 1, 1, alpha);
					GUILayout.Label(m_statusMessage);
					GUI.contentColor = prevColor;
					Repaint(); // Keep repainting for fade animation
				}
				else
				{
					m_statusMessage = null;
				}
			}

			GUILayout.FlexibleSpace();

			int totalCollections = m_sortedKeys?.Count ?? 0;
			GUILayout.Label($"{totalCollections} collections", EditorStyles.miniLabel);

			if (m_dirtyKeys.Count > 0)
			{
				var prevColor = GUI.contentColor;
				GUI.contentColor = COLOR_DIRTY;
				GUILayout.Label($"● {m_dirtyKeys.Count} unsaved", EditorStyles.miniLabel);
				GUI.contentColor = prevColor;
			}

			EditorGUILayout.EndHorizontal();
		}

		//==========================================================================
		// Persist UI State (Feature 4)
		//==========================================================================

		private void SaveUIState()
		{
			EditorPrefs.SetString(PREF_SELECTED_KEY, m_selectedKey ?? "");
			EditorPrefs.SetFloat(PREF_PANEL_WIDTH, m_leftPanelWidth);
			if (m_expandedPaths.Count > 0 && m_expandedPaths.Count < 500)
				EditorPrefs.SetString(PREF_EXPANDED_PATHS, JsonConvert.SerializeObject(m_expandedPaths.ToList()));
			else
				EditorPrefs.DeleteKey(PREF_EXPANDED_PATHS);
		}

		private void LoadUIState()
		{
			m_selectedKey = EditorPrefs.GetString(PREF_SELECTED_KEY, "");
			if (string.IsNullOrEmpty(m_selectedKey)) m_selectedKey = null;
			m_leftPanelWidth = EditorPrefs.GetFloat(PREF_PANEL_WIDTH, DEFAULT_LEFT_PANEL_WIDTH);
			string expandedJson = EditorPrefs.GetString(PREF_EXPANDED_PATHS, "");
			if (!string.IsNullOrEmpty(expandedJson))
			{
				try
				{
					var list = JsonConvert.DeserializeObject<List<string>>(expandedJson);
					if (list != null) m_expandedPaths = new HashSet<string>(list);
				}
				catch { /* ignore corrupt data */ }
			}
		}

		//==========================================================================
		// Auto-backup on Play (Feature 2)
		//==========================================================================

		private void OnPlayModeChanged(PlayModeStateChange state)
		{
			if (state != PlayModeStateChange.ExitingEditMode)
				return;

			try
			{
				string dir = Application.dataPath.Replace("Assets", "Saves");
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				// Rotate old backups: keep only MAX_AUTO_BACKUPS
				var existingBackups = Directory.GetFiles(dir, "AutoBackup_*.json")
					.OrderByDescending(File.GetLastWriteTime)
					.ToArray();

				for (int i = MAX_AUTO_BACKUPS - 1; i < existingBackups.Length; i++)
				{
					try { File.Delete(existingBackups[i]); } catch { }
				}

				string fileName = $"AutoBackup_{DateTime.Now:yyMMdd_HHmm}";
				JObjectDB.Backup(fileName);
				Debug.Log($"[JObjectDB] Auto-backup saved: {fileName}.json");
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[JObjectDB] Auto-backup failed: {ex.Message}");
			}
		}

		//==========================================================================
		// Presets (Feature 3)
		//==========================================================================

		private void DrawPresetSection()
		{
			GUILayout.Space(4);
			EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

			if (m_presetNames != null && m_presetNames.Length > 0)
			{
				m_selectedPresetIndex = EditorGUILayout.Popup(m_selectedPresetIndex, m_presetNames);

				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Load", GUILayout.Height(20)))
				{
					if (m_selectedPresetIndex >= 0 && m_selectedPresetIndex < m_presetNames.Length)
					{
						string path = Path.Combine(GetPresetsDirectory(), m_presetNames[m_selectedPresetIndex] + ".json");
						if (File.Exists(path))
						{
							ImportFromFile(path);
							SetStatus($"✓ Loaded preset: {m_presetNames[m_selectedPresetIndex]}");
						}
					}
				}

				GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
				if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(20)))
				{
					if (m_selectedPresetIndex >= 0 && m_selectedPresetIndex < m_presetNames.Length)
					{
						string presetName = m_presetNames[m_selectedPresetIndex];
						if (EditorUtility.DisplayDialog("Delete Preset", $"Delete preset '{presetName}'?", "Delete", "Cancel"))
						{
							string path = Path.Combine(GetPresetsDirectory(), presetName + ".json");
							if (File.Exists(path)) File.Delete(path);
							RefreshPresetList();
							SetStatus($"✓ Deleted preset: {presetName}");
						}
					}
				}
				GUI.backgroundColor = Color.white;
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				EditorGUILayout.LabelField("(no presets)", EditorStyles.centeredGreyMiniLabel);
			}

			if (GUILayout.Button("Save Current as Preset", GUILayout.Height(20)))
			{
				string presetName = EditorInputDialog.Show("Save Preset", "Enter preset name:", "");
				if (!string.IsNullOrEmpty(presetName))
				{
					SavePreset(presetName);
					SetStatus($"✓ Saved preset: {presetName}");
				}
			}
		}

		private string GetPresetsDirectory()
		{
			string dir = Path.Combine(Application.dataPath.Replace("Assets", "Saves"), "Presets");
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			return dir;
		}

		private void RefreshPresetList()
		{
			string dir = GetPresetsDirectory();
			if (Directory.Exists(dir))
			{
				m_presetNames = Directory.GetFiles(dir, "*.json")
					.Select(Path.GetFileNameWithoutExtension)
					.OrderBy(n => n)
					.ToArray();
			}
			else
			{
				m_presetNames = Array.Empty<string>();
			}
			m_selectedPresetIndex = m_presetNames.Length > 0 ? 0 : -1;
		}

		private void SavePreset(string name)
		{
			// Sanitize filename
			foreach (char c in Path.GetInvalidFileNameChars())
				name = name.Replace(c, '_');

			string path = Path.Combine(GetPresetsDirectory(), name + ".json");
			string json = JsonConvert.SerializeObject(JObjectDB.GetAllData());
			File.WriteAllText(path, json);
			RefreshPresetList();
		}

		//==========================================================================
		// Diff/Compare (Feature 5)
		//==========================================================================

		private void StartDiff(string filePath)
		{
			try
			{
				string content = File.ReadAllText(filePath);
				var parsed = NJObject.Parse(content);

				// Support wrapped format
				JToken dataToken = parsed;
				if (parsed.TryGetValue("data", out var wrappedData) && wrappedData.Type == JTokenType.Object)
					dataToken = wrappedData;

				// Extract the selected collection's data from the diff file
				if (string.IsNullOrEmpty(m_selectedKey))
					return;

				var dataDict = dataToken as NJObject;
				if (dataDict == null || !dataDict.TryGetValue(m_selectedKey, out var collectionJson))
				{
					EditorUtility.DisplayDialog("Diff Error",
						$"Collection '{m_selectedKey}' not found in the comparison file.", "OK");
					return;
				}

				// Parse the collection JSON (it's stored as a string value in the dict)
				JToken baseToken;
				if (collectionJson.Type == JTokenType.String)
					baseToken = JToken.Parse(collectionJson.Value<string>());
				else
					baseToken = collectionJson;

				m_diffBaseToken = baseToken;
				m_diffEnabled = true;

				// Build diff paths
				RebuildDiffCache();
				SetStatus($"✓ Comparing with {Path.GetFileName(filePath)}");
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog("Diff Error", $"Failed to load comparison file:\n{ex.Message}", "OK");
			}
		}

		private void ClearDiff()
		{
			m_diffEnabled = false;
			m_diffBaseToken = null;
			m_diffChangedPaths.Clear();
			m_diffAddedPaths.Clear();
			m_diffRemovedPaths.Clear();
			m_diffAncestorPaths.Clear();
			SetStatus("Diff cleared");
		}

		private void RebuildDiffCache()
		{
			m_diffChangedPaths.Clear();
			m_diffAddedPaths.Clear();
			m_diffRemovedPaths.Clear();
			m_diffAncestorPaths.Clear();

			if (!m_diffEnabled || m_diffBaseToken == null || string.IsNullOrEmpty(m_selectedKey))
				return;

			if (!m_parsedData.TryGetValue(m_selectedKey, out var currentToken))
				return;

			CompareDiffTokens(currentToken, m_diffBaseToken, "");

			// Precompute ancestor paths for all diffs (reuse search's AddAncestorPaths logic)
			foreach (string p in m_diffChangedPaths) AddDiffAncestorPaths(p);
			foreach (string p in m_diffAddedPaths) AddDiffAncestorPaths(p);
			foreach (string p in m_diffRemovedPaths) AddDiffAncestorPaths(p);
		}

		private void AddDiffAncestorPaths(string path)
		{
			for (int i = path.Length - 1; i >= 0; i--)
			{
				if (path[i] == '.' || path[i] == '[')
				{
					if (!m_diffAncestorPaths.Add(path.Substring(0, i)))
						break;
				}
			}
		}

		private void CompareDiffTokens(JToken current, JToken baseline, string path)
		{
			if (current.Type == JTokenType.Object && baseline.Type == JTokenType.Object)
			{
				var currentObj = (NJObject)current;
				var baseObj = (NJObject)baseline;

				// Check current fields
				foreach (var prop in currentObj.Properties())
				{
					string fieldPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
					if (baseObj.TryGetValue(prop.Name, out var baseProp))
						CompareDiffTokens(prop.Value, baseProp, fieldPath);
					else
						m_diffAddedPaths.Add(fieldPath); // New field
				}

				// Check removed fields
				foreach (var prop in baseObj.Properties())
				{
					string fieldPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
					if (!currentObj.ContainsKey(prop.Name))
						m_diffRemovedPaths.Add(fieldPath);
				}
			}
			else if (current.Type == JTokenType.Array && baseline.Type == JTokenType.Array)
			{
				var currentArr = (NJArray)current;
				var baseArr = (NJArray)baseline;
				int maxLen = Math.Max(currentArr.Count, baseArr.Count);

				for (int i = 0; i < maxLen; i++)
				{
					string elemPath = $"{path}[{i}]";
					if (i >= currentArr.Count)
						m_diffRemovedPaths.Add(elemPath);
					else if (i >= baseArr.Count)
						m_diffAddedPaths.Add(elemPath);
					else
						CompareDiffTokens(currentArr[i], baseArr[i], elemPath);
				}
			}
			else
			{
				// Leaf comparison
				if (!JToken.DeepEquals(current, baseline))
					m_diffChangedPaths.Add(path);
			}
		}

		private bool HasDiffChildren(string path)
		{
			return m_diffAncestorPaths.Contains(path);
		}

		private static void DrawColorLegend(Color color, string label)
		{
			var rect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12));
			EditorGUI.DrawRect(rect, color);
			GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(55));
		}

		//==========================================================================
		// Import Save Data
		//==========================================================================

		/// <summary>
		/// Imports save data from a file, supporting both wrapped format (with metadata)
		/// and raw JObjectDB format. Handles Play mode safely.
		/// </summary>
		private void ImportFromFile(string filePath)
		{
			string content;
			try
			{
				content = File.ReadAllText(filePath);
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog("Import Error", $"Failed to read file:\n{ex.Message}", "OK");
				return;
			}

			if (string.IsNullOrEmpty(content))
			{
				EditorUtility.DisplayDialog("Import Error", "File is empty.", "OK");
				return;
			}

			// Detect format and extract raw data
			string jsonData;
			try
			{
				jsonData = ExtractAndConfirmImport(content, filePath);
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog("Import Error", $"Invalid JSON format:\n{ex.Message}", "OK");
				return;
			}

			if (string.IsNullOrEmpty(jsonData))
				return; // User cancelled

			// Import with Play mode safety
			if (Application.isPlaying)
			{
				if (!EditorUtility.DisplayDialog("Import Save Data",
					"Game is running.\nImport will write to PlayerPrefs and stop Play mode.\n\nNew data takes effect on next Play.",
					"Import & Stop Play", "Cancel"))
					return;

				DisableAllManagerAutoSave();
				JObjectDB.Import(jsonData);
				EditorApplication.isPlaying = false;
				SetStatus("✓ Imported & stopped Play mode. Press Play to load new data.");
			}
			else
			{
				JObjectDB.Import(jsonData);
				SetStatus($"✓ Imported from {Path.GetFileName(filePath)}");
			}

			RefreshData();
		}

		/// <summary>
		/// Parses file content, detects wrapped vs raw format, shows metadata confirmation if wrapped.
		/// Returns the raw JObjectDB JSON data string, or null if user cancels.
		/// </summary>
		private string ExtractAndConfirmImport(string content, string filePath)
		{
			var parsed = NJObject.Parse(content);

			// Detect wrapped format: has "data" field that is an object (dict)
			if (parsed.TryGetValue("data", out var dataToken) && dataToken.Type == JTokenType.Object)
			{
				string device = parsed["device"]?.ToString() ?? "Unknown";
				string os = parsed["os"]?.ToString() ?? "Unknown";
				string appVersion = parsed["appVersion"]?.ToString() ?? "Unknown";
				string exportTime = parsed["exportTime"]?.ToString() ?? "Unknown";
				int collectionCount = ((NJObject)dataToken).Count;

				string message = $"Device: {device}\n"
				                 + $"OS: {os}\n"
				                 + $"App Version: {appVersion}\n"
				                 + $"Export Time: {exportTime}\n"
				                 + $"Collections: {collectionCount}\n"
				                 + $"\nFile: {Path.GetFileName(filePath)}";

				if (!EditorUtility.DisplayDialog("Import Save Data", message, "Import", "Cancel"))
					return null;

				return dataToken.ToString(Formatting.None);
			}

			// Raw format — validate it's a Dictionary<string, string>
			// (each value should be a string or parseable object)
			return content;
		}

		/// <summary>
		/// Disables auto-save on all active JObjectDBManagerV2 instances to prevent
		/// stale in-memory data from overwriting imported PlayerPrefs data on quit.
		/// Uses reflection since this editor code doesn't reference game-specific types.
		/// </summary>
		private static void DisableAllManagerAutoSave()
		{
			// Find all MonoBehaviours that inherit from JObjectDBManagerV2<>
			var allMonoBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
			foreach (var mb in allMonoBehaviours)
			{
				var type = mb.GetType();
				while (type != null && type != typeof(MonoBehaviour))
				{
					if (type.IsGenericType && type.GetGenericTypeDefinition().Name.StartsWith("JObjectDBManagerV2"))
					{
						var method = type.GetMethod("EnableAutoSave", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
						method?.Invoke(mb, new object[] { false });
						break;
					}
					type = type.BaseType;
				}
			}
		}

		//==========================================================================
		// Utility — Timestamp
		//==========================================================================

		private static bool IsTimestampField(string fieldName)
		{
			if (string.IsNullOrEmpty(fieldName)) return false;
			string lower = fieldName.ToLowerInvariant().TrimStart('[').TrimEnd(']');
			
			// Suffix match: fields ending with common timestamp suffixes
			foreach (string suffix in TIMESTAMP_SUFFIXES)
			{
				if (lower.EndsWith(suffix))
					return true;
			}
			
			// Contains match: fields containing time-related keywords
			if (lower.Contains("time") || lower.Contains("timestamp") || lower.Contains("expired")
			    || lower.Contains("lastactive") || lower.Contains("firstactive"))
				return true;
			
			return false;
		}
	}

	/// <summary>
	/// Simple input dialog for getting a string from the user.
	/// </summary>
	internal class EditorInputDialog : EditorWindow
	{
		private string m_input = "";
		private string m_message;
		private bool m_firstFrame = true;
		private static string s_result;

		public static string Show(string title, string message, string defaultValue)
		{
			s_result = null;
			var window = CreateInstance<EditorInputDialog>();
			window.titleContent = new GUIContent(title);
			window.m_message = message;
			window.m_input = defaultValue ?? "";
			window.minSize = new Vector2(300, 100);
			window.maxSize = new Vector2(400, 100);
			window.ShowModalUtility();
			return s_result;
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField(m_message);
			GUI.SetNextControlName("InputField");
			m_input = EditorGUILayout.TextField(m_input);

			if (m_firstFrame)
			{
				EditorGUI.FocusTextInControl("InputField");
				m_firstFrame = false;
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if (GUILayout.Button("OK", GUILayout.Width(80)) ||
			    (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return))
			{
				s_result = m_input;
				Close();
			}

			if (GUILayout.Button("Cancel", GUILayout.Width(80)) ||
			    (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
			{
				Close();
			}

			EditorGUILayout.EndHorizontal();
		}
	}
}