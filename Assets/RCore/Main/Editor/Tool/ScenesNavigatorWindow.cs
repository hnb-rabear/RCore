/**
 * Author HNB-RaBear - 2024
 **/

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RCore.Editor
{
	/// <summary>
	/// Editor window to quickly navigate and open scenes in the project.
	/// </summary>
	public class ScenesNavigatorWindow : EditorWindow
	{
		private List<string> m_scenes;
		private REditorPrefString m_search;
		private Vector2 m_scrollPosition;

		public static void ShowWindow()
		{
			GetWindow<ScenesNavigatorWindow>("Scenes Navigator");
		}

		private void OnEnable()
		{
			m_search = new REditorPrefString(typeof(ScenesNavigatorWindow).FullName);
			m_scenes = GetAllScenesInProject();
		}

		private void OnGUI()
		{
			var buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.alignment = TextAnchor.MiddleLeft;
			
			var labelStyle = new GUIStyle(GUI.skin.label);
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.fontStyle = FontStyle.Bold;
			
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
			
			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("Scenes in Build", labelStyle);
			var scenes = EditorBuildSettings.scenes;
			foreach (var scene in scenes)
			{
				string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
				if (GUILayout.Button(sceneName, buttonStyle))
					OpenScene(scene.path);
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical("box");
			GUILayout.Label("All Scenes in project", labelStyle);
			m_search.Value = EditorHelper.TextField(m_search.Value, "Search", 60);
			GUILayout.Space(5);
			for (int i = 0; i < m_scenes.Count; i++)
			{
				string scene = m_scenes[i];
				if ((m_search.Value == "" || scene.ToLower().Contains(m_search.Value.ToLower())) && GUILayout.Button($"{i}\t {scene}", buttonStyle))
					OpenScene(scene);
			}
			EditorGUILayout.EndVertical();
			
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