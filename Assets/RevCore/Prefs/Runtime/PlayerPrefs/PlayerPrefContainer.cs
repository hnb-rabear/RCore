using System.Collections.Generic;

namespace RevCore
{
    public static class PlayerPrefContainer
    {
        private static readonly List<PlayerPref> s_prefs = new();

        public static void Register(PlayerPref pref)
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
