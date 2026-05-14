using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    public class FindComponentReferenceWindow : EditorWindow
    {
        private List<Type> m_types = new();
        private List<string> m_typesArray = new();
        private int m_idx;
        private string m_filter = string.Empty;
        private string m_preFilter;
        private Vector2 m_scrollPosition;

        [MenuItem("RevCore/Tools/Find Component Reference", priority = 200)]
        public static void Open()
        {
            GetWindow<FindComponentReferenceWindow>("Find Component Reference");
        }

        private void OnGUI()
        {
            m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition);

            if (m_filter != m_preFilter)
            {
                m_types = null;
                m_preFilter = m_filter;
            }

            if (m_types == null)
                GetAllTypes();

            GUILayout.Label("Filter");
            m_filter = EditorGUILayout.TextField(m_filter);
            GUILayout.Label("Select Script");
            m_idx = EditorGUILayout.Popup(m_idx, m_typesArray.ToArray());

            if (m_types.Count > 0 && GUILayout.Button("Find all prefabs"))
                ShowItemsOfTypeInProjectHierarchy(m_types[m_idx]);

            GUILayout.EndScrollView();
        }

        private void GetAllTypes()
        {
            string filter = string.IsNullOrEmpty(m_filter) ? string.Empty : m_filter.ToLower();
            m_types = new List<Type>();
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = Array.FindAll(e.Types, t => t != null);
                }

                foreach (Type type in types)
                {
                    if (type == null || !type.IsSubclassOf(typeof(MonoBehaviour)))
                        continue;
                    string scriptName = string.IsNullOrEmpty(type.Namespace) ? type.Name : $"{type.Namespace}.{type.Name}";
                    if (!string.IsNullOrEmpty(filter) && !scriptName.ToLower().Contains(filter))
                        continue;
                    m_types.Add(type);
                }
            }
            m_types.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            m_typesArray = new List<string>();
            foreach (Type type in m_types)
            {
                string scriptName = string.IsNullOrEmpty(type.Namespace) ? type.Name : $"{type.Namespace}.{type.Name}";
                m_typesArray.Add(scriptName);
            }
        }

        private static void ShowItemsOfTypeInProjectHierarchy(Type type)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            var toSelect = new List<int>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var toCheck = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var obj in toCheck)
                {
                    if (obj is not GameObject go)
                        continue;
                    if (go.GetComponent(type) != null || go.GetComponentsInChildren(type).Length > 0)
                        toSelect.Add(go.GetInstanceID());
                }
            }

            Selection.instanceIDs = Array.Empty<int>();
            ShowSelectionInProjectHierarchy();
            Selection.instanceIDs = toSelect.ToArray();
            ShowSelectionInProjectHierarchy();
        }

        private static void ShowSelectionInProjectHierarchy()
        {
            Type pbType = GetType("UnityEditor.ProjectBrowser");
            if (pbType == null)
                return;
            MethodInfo meth = pbType.GetMethod("ShowSelectedObjectsInLastInteractedProjectBrowser",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            meth?.Invoke(null, null);
        }

        private static Type GetType(string name)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = asm.GetType(name);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
