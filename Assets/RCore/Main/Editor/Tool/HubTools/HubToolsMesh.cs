using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RCore.Editor.Tool
{
	public class MeshInfoTool : RCoreHubTool
	{
		public override string Name => "Mesh Info";
		public override string Category => "Mesh";
		public override string Description => "Display vertex count, triangle count, and submesh count for selected objects.";
		public override bool IsQuickAction => true;

		private int m_MeshCount = 1;
		private int m_VertexCount;
		private int m_SubMeshCount;
		private int m_TriangleCount;

		public override void DrawCard()
		{
			if (m_MeshCount == 0)
				GUILayout.Label("Select GameObjects with MeshFilter", EditorStyles.helpBox);

			if (m_MeshCount > 1)
			{
				GUILayout.Label($"Total Vertices: {m_VertexCount}");
				GUILayout.Label($"Total Triangles: {m_TriangleCount}");
				GUILayout.Label($"Total SubMeshes: {m_SubMeshCount}");
			}

			if (EditorHelper.ButtonColor("Scan Mesh Info", Color.cyan))
			{
				m_VertexCount = 0;
				m_TriangleCount = 0;
				m_SubMeshCount = 0;
				m_MeshCount = 0;

				if (Selection.gameObjects != null)
				{
					foreach (var g in Selection.gameObjects)
					{
						var filter = g.GetComponent<MeshFilter>();
						if (filter != null && filter.sharedMesh != null)
						{
							m_VertexCount += filter.sharedMesh.vertexCount;
							m_TriangleCount += filter.sharedMesh.triangles.Length / 3;
							m_SubMeshCount += filter.sharedMesh.subMeshCount;
							m_MeshCount++;
							continue;
						}

						var objs = g.GetComponentsInChildren<SkinnedMeshRenderer>(true);
						if (objs != null)
						{
							foreach (var obj in objs)
							{
								if (obj.sharedMesh == null) continue;
								m_VertexCount += obj.sharedMesh.vertexCount;
								m_TriangleCount += obj.sharedMesh.triangles.Length / 3;
								m_SubMeshCount += obj.sharedMesh.subMeshCount;
								m_MeshCount++;
							}
						}
					}
				}
			}
		}
	}

	public class CombineMeshesTool : RCoreHubTool
	{
		public override string Name => "Combine Meshes";
		public override string Category => "Mesh";
		public override string Description => "Combine child meshes into a single mesh object to reduce draw calls.";
		public override bool IsQuickAction => true;

		public override void DrawCard()
		{
			var objs = Selection.gameObjects;
			if (objs == null || objs.Length == 0)
			{
				GUILayout.Label("Select Objects with MeshFilter & MeshRenderer");
				return;
			}

			if (EditorHelper.ButtonColor("Combine Selected Meshes", Color.cyan))
			{
				var combinedMeshes = new GameObject();
				combinedMeshes.name = "Meshes_Combined";
				combinedMeshes.AddComponent<MeshRenderer>();
				combinedMeshes.AddComponent<MeshFilter>();

				var meshFilters = new List<MeshFilter>();
				foreach (var g in objs)
				{
					var filters = g.GetComponentsInChildren<MeshFilter>(true);
					meshFilters.AddRange(filters);
				}

				var combine = new CombineInstance[meshFilters.Count];
				int i = 0;
				while (i < meshFilters.Count)
				{
					combine[i].mesh = meshFilters[i].sharedMesh;
					combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
					meshFilters[i].gameObject.SetActive(false);
					i++;
				}

				combinedMeshes.GetComponent<MeshFilter>().sharedMesh = new Mesh();
				combinedMeshes.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine);
			}
		}
	}

	public class AlignCenterMeshRendererTool : RCoreHubTool
	{
		public override string Name => "Align Center Mesh Renderer";
		public override string Category => "Mesh";
		public override string Description => "Center the localPosition of the object based on its MeshRenderer bounds.";
		public override bool IsQuickAction => true;

		public override void DrawCard()
		{
			var objs = Selection.gameObjects;
			if (objs == null || objs.Length == 0)
			{
				GUILayout.Label("Select GameObjects with Renderer");
				return;
			}

			if (EditorHelper.ButtonColor("Align Center Extents", Color.cyan))
			{
				foreach (var g in objs)
				{
					var renderer = g.transform.GetComponent<Renderer>();
					if (renderer != null)
					{
						var center = renderer.bounds.extents;
						g.transform.localPosition = new Vector3(-center.x, g.transform.localPosition.y, -center.z);
					}
				}
			}
		}
	}
}
