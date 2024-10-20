#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	public class TextEditorWindow : EditorWindow
	{
		private string m_content;
		private Action<string> m_onContentSaved;
		private Vector2 m_scrollPosition;

		public static void ShowWindow(string initialContent, Action<string> callback)
		{
			var window = CreateInstance<TextEditorWindow>();
			window.titleContent = new GUIContent("Text Editor");
			window.m_content = initialContent;
			window.m_onContentSaved = callback;
			window.ShowUtility();
		}

		// This method is called to render the editor window
		private void OnGUI()
		{
			EditorGUILayout.LabelField("Content:");
			m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.Height(200));
			var textAreaStyle = new GUIStyle(EditorStyles.textArea)
			{
				wordWrap = true
			};
			m_content = EditorGUILayout.TextArea(m_content, textAreaStyle, GUILayout.Height(200));
			EditorGUILayout.EndScrollView();
			GUILayout.Space(10);

			// Buttons for saving or canceling the operation
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Save"))
			{
				m_onContentSaved?.Invoke(m_content);
				Close();
			}
			if (GUILayout.Button("Cancel"))
			{
				Close();
			}
			EditorGUILayout.EndHorizontal();
		}
		
		private void OnLostFocus()
		{
			// Force window to regain focus to prevent clicking on other editor windows
			Focus();
		}
	}
}
#endif