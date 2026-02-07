using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.AssetCleaner
{
    //[CreateAssetMenu(fileName = "RAssetCleanerSettings", menuName = "RCore/Tool/AssetCleaner Settings")]
    public class RAssetCleanerSettings : ScriptableObject
    {
        private static RAssetCleanerSettings m_instance;
        public static RAssetCleanerSettings Instance
        {
            get
            {
                if (m_instance == null)
                {
                    var guids = AssetDatabase.FindAssets("t:RAssetCleanerSettings");
                    if (guids.Length > 0)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        m_instance = AssetDatabase.LoadAssetAtPath<RAssetCleanerSettings>(path);
                    }
                    else
                    {
                        m_instance = CreateInstance<RAssetCleanerSettings>();
                        // Ideally we save this to a file, but for now we might just keep it in memory 
                        // or let the user save it. 
                        // Better to save it automatically if it doesn't exist? 
                        // For now let's assume it might not be persistent if not created manually.
                        // Actually, let's try to save it in RCore default location if possible, 
                        // but to be safe and simple for now, we'll let it be created via menu or on first access if we want to force persistence.
                        // Let's just return the instance. 
                    }
                }
                return m_instance;
            }
        }
        
        public List<string> ignorePaths = new List<string>();
        
        public List<string> deepSearchExtensions = new List<string>()
        {
            ".prefab", ".unity", ".asset", ".mat", ".controller", 
            ".overrideController", ".anim", ".json", ".txt"
        };
        
        public Color unusedColor = new Color(1f, 0.3f, 0.3f, 1f);
        public bool showSize = true;
        public bool showRedOverlay = true;
        public bool deepSearch = false;
        
        public static void Save()
        {
            if (m_instance != null)
            {
                EditorUtility.SetDirty(m_instance);
                //AssetDatabase.SaveAssets(); // Careful with auto saving
            }
        }
    }
}
