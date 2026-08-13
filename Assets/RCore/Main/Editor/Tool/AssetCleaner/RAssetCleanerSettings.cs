using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.AssetCleaner
{
    //[CreateAssetMenu(fileName = "RAssetCleanerSettings", menuName = "RCore/Tool/AssetCleaner Settings")]
    public class RAssetCleanerSettings : ScriptableObject
    {
        private const string EDITOR_PREFS_KEY = "RCore.AssetCleaner.Settings";

        private static RAssetCleanerSettings m_instance;
        private static bool m_instanceIsAsset;

        public static RAssetCleanerSettings Instance
        {
            get
            {
                if (m_instance == null)
                {
                    if (TryLoadAsset(out var asset))
                    {
                        m_instance = asset;
                        m_instanceIsAsset = true;
                    }
                    else
                    {
                        // No settings asset in the project: keep the values local to this machine
                        // instead of losing them on the next domain reload.
                        m_instance = CreateInstance<RAssetCleanerSettings>();
                        m_instance.hideFlags = HideFlags.HideAndDontSave;
                        m_instanceIsAsset = false;
                        LoadFromEditorPrefs(m_instance);
                    }
                }
                return m_instance;
            }
        }

        private static bool TryLoadAsset(out RAssetCleanerSettings settings)
        {
            settings = null;
            var guids = AssetDatabase.FindAssets("t:RAssetCleanerSettings");
            if (guids.Length == 0)
                return false;

            settings = AssetDatabase.LoadAssetAtPath<RAssetCleanerSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
            return settings != null;
        }
        
        public List<string> ignorePaths = new List<string>();
        
        public List<string> deepSearchExtensions = new List<string>()
        {
            ".prefab", ".unity", ".asset", ".mat", ".controller", 
            ".overrideController", ".anim", ".json", ".txt"
        };
        
        public List<string> leakIgnoreExtensions = new List<string>()
        {
            ".cs", ".asmdef", ".dll"
        };

        public Color unusedColor = new Color(1f, 0.3f, 0.3f, 1f);
        public bool showSize = true;
        public bool showReferenceCount = true;
        public bool showRedOverlay = true;
        public bool deepSearch = false;
        public bool scanFirstComponentAssetReference = false;

        public static void Save()
        {
            if (m_instance == null)
                return;

            if (m_instanceIsAsset)
            {
                EditorUtility.SetDirty(m_instance);
                AssetDatabase.SaveAssetIfDirty(m_instance);
                return;
            }

            EditorPrefs.SetString(EDITOR_PREFS_KEY, JsonUtility.ToJson(m_instance));
        }

        private static void LoadFromEditorPrefs(RAssetCleanerSettings settings)
        {
            if (!EditorPrefs.HasKey(EDITOR_PREFS_KEY))
                return;

            try
            {
                JsonUtility.FromJsonOverwrite(EditorPrefs.GetString(EDITOR_PREFS_KEY), settings);
            }
            catch
            {
                // Invalid local preference data must not block Asset Cleaner from using defaults.
            }
        }
    }
}
