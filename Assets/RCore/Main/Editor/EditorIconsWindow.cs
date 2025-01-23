using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	public class EditorIconsWindow : EditorWindow
	{
		private Vector2 m_scrollPosition;

		private void OnGUI()
		{
			m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
			if (GUILayout.Button("View Full Builtin Icons"))
				Application.OpenURL("https://github.com/halak/unity-editor-icons/tree/master");
			int index = 0;
			foreach (var item in EditorIcon.IconDictionary)
			{
				if (index % 5 == 0)
					GUILayout.BeginHorizontal();
		
				var icon = EditorIcon.GetIcon(item.Key);
				GUILayout.BeginHorizontal("box");
				GUILayout.Label(item.Key.ToString(), GUILayout.Width(120));
				GUILayout.Label(icon, GUILayout.Width(23), GUILayout.Height(23));
				GUILayout.EndHorizontal();
				GUILayout.Space(2);
		
				if (index % 5 == 4)
					GUILayout.EndHorizontal();
		
				index++;
			}
		
			if (index % 5 != 0)
				GUILayout.EndHorizontal();
		
			EditorGUILayout.EndScrollView();
		}

		public static void ShowWindow()
		{
			var window = CreateInstance<EditorIconsWindow>();
			window.titleContent = new GUIContent("Editor Icons");
			window.Show();
		}
	}
}