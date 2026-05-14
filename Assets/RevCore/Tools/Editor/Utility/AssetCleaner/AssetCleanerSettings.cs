using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    public class AssetCleanerSettings : ScriptableObject
    {
        private static AssetCleanerSettings s_instance;

        public static AssetCleanerSettings Instance
        {
            get
            {
                if (s_instance == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:AssetCleanerSettings");
                    if (guids.Length > 0)
                        s_instance = AssetDatabase.LoadAssetAtPath<AssetCleanerSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    if (s_instance == null)
                        s_instance = CreateInstance<AssetCleanerSettings>();
                }
                return s_instance;
            }
        }

        public List<string> ignorePaths = new() { "Assets/RevCore" };

        public static void Save()
        {
            if (s_instance != null)
                EditorUtility.SetDirty(s_instance);
        }
    }
}
