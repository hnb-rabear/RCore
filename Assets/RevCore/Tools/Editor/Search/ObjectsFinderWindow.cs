using UnityEditor;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    public class ObjectsFinderWindow : EditorWindow
    {
        private Vector2 m_scrollPosition;
        private ScriptFinder m_scriptFinder = new();
        private ParticleSystemFinder m_particleSystemFinder = new();
        private PersistentEventFinder m_persistentEventFinder = new();

        [MenuItem("RevCore/Tools/Objects Finder", priority = 201)]
        public static void Open()
        {
            GetWindow<ObjectsFinderWindow>("Objects Finder");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

            EditorGUILayout.BeginVertical("box");
            m_scriptFinder.DrawOnGUI();
            EditorGUILayout.EndVertical();

            EditorGuiHelper.Separator();

            EditorGUILayout.BeginVertical("box");
            m_particleSystemFinder.DrawOnGUI();
            EditorGUILayout.EndVertical();

            EditorGuiHelper.Separator();

            EditorGUILayout.BeginVertical("box");
            m_persistentEventFinder.DrawOnGUI();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }
    }
}
