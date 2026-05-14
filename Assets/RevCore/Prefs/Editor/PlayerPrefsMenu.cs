using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
    internal static class PlayerPrefsMenu
    {
        [MenuItem("RevCore/Prefs/Clear PlayerPrefs", priority = 1400)]
        private static void ClearPlayerPrefs()
        {
            if (!EditorUtility.DisplayDialog("Confirm", "Delete all PlayerPrefs?", "Delete", "Cancel"))
                return;

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("PlayerPrefs cleared.");
        }
    }
}
