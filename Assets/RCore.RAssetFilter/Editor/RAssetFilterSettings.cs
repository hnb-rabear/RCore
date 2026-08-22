using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.RAssetFilter.Editor
{
    //[CreateAssetMenu(fileName = "RAssetFilterSettings", menuName = "RCore/Tool/AssetCleaner Settings")]
    public class RAssetFilterSettings : ScriptableObject
    {
        private const string EDITOR_PREFS_KEY = "RCore.RAssetFilter.Settings";
        private const string LEGACY_EDITOR_PREFS_KEY = "RCore.AssetCleaner.Settings";

        private static RAssetFilterSettings m_instance;
        private static bool m_instanceIsAsset;

        public static RAssetFilterSettings Instance
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
                        m_instance = CreateInstance<RAssetFilterSettings>();
                        m_instance.hideFlags = HideFlags.HideAndDontSave;
                        m_instanceIsAsset = false;
                        LoadFromEditorPrefs(m_instance);
                    }
                }
                return m_instance;
            }
        }

        private static bool TryLoadAsset(out RAssetFilterSettings settings)
        {
            settings = null;
            var guids = AssetDatabase.FindAssets("t:RAssetFilterSettings");
            if (guids.Length == 0)
                return false;

            settings = AssetDatabase.LoadAssetAtPath<RAssetFilterSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
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

        private static void LoadFromEditorPrefs(RAssetFilterSettings settings)
        {
            string key;
            if (EditorPrefs.HasKey(EDITOR_PREFS_KEY))
                key = EDITOR_PREFS_KEY;
            else if (EditorPrefs.HasKey(LEGACY_EDITOR_PREFS_KEY))
                key = LEGACY_EDITOR_PREFS_KEY;
            else
                return;

            try
            {
                JsonUtility.FromJsonOverwrite(EditorPrefs.GetString(key), settings);
                if (key == LEGACY_EDITOR_PREFS_KEY)
                    EditorPrefs.SetString(EDITOR_PREFS_KEY, JsonUtility.ToJson(settings));
            }
            catch
            {
                // Invalid local preference data must not block RAsset Filter from using defaults.
            }
        }
    }
}
