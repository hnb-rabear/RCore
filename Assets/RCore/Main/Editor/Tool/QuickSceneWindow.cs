/**
 * Author HNB-RaBear - 2024
 **/

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RCore.Editor
{
	public class QuickSceneWindow : EditorWindow
	{
		private List<string> m_scenes;
		private EditorPrefsString m_search;
		private Vector2 m_scrollPosition;

		public static void ShowWindow()
		{
			GetWindow<QuickSceneWindow>("Quick Scene Opener");
		}

		private void OnEnable()
		{
			m_search = new EditorPrefsString("QuickSceneWindowSearch");
			m_scenes = GetAllScenesInProject();
		}

		private void OnGUI()
		{
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
			GUILayout.Label("Scenes in Build", EditorStyles.boldLabel);

			var scenes = EditorBuildSettings.scenes;
			foreach (var scene in scenes)
			{
				string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
				if (GUILayout.Button(sceneName))
					OpenScene(scene.path);
			}

			GUILayout.Label("All Scenes in project", EditorStyles.boldLabel);
			m_search.Value = EditorHelper.TextField(m_search.Value, "Search");
			foreach (var scene in m_scenes)
			{
				if ((m_search.Value == "" || scene.ToLower().Contains(m_search.Value.ToLower())) && GUILayout.Button(scene))
					OpenScene(scene);
			}
			GUILayout.EndScrollView();
		}

		private void OpenScene(string scenePath)
		{
			if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
				EditorSceneManager.OpenScene(scenePath);
		}

		private List<string> GetAllScenesInProject()
		{
			var scenes = new List<string>();
			string[] guids = AssetDatabase.FindAssets("t:Scene");

			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				scenes.Add(path);
			}

			return scenes;
		}
	}
}