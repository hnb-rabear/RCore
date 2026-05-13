using UnityEditor;
using UnityEngine;

namespace RevCore.UI.Editor
{
    [CustomEditor(typeof(PanelStack), true)]
    public class PanelStackEditor : UnityEditor.Editor
    {
        protected PanelStack m_script;

        protected virtual void OnEnable()
        {
            m_script = target as PanelStack;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Children Count: " + m_script.StackCount, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Index: " + m_script.Index, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Display Order: " + m_script.DisplayOrder, EditorStyles.boldLabel);
            if (m_script.GetComponent<Canvas>() != null)
                GUILayout.Label("NOTE: sub-panel should not have Canvas component!\nIt should be inherited from parent panel");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(m_script.TopPanel == null ? "TopPanel: Null" : $"TopPanel: {m_script.TopPanel.name}");
            ShowChildrenList(m_script, 0);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        private void ShowChildrenList(PanelStack panel, int pLevelIndent)
        {
            int levelIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = pLevelIndent;

            var field = typeof(PanelStack).GetField("panelStack", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.FlattenHierarchy);
            var stack = field?.GetValue(panel) as System.Collections.Generic.Stack<PanelController>;
            if (stack == null)
            {
                EditorGUI.indentLevel = levelIndent;
                return;
            }

            foreach (var p in stack)
            {
                if (GUILayout.Button($"{p.Index}: {p.name}"))
                    Selection.activeObject = p.gameObject;
                if (p.StackCount > 0)
                {
                    EditorGUI.indentLevel++;
                    levelIndent = EditorGUI.indentLevel;
                    ShowChildrenList(p, levelIndent);
                }
            }

            EditorGUI.indentLevel = levelIndent;
        }
    }
}
