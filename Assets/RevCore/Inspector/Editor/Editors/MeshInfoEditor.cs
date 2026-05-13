using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
	[CustomEditor(typeof(SkinnedMeshRenderer))]
	[CanEditMultipleObjects]
	public sealed class SkinnedMeshRendererEditor : UnityEditor.Editor
	{
		private SkinnedMeshRenderer m_renderer;

		private void OnEnable()
		{
			m_renderer = (SkinnedMeshRenderer)target;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			DrawMeshInfo(m_renderer.sharedMesh);
		}

		private static void DrawMeshInfo(Mesh mesh)
		{
			if (mesh == null) return;
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField("Vertices", mesh.vertexCount.ToString());
			EditorGUILayout.LabelField("Triangles", (mesh.triangles.Length / 3).ToString());
			EditorGUILayout.LabelField("Sub Meshes", mesh.subMeshCount.ToString());
			EditorGUILayout.EndVertical();
		}
	}

	[CustomEditor(typeof(MeshFilter))]
	[CanEditMultipleObjects]
	public sealed class MeshFilterEditor : UnityEditor.Editor
	{
		private MeshFilter m_filter;

		private void OnEnable()
		{
			m_filter = (MeshFilter)target;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			DrawMeshInfo(m_filter.sharedMesh);
		}

		private static void DrawMeshInfo(Mesh mesh)
		{
			if (mesh == null) return;
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField("Vertices", mesh.vertexCount.ToString());
			EditorGUILayout.LabelField("Triangles", (mesh.triangles.Length / 3).ToString());
			EditorGUILayout.LabelField("Sub Meshes", mesh.subMeshCount.ToString());
			EditorGUILayout.EndVertical();
		}
	}
}
