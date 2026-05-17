using System;
using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
    [CustomEditor(typeof(JObjectModelCollection), true)]
    public class JObjectModelCollectionEditor : UnityEditor.Editor
    {
        private JObjectModelCollection m_collection;

        private void OnEnable()
        {
            m_collection = target as JObjectModelCollection;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load")) m_collection.Load();
            if (GUILayout.Button("Save")) m_collection.Save();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("JObject DB", EditorStyles.boldLabel);

            if (JObjectDB.GetCollectionKeys().Count > 0 && GUILayout.Button("Save All Collections"))
                JObjectDB.Save();

            if (GUILayout.Button("Backup All Data..."))
            {
                var t = DateTime.Now;
                string fileName = $"GameData_{t:yyMMdd_HHmm}";
                string path = EditorUtility.SaveFilePanel("Backup Data",
                    Application.dataPath.Replace("Assets", "Saves"), fileName, "json");
                if (!string.IsNullOrEmpty(path))
                    JObjectDB.Backup(path);
            }

            if (GUILayout.Button("Copy All Data to Clipboard"))
                JObjectDB.CopyAllData();

            if (!Application.isPlaying)
            {
                if (GUILayout.Button("Delete All Data") &&
                    EditorUtility.DisplayDialog("Confirm", "Delete all data in PlayerPrefs managed by JObjectDB?", "Delete All", "Cancel"))
                    JObjectDB.DeleteAll();

                if (GUILayout.Button("Restore Data from File..."))
                {
                    string filePath = EditorUtility.OpenFilePanel("Select Data File",
                        Application.dataPath.Replace("Assets", "Saves"), "json,txt");
                    if (!string.IsNullOrEmpty(filePath))
                        JObjectDB.Restore(filePath);
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
