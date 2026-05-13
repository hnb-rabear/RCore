using System;
using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
    [CustomEditor(typeof(JObjectDBManager<>), true)]
    public class JObjectDBManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty m_dataCollection;

        private void OnEnable()
        {
            m_dataCollection = serializedObject.FindProperty("m_dataCollection");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(5);

            if (m_dataCollection?.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Assign a JObjectModelCollection ScriptableObject to m_dataCollection.", MessageType.Error);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("JObject DB", EditorStyles.boldLabel);

            if (JObjectDB.collections.Count > 0 && GUILayout.Button("Save"))
                JObjectDB.Save();

            if (GUILayout.Button("Backup"))
            {
                var t = DateTime.Now;
                string fileName = $"GameData_{t.Year % 100}{t.Month:00}{t.Day:00}_{t.Hour:00}h{t.Minute:00}";
                string path = EditorUtility.SaveFilePanel("Backup Data", null, fileName, "json");
                if (!string.IsNullOrEmpty(path))
                    JObjectDB.Backup(path);
            }

            if (GUILayout.Button("Copy All"))
                JObjectDB.CopyAllData();

            if (!Application.isPlaying)
            {
                if (GUILayout.Button("Delete All") &&
                    EditorUtility.DisplayDialog("Confirm", "Delete All Data", "Delete", "Cancel"))
                    JObjectDB.DeleteAll();

                if (GUILayout.Button("Restore"))
                {
                    string filePath = EditorUtility.OpenFilePanel("Select Data File",
                        Application.dataPath, "json,txt");
                    if (!string.IsNullOrEmpty(filePath))
                        JObjectDB.Restore(filePath);
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
