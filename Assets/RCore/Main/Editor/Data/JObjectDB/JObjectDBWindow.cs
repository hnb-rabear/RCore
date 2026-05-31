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
	public partial class JObjectDBWindow : EditorWindow
	{
		private enum SearchMode { Key, Value }

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
		private SearchMode m_searchMode = SearchMode.Key;
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
			PlayerPrefs.Save();
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