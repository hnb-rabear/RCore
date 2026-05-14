using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    public class ScenesNavigatorWindow : EditorWindow
    {
        private readonly EditorPrefsValue<string> m_search = new("RevCore.Tools.ScenesNavigator.Search", string.Empty);
        private List<string> m_scenes = new();
        private Vector2 m_scroll;

        [MenuItem("RevCore/Tools/Scenes Navigator", priority = 100)]
        public static void Open()
        {
            GetWindow<ScenesNavigatorWindow>("Scenes Navigator");
        }

        private void OnEnable()
        {
            m_scenes = GetAllScenesInProject();
        }

        private void OnGUI()
        {
            var buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };
            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Scenes in Build", EditorStyles.boldLabel);
            foreach (var scene in EditorBuildSettings.scenes)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scene.path);
                if (GUILayout.Button(sceneName, buttonStyle))
                    OpenScene(scene.path);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("All Scenes in Project", EditorStyles.boldLabel);
            m_search.Value = EditorGUILayout.TextField("Search", m_search.Value);
            for (int i = 0; i < m_scenes.Count; i++)
            {
                string scene = m_scenes[i];
                if (!string.IsNullOrEmpty(m_search.Value) && !scene.ToLowerInvariant().Contains(m_search.Value.ToLowerInvariant()))
                    continue;
                if (GUILayout.Button($"{i}\t {scene}", buttonStyle))
                    OpenScene(scene);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        private static void OpenScene(string scenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(scenePath);
        }

        private static List<string> GetAllScenesInProject()
        {
            var scenes = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
                scenes.Add(AssetDatabase.GUIDToAssetPath(guid));
            return scenes;
        }
    }
}
