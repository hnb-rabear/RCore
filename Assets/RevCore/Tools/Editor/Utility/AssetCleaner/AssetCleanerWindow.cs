using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RevCore.Tools.Editor
{
    public class AssetCleanerWindow : EditorWindow
    {
        private int m_tabIndex;
        private readonly string[] m_tabs = { "Cleaner", "Reference Finder", "Settings" };

        private List<string> m_unusedAssets = new();
        private Vector2 m_scrollPos;
        private bool m_scanned;

        private Object m_selectedAsset;
        private List<string> m_referencingAssets = new();
        private Vector2 m_refScrollPos;

        [MenuItem("RevCore/Tools/Asset Cleaner", priority = 400)]
        public static void Open()
        {
            GetWindow<AssetCleanerWindow>("Asset Cleaner");
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
            if (GUILayout.Button("Scan Project", GUILayout.Height(30)))
            {
                m_unusedAssets = AssetCleaner.FindUnusedAssets(AssetCleanerSettings.Instance.ignorePaths);
                m_scanned = true;
            }

            if (!m_scanned) return;

            GUILayout.Label($"Found {m_unusedAssets.Count} unused assets. Total: {AssetCleaner.GetTotalSizeFormatted(m_unusedAssets)}");
            EditorGuiHelper.Separator();

            EditorGUILayout.BeginHorizontal();
            if (EditorGuiHelper.ButtonColor("Delete All", Color.red, 100))
            {
                int count = m_unusedAssets.Count;
                string preview = string.Join("\n", m_unusedAssets.Take(20));
                if (count > 20) preview += $"\n... and {count - 20} more";

                if (EditorUtility.DisplayDialog("Delete All Unused Assets",
                    $"Delete {count} assets?\n\n{preview}", "Delete", "Cancel"))
                {
                    foreach (string path in m_unusedAssets.ToList())
                        AssetDatabase.DeleteAsset(path);
                    m_unusedAssets.Clear();
                    AssetDatabase.Refresh();
                }
            }
            EditorGUILayout.EndHorizontal();

            m_scrollPos = GUILayout.BeginScrollView(m_scrollPos);
            for (int i = 0; i < m_unusedAssets.Count; i++)
            {
                string path = m_unusedAssets[i];
                EditorGUILayout.BeginHorizontal();
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                EditorGUILayout.ObjectField(asset, typeof(Object), false);
                GUILayout.Label(EditorUtility.FormatBytes(AssetCleaner.GetAssetSize(path)), GUILayout.Width(70));
                if (EditorGuiHelper.ButtonColor("Del", Color.red, 40))
                {
                    if (EditorUtility.DisplayDialog("Delete Asset", $"Delete?\n{path}", "Delete", "Cancel"))
                    {
                        AssetDatabase.DeleteAsset(path);
                        m_unusedAssets.RemoveAt(i);
                        i--;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private void DrawReferenceFinderTab()
        {
            GUILayout.Label("Find References", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            m_selectedAsset = EditorGUILayout.ObjectField("Asset", m_selectedAsset, typeof(Object), false);
            if (GUILayout.Button("Find", GUILayout.Width(60)))
            {
                if (m_selectedAsset != null)
                {
                    if (AssetCleaner.ReferenceCache.Count == 0)
                        AssetCleaner.BuildCache();
                    string targetPath = AssetDatabase.GetAssetPath(m_selectedAsset);
                    m_referencingAssets = AssetCleaner.FindReferences(targetPath).OrderBy(x => x).ToList();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (m_selectedAsset == null) return;

            GUILayout.Label($"Used by {m_referencingAssets.Count} assets:");
            m_refScrollPos = GUILayout.BeginScrollView(m_refScrollPos);
            foreach (string refPath in m_referencingAssets)
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(refPath);
                if (asset == null) continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(asset, typeof(Object), false);
                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private void DrawSettingsTab()
        {
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            var settings = AssetCleanerSettings.Instance;

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
                settings.ignorePaths.Add("Assets/");

            if (GUI.changed)
                AssetCleanerSettings.Save();
        }
    }
}
