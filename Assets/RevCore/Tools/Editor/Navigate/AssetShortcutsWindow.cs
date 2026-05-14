using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RevCore.Tools.Editor
{
    public class AssetShortcutsWindow : EditorWindow
    {
        [Serializable]
        private class Category
        {
            public string name;
            public List<string> guids = new();
        }

        [Serializable]
        private class ShortcutData
        {
            public List<Category> categories = new();
        }

        private const string FilePath = "ProjectSettings/RevCore/AssetShortcuts.json";
        private ShortcutData m_data = new();
        private Vector2 m_scroll;
        private string m_newCategoryName = string.Empty;
        private int m_editingCategory = -1;

        [MenuItem("RevCore/Tools/Asset Shortcuts", priority = 101)]
        public static void Open()
        {
            GetWindow<AssetShortcutsWindow>("Asset Shortcuts");
        }

        private void OnEnable()
        {
            LoadData();
        }

        private void OnDisable()
        {
            SaveData();
        }

        private void LoadData()
        {
            if (File.Exists(FilePath))
                m_data = JsonUtility.FromJson<ShortcutData>(File.ReadAllText(FilePath));
            m_data ??= new ShortcutData();
            m_data.categories ??= new List<Category>();
        }

        private void SaveData()
        {
            AssetPathHelper.EnsureDirectory(FilePath);
            File.WriteAllText(FilePath, JsonUtility.ToJson(m_data, true));
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Categories", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            m_newCategoryName = EditorGUILayout.TextField(m_newCategoryName);
            if (GUILayout.Button("Add Category", GUILayout.Width(110)) && !string.IsNullOrEmpty(m_newCategoryName))
            {
                m_data.categories.Add(new Category { name = m_newCategoryName });
                m_newCategoryName = string.Empty;
                SaveData();
            }
            EditorGUILayout.EndHorizontal();

            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
            for (int i = 0; i < m_data.categories.Count; i++)
                DrawCategory(i);
            EditorGUILayout.EndScrollView();
        }

        private void DrawCategory(int index)
        {
            Category category = m_data.categories[index];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            if (m_editingCategory == index)
                category.name = EditorGUILayout.TextField(category.name);
            else
                EditorGUILayout.LabelField(category.name, EditorStyles.boldLabel);

            if (GUILayout.Button(m_editingCategory == index ? "Done" : "Edit", GUILayout.Width(60)))
            {
                m_editingCategory = m_editingCategory == index ? -1 : index;
                SaveData();
            }
            if (EditorGuiHelper.ButtonColor("Delete", Color.red, 70))
            {
                m_data.categories.RemoveAt(index);
                SaveData();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUIUtility.ExitGUI();
                return;
            }
            EditorGUILayout.EndHorizontal();

            DrawDropArea(category);
            DrawAssets(category);
            EditorGUILayout.EndVertical();
        }

        private void DrawDropArea(Category category)
        {
            Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop Assets Here");
            Event evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition))
                return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (Object obj in DragAndDrop.objectReferences)
                {
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
                    if (!string.IsNullOrEmpty(guid) && !category.guids.Contains(guid))
                        category.guids.Add(guid);
                }
                SaveData();
                evt.Use();
            }
        }

        private void DrawAssets(Category category)
        {
            for (int i = 0; i < category.guids.Count; i++)
            {
                string guid = category.guids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset == null)
                    continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(asset, typeof(Object), false);
                if (asset is SceneAsset)
                {
                    if (GUILayout.Button("Open", GUILayout.Width(50)) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        EditorSceneManager.OpenScene(path);
                }
                else if (PrefabUtility.GetPrefabAssetType(asset) == PrefabAssetType.Regular)
                {
                    if (GUILayout.Button("Open", GUILayout.Width(50)))
                        AssetDatabase.OpenAsset(asset);
                }

                if (EditorGuiHelper.ButtonColor("Delete", Color.red, 70))
                {
                    category.guids.RemoveAt(i);
                    SaveData();
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
