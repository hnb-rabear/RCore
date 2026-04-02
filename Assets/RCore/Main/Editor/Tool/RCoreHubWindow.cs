using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace RCore.Editor.Tool
{
    /// <summary>
    /// Unified Smart Minimap Hub for RCore Editor Tools.
    /// Automatically loads all classes implementing `RCoreHubTool` via reflection.
    /// </summary>
    public class RCoreHubWindow : EditorWindow
    {
        private const string PREF_LAST_CATEGORY = "RCoreHub_LastCategory";
        private const float CARD_WIDTH = 280f;
        private const float CARD_HEIGHT_QUICK = -1f; // Auto-height for quick actions

        private Vector2 m_mainScroll;
        private string m_searchQuery = "";

        // Tool Data
        private List<RCoreHubTool> m_allTools = new List<RCoreHubTool>();
        private Dictionary<string, List<RCoreHubTool>> m_toolsByCategory = new Dictionary<string, List<RCoreHubTool>>();
        private List<string> m_categories = new List<string>();

        private string m_activeCategory = "";
        private RCoreHubTool m_focusedTool = null;

        [MenuItem("RCore/RCore Hub " + "_%&/", priority = RMenu.GROUP_10 + 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<RCoreHubWindow>("RCore Hub", true);
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        public static void ShowWindow(string categoryFocus)
        {
            var window = GetWindow<RCoreHubWindow>("RCore Hub", true);
            EditorPrefs.SetString(PREF_LAST_CATEGORY, categoryFocus);
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        public static void OpenAndFocusTool(string toolName)
        {
            var window = GetWindow<RCoreHubWindow>("RCore Hub", true);
            window.minSize = new Vector2(800, 600);

            if (window.m_allTools.Count == 0)
                window.LoadToolsViaReflection();

            var tool = window.m_allTools.FirstOrDefault(t => t.Name == toolName);
            if (tool != null)
            {
                window.m_activeCategory = tool.Category;
                EditorPrefs.SetString(PREF_LAST_CATEGORY, tool.Category);
                window.m_focusedTool = tool;
                window.m_searchQuery = "";
            }
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            LoadToolsViaReflection();
        }

        private void LoadToolsViaReflection()
        {
            m_allTools.Clear();
            m_toolsByCategory.Clear();
            m_categories.Clear();

            var type = typeof(RCoreHubTool);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsAbstract);

            foreach (var t in types)
            {
                try
                {
                    if (Activator.CreateInstance(t) is RCoreHubTool toolInstance)
                    {
                        toolInstance.Initialize();
                        m_allTools.Add(toolInstance);

                        var category = string.IsNullOrEmpty(toolInstance.Category) ? "Uncategorized" : toolInstance.Category;
                        if (!m_toolsByCategory.ContainsKey(category))
                        {
                            m_toolsByCategory[category] = new List<RCoreHubTool>();
                            m_categories.Add(category);
                        }
                        m_toolsByCategory[category].Add(toolInstance);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RCore Hub] Failed to instantiate tool {t.Name}: {ex.Message}");
                }
            }

            // Sort Categories
            m_categories.Sort();
            // Ensure "Navigate" comes first if it exists
            if (m_categories.Contains("Navigate"))
            {
                m_categories.Remove("Navigate");
                m_categories.Insert(0, "Navigate");
            }

            // Sort Tools inside categories
            foreach (var kvp in m_toolsByCategory)
            {
                kvp.Value.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            }

            // Restore previous state
            m_activeCategory = EditorPrefs.GetString(PREF_LAST_CATEGORY, m_categories.Count > 0 ? m_categories[0] : "");
        }

        private void OnGUI()
        {
            if (m_categories.Count == 0)
            {
                EditorGUILayout.HelpBox("No RCoreHubTools found in the project assemblies.", MessageType.Warning);
                if (GUILayout.Button("Reload")) LoadToolsViaReflection();
                return;
            }

            DrawTopBar();
            DrawTopTabs();
            DrawMainWorkspace();
        }

        private void DrawTopBar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();

            // Global Search Bar
            GUILayout.Label("Search Tools: ", EditorStyles.miniLabel, GUILayout.Width(80));
            GUI.SetNextControlName("HubGlobalSearch");
            m_searchQuery = GUILayout.TextField(m_searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(300));
            if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                m_searchQuery = "";
                GUI.FocusControl(null);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawTopTabs()
        {
            int selectedIndex = m_categories.IndexOf(m_activeCategory);
            if (selectedIndex == -1 && m_categories.Count > 0)
                selectedIndex = 0;

            GUIContent[] tabs = new GUIContent[m_categories.Count];
            for (int i = 0; i < m_categories.Count; i++)
            {
                tabs[i] = GetCategoryIcon(m_categories[i]);
            }

            GUILayout.Space(5);
            int newIndex = GUILayout.Toolbar(selectedIndex, tabs, GUILayout.Height(30));
            if (newIndex != selectedIndex && newIndex >= 0 && newIndex < m_categories.Count)
            {
                m_activeCategory = m_categories[newIndex];
                m_focusedTool = null;
                m_searchQuery = "";
                EditorPrefs.SetString(PREF_LAST_CATEGORY, m_activeCategory);
                GUI.FocusControl(null);
            }
            GUILayout.Space(5);

            Rect sepRect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(sepRect, new Color(0, 0, 0, 0.3f));
        }

        private GUIContent GetCategoryIcon(string category)
        {
            GUIContent content = null;
            switch (category)
            {
                case "Navigate": content = new GUIContent(EditorGUIUtility.IconContent("Folder Icon")); break;
                case "Search": content = new GUIContent(EditorGUIUtility.IconContent("Search Icon")); break;
                case "UI Tools": content = new GUIContent(EditorGUIUtility.IconContent("RectTransform Icon")); break;
                case "Mesh": content = new GUIContent(EditorGUIUtility.IconContent("Mesh Icon")); break;
                case "Generators": content = new GUIContent(EditorGUIUtility.IconContent("Prefab Icon")); break;
            }

            if (content != null && content.image != null)
            {
                content.tooltip = category;
                content.text = ""; // Icon-only: clear label text
                return content;
            }
            return new GUIContent(category); // Fallback to text if icon is unavailable
        }

        private void DrawMainWorkspace()
        {
            GUILayout.BeginVertical();

            // Padding top
            GUILayout.Space(10);

            m_mainScroll = GUILayout.BeginScrollView(m_mainScroll);

            if (m_focusedTool != null && string.IsNullOrEmpty(m_searchQuery))
            {
                DrawFocusedToolSpace();
            }
            else
            {
                DrawMinimapGrid();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawFocusedToolSpace()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("←", "Back to Minimap"), GUILayout.Width(35), GUILayout.Height(22)))
            {
                m_focusedTool = null;
                GUI.FocusControl(null);
                return;
            }

            GUILayout.Space(20);
            GUILayout.Label(m_focusedTool.Name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorHelper.Separator();
            GUILayout.Space(10);

            try
            {
                m_focusedTool.DrawFocusMode();
            }
            catch (Exception e)
            {
                // Keep Window alive even if a specific tool throws error
                EditorGUILayout.HelpBox($"Error rendering {m_focusedTool.Name}: {e.Message}", MessageType.Error);
            }
        }

        private void DrawMinimapGrid()
        {
            bool isSearching = !string.IsNullOrEmpty(m_searchQuery);
            string sq = m_searchQuery.ToLower();

            IEnumerable<RCoreHubTool> toolsToDisplay;

            if (isSearching)
            {
                // Show all matching tools across categories
                toolsToDisplay = m_allTools.Where(t =>
                    t.Name.ToLower().Contains(sq) ||
                    (!string.IsNullOrEmpty(t.Description) && t.Description.ToLower().Contains(sq)) ||
                    t.Category.ToLower().Contains(sq));
            }
            else
            {
                // Show only current category
                toolsToDisplay = m_toolsByCategory[m_activeCategory];
            }

            if (!toolsToDisplay.Any())
            {
                GUILayout.Label(isSearching ? "No tools match your search." : "No tools in this category.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            foreach (var tool in toolsToDisplay)
            {
                DrawToolListItem(tool);
            }
        }

        private void DrawToolListItem(RCoreHubTool tool)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.Label(tool.Name, EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(m_searchQuery))
            {
                GUI.color = Color.cyan;
                GUILayout.Label($"[{tool.Category}]", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            GUILayout.FlexibleSpace();

            if (!tool.IsQuickAction)
            {
                if (GUILayout.Button("Open Workspace", GUILayout.Width(120), GUILayout.Height(22)))
                {
                    m_focusedTool = tool;
                    m_searchQuery = "";
                    GUI.FocusControl(null);
                }
            }
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(tool.Description))
            {
                var descStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
                descStyle.normal.textColor = Color.gray;
                GUILayout.Label(tool.Description, descStyle);
            }

            if (tool.IsQuickAction)
            {
                GUILayout.Space(5);
                GUILayout.BeginVertical();
                try
                {
                    tool.DrawCard();
                }
                catch (Exception e)
                {
                    EditorGUILayout.HelpBox($"Error: {e.Message}", MessageType.Error);
                }
                GUILayout.EndVertical();
            }

            GUILayout.Space(12);

            // Subtle horizontal separator
            Rect sepRect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(sepRect, new Color(0, 0, 0, 0.2f));

            GUILayout.EndVertical();
        }
    }
}
