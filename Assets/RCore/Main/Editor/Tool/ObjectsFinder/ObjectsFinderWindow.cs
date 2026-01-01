using UnityEngine;
using UnityEditor;

namespace RCore.Editor.Tool
{
	/// <summary>
	/// Editor window to find objects in the project, such as scripts, particle systems, and persistent events.
	/// </summary>
	public class ObjectsFinderWindow : EditorWindow
	{
		private Vector2 m_scrollPosition;
		private ScriptFinder m_scriptFinder = new ScriptFinder();
		private ParticleSystemFinder m_particleSystemFinder = new ParticleSystemFinder();
		private PersistentEventFinder m_persistentEventFinder = new PersistentEventFinder();

		public static void ShowWindow()
		{
			var window = GetWindow<ObjectsFinderWindow>();
			window.titleContent = new GUIContent("Objects Finder");
			window.Show();
		}

		private void OnGUI()
		{
			EditorGUILayout.Space();

			m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

			EditorGUILayout.BeginVertical("box");
			m_scriptFinder.DrawOnGUI();
			EditorGUILayout.EndVertical();
			
			EditorHelper.Separator();
			
			EditorGUILayout.BeginVertical("box");
			m_particleSystemFinder.DrawOnGUI();
			EditorGUILayout.EndVertical();
			
			EditorHelper.Separator();
			
			EditorGUILayout.BeginVertical("box");
			m_persistentEventFinder.DrawOnGUI();
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndScrollView();
		}
	}
}