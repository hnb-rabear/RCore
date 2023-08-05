using RCore.Common;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Debug = RCore.Common.Debug;

public class UnusedScriptFinder : EditorWindow
{
	private Object m_TargetFolder;
	private List<MonoScript> m_AllScripts;
	private List<MonoScript> m_UsedScripts;
	private Vector2 m_ScrollPosition;
	private string m_FolderPath;

	[MenuItem("RCore/Tools/Find Unused Mono Scripts")]
	public static void ShowWindow()
	{
		var window = GetWindow<UnusedScriptFinder>();
		window.titleContent = new GUIContent("Unused Scripts");
		window.Show();
	}

	private void OnGUI()
	{
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Target Folder:", GUILayout.Width(100));
		m_TargetFolder = EditorGUILayout.ObjectField(m_TargetFolder, typeof(DefaultAsset), false);
		EditorGUILayout.EndHorizontal();

		if (GUILayout.Button("Find Unused Scripts"))
			FindUnusedScripts();

		EditorGUILayout.Space();

		m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

		if (EditorHelper.HeaderFoldout("All Scripts"))
		{
			if (m_AllScripts != null)
			{
				GUILayout.Label($"All Scripts: {m_AllScripts.Count}");
				foreach (var script in m_AllScripts)
				{
					EditorGUILayout.ObjectField(script, typeof(MonoScript), false);
				}
			}
		}
		if (EditorHelper.HeaderFoldout("Used Scripts"))
		{
			if (m_UsedScripts != null)
			{
				GUILayout.Label($"Used Scripts: {m_UsedScripts.Count}");
				foreach (var script in m_UsedScripts)
				{
					EditorGUILayout.ObjectField(script, typeof(MonoScript), false);
				}
			}
		}
		if (EditorHelper.HeaderFoldout("Unused Scripts"))
		{
			if (m_UsedScripts != null && m_AllScripts != null)
			{
				var scripts = m_AllScripts.Except(m_UsedScripts).ToList();
				GUILayout.Label($"Unused Scripts: {scripts.Count}");
				foreach (var script in scripts)
				{
					EditorGUILayout.ObjectField(script, typeof(MonoScript), false);
				}
				if (GUILayout.Button("Delete Unused Scripts"))
					DeleteUnusedScripts(scripts);
			}
		}

		EditorGUILayout.EndScrollView();
	}

	private void FindUnusedScripts()
	{
		if (m_TargetFolder != null)
			m_FolderPath = AssetDatabase.GetAssetPath(m_TargetFolder);
		else m_FolderPath = "Assets";
		m_AllScripts = GetAllMonoScriptsInFolder();
		m_UsedScripts = GetUsedMonoScripts();
		
		var unusedScripts = m_AllScripts.Except(m_UsedScripts).ToList();
		foreach (var unusedScript in unusedScripts)
		{
			var unusedScriptType = unusedScript.GetClass();
			foreach (var usedScript in m_UsedScripts)
			{
				var usedScriptType = usedScript.GetClass();
				if (usedScriptType.IsSubclassOf(unusedScriptType))
				{
					if (!m_UsedScripts.Contains(unusedScript))
						m_UsedScripts.Add(unusedScript);
					break;
				}
			}
		}
		Repaint();
	}

	private List<MonoScript> GetAllMonoScriptsInFolder()
	{
		var scripts = new List<MonoScript>();
		string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { m_FolderPath });

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

	private List<MonoScript> GetUsedMonoScripts()
	{
		var usedScripts = new List<MonoScript>();

		//======================================

		// Find all objects in the project
		string[] allAssetGuids = AssetDatabase.FindAssets("t:GameObject t:ScriptableObject", new[] { m_FolderPath });
		foreach (string assetGuid in allAssetGuids)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
			var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

			if (asset != null)
			{
				if (asset is GameObject go)
				{
					var components = go.FindAllComponentsInChildren<MonoBehaviour>();
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
		string[] allScenes = GetAllScenes();
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

	private string[] GetAllScenes()
	{
		string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { m_FolderPath });
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
		if (scriptType != null && (scriptType.IsSubclassOf(typeof(Editor)) || scriptType.IsSubclassOf(typeof(EditorWindow)) || scriptType.IsSubclassOf(typeof(UIBehaviour))))
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