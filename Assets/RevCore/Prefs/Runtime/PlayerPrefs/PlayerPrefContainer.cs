using System.Collections.Generic;

namespace RevCore
{
    /// <summary>
    /// Registry of all <see cref="PlayerPref"/> instances. Lets you flush every cached prefs
    /// entry to disk with a single <see cref="SaveChanges"/> call (typically wired to
    /// <see cref="UnityEngine.Application.quitting"/>) and to bulk-delete on factory-reset.
    /// </summary>
    public static class PlayerPrefContainer
    {
        private static readonly List<PlayerPref> s_prefs = new();

        /// <summary>
        /// Registers <paramref name="pref"/>. If an entry with the same key is already registered it
        /// is replaced (last-registered wins). Called from <see cref="PlayerPref"/>'s constructor — most
        /// consumers do not call this directly.
        /// </summary>
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

        /// <summary>Calls <see cref="PlayerPref.SaveChange"/> on every registered entry.</summary>
        public static void SaveChanges()
        {
            for (int i = 0; i < s_prefs.Count; i++)
                s_prefs[i].SaveChange();
        }

        /// <summary>Calls <see cref="IPref.Delete"/> on every registered entry.</summary>
        public static void DeleteAll()
        {
            for (int i = 0; i < s_prefs.Count; i++)
                s_prefs[i].Delete();
        }

        /// <summary>
        /// Clears the registration list. Does NOT delete or save anything — use when shutting down
        /// a test fixture to avoid leaking references across cases.
        /// </summary>
        public static void ClearRegistered()
        {
            s_prefs.Clear();
        }
    }
}
