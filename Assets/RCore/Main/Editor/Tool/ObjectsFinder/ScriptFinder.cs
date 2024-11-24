using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace RCore.Editor.Tool
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
				if (!string.IsNullOrEmpty(folderPath) && folderPath.StartsWith(Application.dataPath))
					Debug.Log($"Selected folder: {folderPath}");
				else
					Debug.LogError("The selected folder is outside the project directory or no folder was selected.");
				folderPath = EditorHelper.FormatPathToUnityPath(folderPath);
				FindUnusedScripts(folderPath);
			}
			
			if (m_allScripts != null && m_allScripts.Count > 0)
			{
				if (EditorHelper.HeaderFoldout($"All Scripts: {m_allScripts.Count}", "All Scripts"))
					foreach (var script in m_allScripts)
						EditorGUILayout.ObjectField(script, typeof(MonoScript), false);
			}

			if (m_usedScripts != null && m_usedScripts.Count > 0)
			{
				if (EditorHelper.HeaderFoldout($"Used Scripts: {m_usedScripts.Count}", "Used Scripts"))
					foreach (var script in m_usedScripts)
						EditorGUILayout.ObjectField(script, typeof(MonoScript), false);
			}

			if (m_usedScripts != null && m_allScripts != null && m_usedScripts.Count > 0)
			{
				if (EditorHelper.HeaderFoldout($"Unused Scripts: {m_unusedScriptCount}"))
				{
					var scripts = m_allScripts.Except(m_usedScripts).ToList();
					m_unusedScriptCount = scripts.Count;
					GUILayout.Label($"Unused Scripts: {m_unusedScriptCount}");
					foreach (var script in scripts)
						EditorGUILayout.ObjectField(script, typeof(MonoScript), false);
					if (GUILayout.Button("Delete Unused Scripts"))
						DeleteUnusedScripts(scripts);
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
				foreach (var usedScript in m_usedScripts)
				{
					var usedScriptType = usedScript.GetClass();
					if (usedScriptType.IsSubclassOf(unusedScriptType))
					{
						if (!m_usedScripts.Contains(unusedScript))
							m_usedScripts.Add(unusedScript);
						break;
					}
				}
			}
			m_unusedScriptCount = m_allScripts.Except(m_usedScripts).ToArray().Length;
		}

		private List<MonoScript> FindMonoScriptsInFolder(string folderPath)
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

			scripts = scripts.OrderBy(script => script.name).ToList();
			return scripts;
		}

		private List<MonoScript> FindUsedMonoScripts(string folderPath)
		{
			var usedScripts = new List<MonoScript>();

			//======================================

			// Find all objects in the project
			string[] allAssetGuids = AssetDatabase.FindAssets("t:GameObject t:ScriptableObject", new[] { folderPath });
			foreach (string assetGuid in allAssetGuids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
				var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

				if (asset != null)
				{
					if (asset is GameObject go)
					{
						var components = go.GetComponentsInChildren<MonoBehaviour>(true);
						foreach (var component in components)
						{
							if (component == null)
								continue;
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
			}

			// Find all scenes in the project
			string[] allScenes = GetAllScenes(folderPath);
			foreach (string scenePath in allScenes)
			{
				EditorSceneManager.OpenScene(scenePath);
				var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

				foreach (var rootObject in rootObjects)
				{
					var components = rootObject.GetComponentsInChildren<Component>(true);

					foreach (var component in components)
					{
						if (component == null)
							continue;
						var script = MonoScript.FromMonoBehaviour(component as MonoBehaviour);
						if (script != null && !usedScripts.Contains(script))
							usedScripts.Add(script);
					}
				}
			}

			var sortedScripts = usedScripts.OrderBy(script => script.name).ToList();
			return sortedScripts;
		}
		
		private string[] GetAllScenes(string folderPath)
		{
			string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { folderPath });
			var scenePaths = new List<string>();

			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				scenePaths.Add(path);
			}

			return scenePaths.ToArray();
		}
		
		private static bool IsEssentialScript(MonoScript script)
		{
			var scriptType = script.GetClass();
			if (scriptType != null && (scriptType.IsSubclassOf(typeof(UnityEditor.Editor)) || scriptType.IsSubclassOf(typeof(EditorWindow)) || scriptType.IsSubclassOf(typeof(UIBehaviour))))
				return true;

			return false;
		}

		private static bool IsUnityScript(MonoScript script)
		{
			var scriptType = script.GetClass();
			if (scriptType != null && (scriptType.IsSubclassOf(typeof(MonoBehaviour)) || scriptType.IsSubclassOf(typeof(ScriptableObject))))
				return true;

			return false;
		}
		
		private void DeleteUnusedScripts(List<MonoScript> unreferencedScripts)
		{
			if (EditorUtility.DisplayDialog("Delete Unused Scripts",
				    "Are you sure you want to delete the unused scripts permanently? This operation cannot be undone.",
				    "Delete", "Cancel"))
			{
				foreach (var script in unreferencedScripts)
				{
					string scriptPath = AssetDatabase.GetAssetPath(script);
					AssetDatabase.DeleteAsset(scriptPath);
				}

				AssetDatabase.Refresh();
				Debug.Log("Unused scripts have been deleted.");
			}
		}
	}
}