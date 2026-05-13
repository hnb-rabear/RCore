using System.Collections.Generic;
using UnityEditor;

namespace RevCore.Editor
{
    [InitializeOnLoad]
    public static class EditorPrefContainer
    {
        private static readonly List<EditorPref> s_prefs = new();

        static EditorPrefContainer()
        {
            EditorApplication.update += SaveChanges;
            EditorApplication.playModeStateChanged += _ => SaveChanges();
        }

        public static void Register(EditorPref pref)
        {
            for (int i = 0; i < s_prefs.Count; i++)
            {
                if (s_prefs[i].Key == pref.Key)
                {
                    s_prefs[i] = pref;
                    return;
                }
            }

            s_prefs.Add(pref);
        }

        public static void SaveChanges()
        {
            for (int i = 0; i < s_prefs.Count; i++)
                s_prefs[i].SaveChange();
        }

        public static void DeleteAll()
        {
            for (int i = 0; i < s_prefs.Count; i++)
                s_prefs[i].Delete();
        }

        public static void ClearRegistered()
        {
            s_prefs.Clear();
        }
    }
}
