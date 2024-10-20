using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using UnityEditorInternal;

namespace RCore.Editor.Inspector
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(MeshRenderer))]
	public class MeshRendererEditor : UnityEditor.Editor
	{
		private string[] m_sortingLayerNames;
		private int m_sortingOrder;
		private int m_sortingLayer;
		private MeshRenderer[] m_renderer;
		private bool m_sortingLayerEqual;
		private bool m_sortingOrderEqual;

		private void OnEnable()
		{
			m_sortingLayerNames = GetSortingLayerNames();

			System.Object[] objects = serializedObject.targetObjects;

			var first = objects[0] as MeshRenderer;
			m_sortingOrder = first.sortingOrder;
			string layerName = first.sortingLayerName;
			m_sortingLayer = Mathf.Max(Array.IndexOf(m_sortingLayerNames, layerName), 0);

			m_renderer = new MeshRenderer[objects.Length];
			m_sortingLayerEqual = true;
			m_sortingOrderEqual = true;
			for (int i = 0; i < objects.Length; i++)
			{
				m_renderer[i] = objects[i] as MeshRenderer;
				if (m_renderer[i].sortingOrder != m_sortingOrder)
					m_sortingOrderEqual = false;
				if (m_renderer[i].sortingLayerName != layerName)
					m_sortingLayerEqual = false;
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.Space();

			EditorGUI.BeginChangeCheck();

			EditorGUI.showMixedValue = !m_sortingLayerEqual;
			m_sortingLayer = EditorGUILayout.Popup(m_sortingLayer, m_sortingLayerNames);

			if (EditorGUI.EndChangeCheck())
			{
				foreach (var r in m_renderer)
				{
					r.sortingLayerName = m_sortingLayerNames[m_sortingLayer];
					EditorUtility.SetDirty(r);
				}
				m_sortingLayerEqual = true;
			}

			EditorGUI.BeginChangeCheck();

			EditorGUI.showMixedValue = !m_sortingOrderEqual;
			m_sortingOrder = EditorGUILayout.IntField("Order in Layer", m_sortingOrder);

			if (EditorGUI.EndChangeCheck())
			{
				foreach (var r in m_renderer)
				{
					r.sortingOrder = m_sortingOrder;
					EditorUtility.SetDirty(r);
				}
				m_sortingOrderEqual = true;
			}
		}

		private string[] GetSortingLayerNames()
		{
			var t = typeof(InternalEditorUtility);
			var prop = t.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
			return (string[])prop.GetValue(null, null);
		}
	}
}