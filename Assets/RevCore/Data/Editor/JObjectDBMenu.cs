using System;
using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
    internal static class JObjectDBMenu
    {
        private const int Priority = 1500;

        [MenuItem("RevCore/Data/Backup All", priority = Priority)]
        private static void BackupAll()
        {
            var t = DateTime.Now;
            string fileName = $"GameData_{t.Year % 100}{t.Month:00}{t.Day:00}_{t.Hour:00}h{t.Minute:00}";
            string path = EditorUtility.SaveFilePanel("Backup Data", null, fileName, "json");
            if (!string.IsNullOrEmpty(path))
                JObjectDB.Backup(path);
        }

        [MenuItem("RevCore/Data/Restore...", priority = Priority + 1)]
        private static void Restore()
        {
            string path = EditorUtility.OpenFilePanel("Select Data File", Application.dataPath, "json,txt");
            if (!string.IsNullOrEmpty(path))
                JObjectDB.Restore(path);
        }

        [MenuItem("RevCore/Data/Clear All Data", priority = Priority + 2)]
        private static void ClearAllData()
        {
            if (EditorUtility.DisplayDialog("Confirm", "Delete all JObjectDB data?", "Delete", "Cancel"))
                JObjectDB.DeleteAll();
        }

        [MenuItem("RevCore/Data/Log All Data", priority = Priority + 3)]
        private static void LogAllData()
        {
            JObjectDB.CopyAllData();
        }
    }
}
