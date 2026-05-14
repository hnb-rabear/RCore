#if ADDRESSABLES
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    [InitializeOnLoad]
    public class AddressableGroupsColorizer
    {
        private static Dictionary<string, Color> s_colors;
        private static Dictionary<string, Color> s_directories;
        private static HashSet<string> s_ignoreGuids;
        private static Dictionary<string, string> s_pathCache;
        private static AddressableGroupsColorizerSettings s_settings;

        static AddressableGroupsColorizer()
        {
            Init();
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
            if (AddressableAssetSettingsDefaultObject.Settings != null)
                AddressableAssetSettingsDefaultObject.Settings.OnModification += OnSettingsModification;
        }

        [MenuItem("RevCore/Tools/Addressable Groups Colorizer Settings", priority = 500)]
        public static void OpenSettings()
        {
            LoadSettings();
            if (s_settings != null)
            {
                Selection.activeObject = s_settings;
                EditorGUIUtility.PingObject(s_settings);
            }
            else
            {
                if (EditorUtility.DisplayDialog("Settings not found",
                    "AddressableGroupsColorizerSettings asset not found.\n\nCreate one now?", "Create", "Cancel"))
                {
                    string path = EditorUtility.SaveFilePanelInProject("Save Settings",
                        "AddressableGroupsColorizerSettings", "asset", "Save settings");
                    if (!string.IsNullOrEmpty(path))
                    {
                        s_settings = ScriptableObject.CreateInstance<AddressableGroupsColorizerSettings>();
                        AssetDatabase.CreateAsset(s_settings, path);
                        AssetDatabase.SaveAssets();
                        Selection.activeObject = s_settings;
                        EditorGUIUtility.PingObject(s_settings);
                    }
                }
            }
        }

        private static void Init()
        {
            s_ignoreGuids = new HashSet<string>();
            s_colors = new Dictionary<string, Color>();
            s_directories = new Dictionary<string, Color>();
            s_pathCache = new Dictionary<string, string>();
            LoadSettings();
        }

        private static void LoadSettings()
        {
            s_settings = Resources.Load<AddressableGroupsColorizerSettings>(nameof(AddressableGroupsColorizerSettings));
            if (s_settings == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:" + nameof(AddressableGroupsColorizerSettings));
                if (guids.Length > 0)
                    s_settings = AssetDatabase.LoadAssetAtPath<AddressableGroupsColorizerSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        private static void OnSettingsModification(AddressableAssetSettings settings, AddressableAssetSettings.ModificationEvent evt, object data)
        {
            if (evt == AddressableAssetSettings.ModificationEvent.EntryAdded
                || evt == AddressableAssetSettings.ModificationEvent.EntryMoved
                || evt == AddressableAssetSettings.ModificationEvent.EntryCreated
                || evt == AddressableAssetSettings.ModificationEvent.EntryModified
                || evt == AddressableAssetSettings.ModificationEvent.EntryRemoved)
                Init();
        }

        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            if (s_settings == null || !s_settings.enabled || string.IsNullOrEmpty(guid) || Event.current.type != EventType.Repaint || s_ignoreGuids.Contains(guid))
                return;
            DrawColorMark(guid, selectionRect);
        }

        private static string GetAssetPath(string guid)
        {
            if (!s_pathCache.TryGetValue(guid, out string path))
            {
                path = AssetDatabase.GUIDToAssetPath(guid);
                s_pathCache[guid] = path;
            }
            return path;
        }

        private static void DrawColorMark(string guid, Rect selectionRect)
        {
            if (!s_colors.TryGetValue(guid, out Color color))
            {
                string path = GetAssetPath(guid);
                if (path.EndsWith(".cs"))
                {
                    s_ignoreGuids.Add(guid);
                    return;
                }

                var aaSettings = AddressableAssetSettingsDefaultObject.Settings;
                if (aaSettings == null) return;

                var entry = aaSettings.FindAssetEntry(guid, true);
                if (entry != null && entry.parentGroup != null)
                {
                    string groupName = entry.parentGroup.name;
                    bool found = false;

                    if (s_settings?.rules != null)
                    {
                        foreach (var rule in s_settings.rules)
                        {
                            if (groupName.StartsWith(rule.prefix))
                            {
                                color = rule.color;
                                found = true;
                                break;
                            }
                        }
                    }

                    if (!found)
                        color = Color.yellow;

                    s_colors.Add(guid, color);
                    if (AssetDatabase.IsValidFolder(path))
                        s_directories.Add(path, color);
                }
                else
                {
                    bool isSubAsset = false;
                    foreach (var dir in s_directories)
                    {
                        if (path.StartsWith(dir.Key))
                        {
                            isSubAsset = true;
                            color = dir.Value;
                            s_colors.Add(guid, color);
                            break;
                        }
                    }
                    if (!isSubAsset)
                        s_ignoreGuids.Add(guid);
                }
            }
            else
            {
                Color oldColor = GUI.color;
                GUI.color = color;
                GUI.Box(selectionRect, "");
                GUI.color = oldColor;
            }
        }
    }
}
#endif
