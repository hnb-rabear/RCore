using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
    [CustomEditor(typeof(SkinnedMeshRenderer))]
    public class SkinnedMeshRendererEditor : UnityEditor.Editor
    {
        private SkinnedMeshRenderer mScript;

        private void OnEnable()
        {
            mScript = (SkinnedMeshRenderer)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (mScript.sharedMesh == null)
                return;

            GUILayout.BeginVertical("box");
            int a = mScript.sharedMesh.vertexCount;
            int b = mScript.sharedMesh.triangles.Length / 3;
            int c = mScript.sharedMesh.subMeshCount;
            EditorGUILayout.LabelField("Vertices: ", a.ToString());
            EditorGUILayout.LabelField("Triangles: ", b.ToString());
            EditorGUILayout.LabelField("SubMeshes: ", c.ToString());
            GUILayout.EndVertical();
        }
    }

    [CustomEditor(typeof(MeshFilter))]
    public class MeshFilterEditor : UnityEditor.Editor
    {
        private MeshFilter mScript;

        private void OnEnable()
        {
            mScript = (MeshFilter)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (mScript.sharedMesh == null)
                return;

            GUILayout.BeginVertical("box");
            int a = mScript.sharedMesh.vertexCount;
            int b = mScript.sharedMesh.triangles.Length / 3;
            int c = mScript.sharedMesh.subMeshCount;
            EditorGUILayout.LabelField("Vertices: ", a.ToString());
            EditorGUILayout.LabelField("Triangles: ", b.ToString());
            EditorGUILayout.LabelField("SubMeshes: ", c.ToString());
            GUILayout.EndVertical();
        }
    }
}