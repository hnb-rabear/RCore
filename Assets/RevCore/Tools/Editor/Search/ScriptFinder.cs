using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace RevCore.Tools.Editor
{
    public class ScriptFinder
    {
        private List<MonoScript> m_allScripts;
        private List<MonoScript> m_usedScripts;
        private int m_unusedScriptCount;

        public void DrawOnGUI()
        {
            if (GUILayout.Button("Scan Scripts"))
            {
                string folderPath = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
                if (string.IsNullOrEmpty(folderPath) || !folderPath.StartsWith(Application.dataPath))
                {
                    Debug.LogError("The selected folder is outside the project directory or no folder was selected.");
                    return;
                }
                folderPath = AssetPathHelper.ToUnityPath(folderPath);
                FindUnusedScripts(folderPath);
            }

            if (m_allScripts != null && m_allScripts.Count > 0)
            {
                if (EditorGuiHelper.HeaderFoldout($"All Scripts: {m_allScripts.Count}", "All Scripts"))
                    foreach (var script in m_allScripts)
                        EditorGUILayout.ObjectField(script, typeof(MonoScript), false);
            }

            if (m_usedScripts != null && m_usedScripts.Count > 0)
            {
                if (EditorGuiHelper.HeaderFoldout($"Used Scripts: {m_usedScripts.Count}", "Used Scripts"))
                    foreach (var script in m_usedScripts)
                        EditorGUILayout.ObjectField(script, typeof(MonoScript), false);
            }

            if (m_usedScripts != null && m_allScripts != null && m_usedScripts.Count > 0)
            {
                if (EditorGuiHelper.HeaderFoldout($"Unused Scripts: {m_unusedScriptCount}"))
                {
                    var scripts = m_allScripts.Except(m_usedScripts).ToList();
                    m_unusedScriptCount = scripts.Count;
                    GUILayout.Label($"Unused Scripts: {m_unusedScriptCount}");
                    foreach (var script in scripts)
                        EditorGUILayout.ObjectField(script, typeof(MonoScript), false);
                }
            }
        }

        private void FindUnusedScripts(string folderPath)
        {
            m_allScripts = FindMonoScriptsInFolder(folderPath);
            m_usedScripts = FindUsedMonoScripts(folderPath);

            var unusedScripts = m_allScripts.Except(m_usedScripts).ToList();
            foreach (var unusedScript in unusedScripts)
            {
                var unusedScriptType = unusedScript.GetClass();
                if (unusedScriptType == null) continue;
                foreach (var usedScript in m_usedScripts)
                {
                    var usedScriptType = usedScript.GetClass();
                    if (usedScriptType != null && usedScriptType.IsSubclassOf(unusedScriptType))
                    {
                        if (!m_usedScripts.Contains(unusedScript))
                            m_usedScripts.Add(unusedScript);
                        break;
                    }
                }
            }
            m_unusedScriptCount = m_allScripts.Except(m_usedScripts).Count();
        }

        private static List<MonoScript> FindMonoScriptsInFolder(string folderPath)
        {
            var scripts = new List<MonoScript>();
            string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && IsUnityScript(script) && !IsEssentialScript(script))
                    scripts.Add(script);
            }
            return scripts.OrderBy(s => s.name).ToList();
        }

        private static List<MonoScript> FindUsedMonoScripts(string folderPath)
        {
            var usedScripts = new List<MonoScript>();

            string[] allAssetGuids = AssetDatabase.FindAssets("t:GameObject t:ScriptableObject", new[] { folderPath });
            foreach (string assetGuid in allAssetGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (asset == null) continue;

                if (asset is GameObject go)
                {
                    foreach (var component in go.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (component == null) continue;
                        var script = MonoScript.FromMonoBehaviour(component);
                        if (script != null && !usedScripts.Contains(script))
                            usedScripts.Add(script);
                    }
                }
                else if (asset is ScriptableObject so)
                {
                    var script = MonoScript.FromScriptableObject(so);
                    if (script != null && !usedScripts.Contains(script))
                        usedScripts.Add(script);
                }
            }

            string[] allScenes = GetAllScenes(folderPath);
            foreach (string scenePath in allScenes)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    continue;
                EditorSceneManager.OpenScene(scenePath);
                foreach (var rootObject in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    foreach (var component in rootObject.GetComponentsInChildren<Component>(true))
                    {
                        if (component == null) continue;
                        var script = MonoScript.FromMonoBehaviour(component as MonoBehaviour);
                        if (script != null && !usedScripts.Contains(script))
                            usedScripts.Add(script);
                    }
                }
            }

            return usedScripts.OrderBy(s => s.name).ToList();
        }

        private static string[] GetAllScenes(string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { folderPath });
            var scenePaths = new List<string>();
            foreach (string guid in guids)
                scenePaths.Add(AssetDatabase.GUIDToAssetPath(guid));
            return scenePaths.ToArray();
        }

        private static bool IsEssentialScript(MonoScript script)
        {
            var scriptType = script.GetClass();
            return scriptType != null && (scriptType.IsSubclassOf(typeof(UnityEditor.Editor)) || scriptType.IsSubclassOf(typeof(EditorWindow)) || scriptType.IsSubclassOf(typeof(UIBehaviour)));
        }

        private static bool IsUnityScript(MonoScript script)
        {
            var scriptType = script.GetClass();
            return scriptType != null && (scriptType.IsSubclassOf(typeof(MonoBehaviour)) || scriptType.IsSubclassOf(typeof(ScriptableObject)));
        }
    }
}
