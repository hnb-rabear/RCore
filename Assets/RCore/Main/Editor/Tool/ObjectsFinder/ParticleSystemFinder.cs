using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Tool
{
	public class ParticleSystemFinder
	{
		private List<GameObject> m_particleSystemPrefabs;

		public void DrawOnGUI()
		{
			if (GUILayout.Button("Scan Particle Systems"))
			{
				string folderPath = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
				if (!string.IsNullOrEmpty(folderPath) && folderPath.StartsWith(Application.dataPath))
					Debug.Log($"Selected folder: {folderPath}");
				else
					Debug.LogError("The selected folder is outside the project directory or no folder was selected.");
				folderPath = EditorHelper.FormatPathToUnityPath(folderPath);
				FindParticleSystems(folderPath);
			}
			
			if (m_particleSystemPrefabs != null && m_particleSystemPrefabs.Count > 0)
			{
				if (EditorHelper.HeaderFoldout($"ParticleSystem prefabs {m_particleSystemPrefabs.Count}", "ParticleSystem prefabs"))
					foreach (var script in m_particleSystemPrefabs)
						EditorGUILayout.ObjectField(script, typeof(GameObject), false);

				if (EditorHelper.Button("Select All"))
					Selection.objects = m_particleSystemPrefabs.ToArray();
			}
		}
		
		private void FindParticleSystems(string folderPath)
		{
			m_particleSystemPrefabs = new List<GameObject>();
			var guids = AssetDatabase.FindAssets("t:GameObject", new[] { folderPath });
			foreach (string guid in guids)
			{
				var obj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
				if (obj.GetComponent<ParticleSystem>())
				{
					if (!m_particleSystemPrefabs.Contains(obj))
						m_particleSystemPrefabs.Add(obj);
					continue;
				}
				var ps = obj.gameObject.GetComponentInChildren<ParticleSystem>(true);
				if (ps != null && !m_particleSystemPrefabs.Contains(obj) && !PrefabUtility.IsPartOfPrefabInstance(ps.gameObject))
					m_particleSystemPrefabs.Add(obj);
			}
		}
	}
}