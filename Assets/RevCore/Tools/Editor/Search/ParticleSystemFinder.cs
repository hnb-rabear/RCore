using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    public class ParticleSystemFinder
    {
        private List<GameObject> m_particleSystemPrefabs;

        public void DrawOnGUI()
        {
            if (GUILayout.Button("Scan Particle Systems"))
            {
                string folderPath = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
                if (string.IsNullOrEmpty(folderPath) || !folderPath.StartsWith(Application.dataPath))
                {
                    Debug.LogError("The selected folder is outside the project directory or no folder was selected.");
                    return;
                }
                folderPath = AssetPathHelper.ToUnityPath(folderPath);
                FindParticleSystems(folderPath);
            }

            if (m_particleSystemPrefabs != null && m_particleSystemPrefabs.Count > 0)
            {
                if (EditorGuiHelper.HeaderFoldout($"ParticleSystem prefabs {m_particleSystemPrefabs.Count}", "ParticleSystem prefabs"))
                    foreach (var obj in m_particleSystemPrefabs)
                        EditorGUILayout.ObjectField(obj, typeof(GameObject), false);

                if (GUILayout.Button("Select All"))
                    Selection.objects = m_particleSystemPrefabs.ToArray();
            }
        }

        private void FindParticleSystems(string folderPath)
        {
            m_particleSystemPrefabs = new List<GameObject>();
            string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { folderPath });
            foreach (string guid in guids)
            {
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (obj.GetComponent<ParticleSystem>())
                {
                    if (!m_particleSystemPrefabs.Contains(obj))
                        m_particleSystemPrefabs.Add(obj);
                    continue;
                }
                var ps = obj.GetComponentInChildren<ParticleSystem>(true);
                if (ps != null && !m_particleSystemPrefabs.Contains(obj) && !PrefabUtility.IsPartOfPrefabInstance(ps.gameObject))
                    m_particleSystemPrefabs.Add(obj);
            }
        }
    }
}
