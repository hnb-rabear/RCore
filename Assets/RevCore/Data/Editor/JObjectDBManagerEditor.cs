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

            if (JObjectDB.GetCollectionKeys().Count > 0 && GUILayout.Button("Save"))
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

            // Per-collection reset: clears one collection's data back to type defaults, keeping its key registered.
            var keys = JObjectDB.GetCollectionKeys();
            if (keys.Count > 0)
            {
                GUILayout.Space(5);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Collections", EditorStyles.boldLabel);

                foreach (string key in keys)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(key);
                    if (GUILayout.Button("Reset", GUILayout.Width(60)) &&
                        EditorUtility.DisplayDialog("Reset Collection",
                            $"Reset '{key}' to default values? Its saved data will be cleared (the key stays registered).",
                            "Reset", "Cancel"))
                    {
                        JObjectDB.Reset(key);
                        GUIUtility.ExitGUI();
                    }
                    // Delete removes the key entirely; restricted to edit mode since a live model still
                    // references its data object and would re-persist it on the next save.
                    using (new EditorGUI.DisabledScope(Application.isPlaying))
                    {
                        if (GUILayout.Button("Delete", GUILayout.Width(60)) &&
                            EditorUtility.DisplayDialog("Delete Collection",
                                $"Delete '{key}' entirely? Its key and data are removed from the database.",
                                "Delete", "Cancel"))
                        {
                            JObjectDB.Delete(key);
                            GUIUtility.ExitGUI();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
        }
    }
}
