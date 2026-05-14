using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace RevCore.Hub.Editor
{
    public class RevCoreHubWindow : EditorWindow
    {
        private static readonly ToolEntry[] Tools =
        {
            new("Data", "Backup All", "RevCore/Data/Backup All"),
            new("Data", "Restore", "RevCore/Data/Restore..."),
            new("Data", "Clear All Data", "RevCore/Data/Clear All Data"),
            new("Data", "Log All Data", "RevCore/Data/Log All Data"),
            new("Audio", "Create Audio Collection", "RevCore/Audio/Create Audio Collection"),
            new("Audio", "Generate Audio IDs", "RevCore/Audio/Generate Audio IDs"),
            new("Audio", "Sort Active Collection", "RevCore/Audio/Sort Active Collection"),
            new("Prefs", "Clear PlayerPrefs", "RevCore/Prefs/Clear PlayerPrefs"),
        };

        private readonly HashSet<string> m_installedPackages = new();
        private ListRequest m_listRequest;
        private Vector2 m_scroll;

        [MenuItem("RevCore/Hub", priority = 0)]
        private static void Open()
        {
            GetWindow<RevCoreHubWindow>("RevCore Hub");
        }

        private void OnEnable()
        {
            RefreshPackages();
        }

        private void Update()
        {
            if (m_listRequest == null || !m_listRequest.IsCompleted)
                return;

            m_installedPackages.Clear();
            if (m_listRequest.Status == StatusCode.Success)
            {
                foreach (var package in m_listRequest.Result)
                    if (package.name.StartsWith("com.rabear.revcore."))
                        m_installedPackages.Add(package.name);
            }

            ScanLocalPackages();
            m_listRequest = null;
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("RevCore Hub", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
                RefreshPackages();
            EditorGUILayout.EndHorizontal();

            if (m_listRequest != null)
                EditorGUILayout.HelpBox("Scanning RevCore packages...", MessageType.Info);

            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
            DrawPackageStatus();
            EditorGUILayout.Space(8);
            DrawTools();
            EditorGUILayout.EndScrollView();
        }

        private void DrawPackageStatus()
        {
            EditorGUILayout.LabelField("Installed Packages", EditorStyles.boldLabel);
            DrawPackage("Foundation", "com.rabear.revcore.foundation");
            DrawPackage("Inspector", "com.rabear.revcore.inspector");
            DrawPackage("Timer", "com.rabear.revcore.timer");
            DrawPackage("Pool", "com.rabear.revcore.pool");
            DrawPackage("Prefs", "com.rabear.revcore.prefs");
            DrawPackage("Data", "com.rabear.revcore.data");
            DrawPackage("Audio", "com.rabear.revcore.audio");
            DrawPackage("UI", "com.rabear.revcore.ui");
        }

        private void DrawPackage(string label, string packageName)
        {
            bool installed = m_installedPackages.Contains(packageName);
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField(label);
            GUILayout.Label(installed ? "Installed" : "Not Installed", GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTools()
        {
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
            foreach (var tool in Tools)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField($"{tool.Package}/{tool.Label}");
                if (GUILayout.Button("Open", GUILayout.Width(80)))
                    Execute(tool.MenuPath);
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void Execute(string menuPath)
        {
            if (!EditorApplication.ExecuteMenuItem(menuPath))
                Debug.LogWarning($"RevCore Hub: menu item not available: {menuPath}");
        }

        private void RefreshPackages()
        {
            m_listRequest = Client.List(true, true);
            ScanLocalPackages();
        }

        private void ScanLocalPackages()
        {
            string assetsPath = Application.dataPath;
            string revCorePath = Path.Combine(assetsPath, "RevCore");
            if (!Directory.Exists(revCorePath))
                return;

            foreach (string dir in Directory.GetDirectories(revCorePath))
            {
                string packageJson = Path.Combine(dir, "package.json");
                if (!File.Exists(packageJson))
                    continue;

                string json = File.ReadAllText(packageJson);
                int nameStart = json.IndexOf("\"name\"");
                if (nameStart < 0) continue;
                int colonPos = json.IndexOf(':', nameStart);
                if (colonPos < 0) continue;
                int quoteStart = json.IndexOf('"', colonPos + 1);
                if (quoteStart < 0) continue;
                int quoteEnd = json.IndexOf('"', quoteStart + 1);
                if (quoteEnd < 0) continue;

                string packageName = json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                if (packageName.StartsWith("com.rabear.revcore."))
                    m_installedPackages.Add(packageName);
            }
        }

        private readonly struct ToolEntry
        {
            public readonly string Package;
            public readonly string Label;
            public readonly string MenuPath;

            public ToolEntry(string package, string label, string menuPath)
            {
                Package = package;
                Label = label;
                MenuPath = menuPath;
            }
        }
    }
}
