using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    public class RevCoreToolsWindow : EditorWindow
    {
        private readonly List<RevCoreTool> m_tools = new();
        private Vector2 m_scroll;
        private string m_search = string.Empty;
        private string m_category = "All";

        [MenuItem("RevCore/Tools Hub", priority = 0)]
        public static void Open()
        {
            GetWindow<RevCoreToolsWindow>("RevCore Tools");
        }

        private void OnEnable()
        {
            RefreshTools();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawCategories();

            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
            foreach (RevCoreTool tool in FilteredTools())
                DrawTool(tool);
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("RevCore Tools", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            m_search = GUILayout.TextField(m_search, GUI.skin.FindStyle("ToolbarSearchTextField"), GUILayout.Width(220));
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                RefreshTools();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCategories()
        {
            string[] categories = new[] { "All" }.Concat(m_tools.Select(x => x.Category).Distinct().OrderBy(x => x)).ToArray();
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            foreach (string category in categories)
            {
                bool selected = category == m_category;
                if (GUILayout.Toggle(selected, category, EditorStyles.toolbarButton))
                    m_category = category;
            }
            EditorGUILayout.EndHorizontal();
        }

        private IEnumerable<RevCoreTool> FilteredTools()
        {
            foreach (RevCoreTool tool in m_tools.OrderBy(x => x.Category).ThenBy(x => x.Name))
            {
                if (m_category != "All" && tool.Category != m_category)
                    continue;
                if (!string.IsNullOrWhiteSpace(m_search) && !tool.Name.ToLowerInvariant().Contains(m_search.ToLowerInvariant()))
                    continue;
                yield return tool;
            }
        }

        private static void DrawTool(RevCoreTool tool)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{tool.Category} / {tool.Name}", EditorStyles.boldLabel);
            if (!tool.IsQuickAction && GUILayout.Button("Open", GUILayout.Width(80)))
                tool.OnOpen();
            EditorGUILayout.EndHorizontal();

            if (tool.IsQuickAction)
                tool.OnGUI();

            EditorGUILayout.EndVertical();
        }

        private void RefreshTools()
        {
            m_tools.Clear();
            Type baseType = typeof(RevCoreTool);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(x => x != null).ToArray();
                }

                foreach (Type type in types)
                {
                    if (type == null || type.IsAbstract || !baseType.IsAssignableFrom(type))
                        continue;
                    if (Activator.CreateInstance(type) is RevCoreTool tool)
                        m_tools.Add(tool);
                }
            }
        }
    }
}
