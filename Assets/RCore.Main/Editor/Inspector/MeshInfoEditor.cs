using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Inspector
{
    [CustomEditor(typeof(SkinnedMeshRenderer))]
    [CanEditMultipleObjects]
    public class SkinnedMeshRendererEditor : UnityEditor.Editor
    {
        private SkinnedMeshRenderer m_script;

        private void OnEnable()
        {
            m_script = (SkinnedMeshRenderer)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (m_script.sharedMesh == null)
                return;

            GUILayout.BeginVertical("box");
            int a = m_script.sharedMesh.vertexCount;
            int b = m_script.sharedMesh.triangles.Length / 3;
            int c = m_script.sharedMesh.subMeshCount;
            EditorGUILayout.LabelField("Vertices: ", a.ToString());
            EditorGUILayout.LabelField("Triangles: ", b.ToString());
            EditorGUILayout.LabelField("SubMeshes: ", c.ToString());
            GUILayout.EndVertical();
        }
    }

    [CustomEditor(typeof(MeshFilter))]
    [CanEditMultipleObjects]
    public class MeshFilterEditor : UnityEditor.Editor
    {
        private MeshFilter m_script;

        private void OnEnable()
        {
            m_script = (MeshFilter)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (m_script.sharedMesh == null)
                return;

            GUILayout.BeginVertical("box");
            int a = m_script.sharedMesh.vertexCount;
            int b = m_script.sharedMesh.triangles.Length / 3;
            int c = m_script.sharedMesh.subMeshCount;
            EditorGUILayout.LabelField("Vertices: ", a.ToString());
            EditorGUILayout.LabelField("Triangles: ", b.ToString());
            EditorGUILayout.LabelField("SubMeshes: ", c.ToString());
            GUILayout.EndVertical();
        }
    }
}