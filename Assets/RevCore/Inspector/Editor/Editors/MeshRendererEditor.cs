using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
	[CustomEditor(typeof(MeshRenderer))]
	[CanEditMultipleObjects]
	public sealed class MeshRendererEditor : UnityEditor.Editor
	{
		private MeshRenderer[] m_renderers;

		private void OnEnable()
		{
			m_renderers = targets.Cast<MeshRenderer>().ToArray();
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (m_renderers == null || m_renderers.Length == 0) return;

			string[] layerNames = SortingLayer.layers.Select(layer => layer.name).ToArray();
			if (layerNames.Length == 0) return;

			var first = m_renderers[0];
			int selected = System.Array.IndexOf(layerNames, first.sortingLayerName);
			if (selected < 0) selected = 0;

			EditorGUI.showMixedValue = m_renderers.Any(r => r.sortingLayerName != first.sortingLayerName);
			EditorGUI.BeginChangeCheck();
			selected = EditorGUILayout.Popup("Sorting Layer", selected, layerNames);
			if (EditorGUI.EndChangeCheck())
			{
				foreach (var renderer in m_renderers)
				{
					Undo.RecordObject(renderer, "Change Sorting Layer");
					renderer.sortingLayerName = layerNames[selected];
					EditorUtility.SetDirty(renderer);
				}
			}

			EditorGUI.showMixedValue = m_renderers.Any(r => r.sortingOrder != first.sortingOrder);
			EditorGUI.BeginChangeCheck();
			int order = EditorGUILayout.IntField("Order in Layer", first.sortingOrder);
			if (EditorGUI.EndChangeCheck())
			{
				foreach (var renderer in m_renderers)
				{
					Undo.RecordObject(renderer, "Change Sorting Order");
					renderer.sortingOrder = order;
					EditorUtility.SetDirty(renderer);
				}
			}

			EditorGUI.showMixedValue = false;
		}
	}
}
