/***
 * Author RadBear - nbhung71711 @gmail.com - 2017
 **/

#if USE_DOTWEEN
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RCore.Common;
using System;
using Debug = RCore.Common.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Components
{
	public class OptimizedHorizontalScrollView : MonoBehaviour
	{
		public ScrollRect scrollView;
		public RectTransform container;
		public RectTransform viewRect;
		public OptimizedScrollItem prefab; //Pivot of prefab must be (0.5, 0.5)
		public int total = 1;
		public float spacing;
		public RectTransform borderLeft;
		public RectTransform borderRight;
		public RectTransform content => scrollView.content;

		private int m_TotalBuffer = 2;
		private int m_TotalVisible;
		private float m_HalfSizeContainer;
		private float m_CellSizeX;
		private float m_RightBarOffset;
		private float m_LeftBarOffset;

		private List<RectTransform> m_ItemsRect = new List<RectTransform>();
		private List<OptimizedScrollItem> m_ItemsScrolled = new List<OptimizedScrollItem>();
		private int m_OptimizedTotal;
		private Vector3 m_StartPos;
		private Vector3 m_OffsetVec;

		private void Start()
		{
			scrollView.horizontalScrollbar.onValueChanged.AddListener(ScrollBarChanged);
		}

		public void Init(OptimizedScrollItem pPrefab, int pTotalItems, bool pForce)
		{
			prefab = pPrefab;

			Init(pTotalItems, pForce);
		}

		private void LateUpdate()
		{
			for (int i = 0; i < m_ItemsScrolled.Count; i++)
				m_ItemsScrolled[i].ManualUpdate();
		}

		public void Init(int pTotalItems, bool pForce)
		{
			if (total == pTotalItems && !pForce)
				return;

			m_ItemsRect = new List<RectTransform>();

			if (m_ItemsScrolled == null || m_ItemsScrolled.Count == 0)
			{
				m_ItemsScrolled = new List<OptimizedScrollItem>();
				m_ItemsScrolled.Prepare(prefab, container.parent, 5);
			}
			else
				m_ItemsScrolled.Free(container);

			total = pTotalItems;

			container.anchoredPosition3D = new Vector3(0, 0, 0);

			var rectZero = m_ItemsScrolled[0].GetComponent<RectTransform>();
			var prefabSize = rectZero.rect.size;
			m_CellSizeX = prefabSize.x + spacing;

			container.sizeDelta = new Vector2(m_CellSizeX * total, prefabSize.y);

			if (borderLeft != null && borderLeft.gameObject.activeSelf)
				container.sizeDelta = container.sizeDelta.AddX(borderLeft.rect.size.x);
			if (borderRight != null && borderRight.gameObject.activeSelf)
				container.sizeDelta = container.sizeDelta.AddX(borderRight.rect.size.x);

			if (borderLeft != null && borderLeft.gameObject.activeSelf)
				m_LeftBarOffset = borderLeft.rect.size.x / container.sizeDelta.x;
			if (borderRight != null && borderRight.gameObject.activeSelf)
				m_RightBarOffset = borderRight.rect.size.x / container.sizeDelta.x;

			m_HalfSizeContainer = container.rect.size.x * 0.5f;

			m_TotalVisible = Mathf.CeilToInt(viewRect.rect.size.x / m_CellSizeX);

			m_OffsetVec = Vector3.right;
			m_StartPos = container.anchoredPosition3D - (m_OffsetVec * m_HalfSizeContainer) + (m_OffsetVec * (prefabSize.x * 0.5f));
			m_OptimizedTotal = Mathf.Min(total, m_TotalVisible + m_TotalBuffer);

			if (borderLeft != null && borderLeft.gameObject.activeSelf)
				m_StartPos.x += borderLeft.rect.size.x;

			for (int i = 0; i < m_OptimizedTotal; i++)
			{
				var item = m_ItemsScrolled.Obtain(container);
				var rt = item.transform as RectTransform;
				rt.anchoredPosition3D = m_StartPos + m_OffsetVec * (i * m_CellSizeX);
				m_ItemsRect.Add(rt);

				item.SetActive(true);
				item.UpdateContent(i, true);
			}

			prefab.gameObject.SetActive(false);
			container.anchoredPosition3D += m_OffsetVec * (m_HalfSizeContainer - (viewRect.rect.size.x * 0.5f));
		}

		public void MoveToTop()
		{
			scrollView.StopMovement();
			ScrollBarChanged(0);
			scrollView.horizontalScrollbar.value = 0;
		}

		public void MoveToBot()
		{
			scrollView.StopMovement();
			ScrollBarChanged(1);
			scrollView.horizontalScrollbar.value = 1;
		}

		public void RefreshScrollBar()
		{
			ScrollBarChanged(scrollView.horizontalScrollbar.value);
		}

		public void ScrollBarChanged(float pNormPos)
		{
			if (m_OptimizedTotal == 0)
			{
				Debug.LogError("m_OptimizedTotal should not be Zero");
				return;
			}
			float normPos = pNormPos;
			normPos += m_RightBarOffset * pNormPos;
			normPos -= m_LeftBarOffset * (1 - pNormPos);
			normPos = Mathf.Clamp(normPos, 0, 1);
			int numOutOfView = Mathf.CeilToInt(normPos * (total - m_TotalVisible));   //number of elements beyond the left boundary (or top)
			int firstIndex = Mathf.Max(0, numOutOfView - m_TotalBuffer);   //index of first element beyond the left boundary (or top)
			int originalIndex = firstIndex % m_OptimizedTotal;

			int newIndex = firstIndex;
			for (int i = originalIndex; i < m_OptimizedTotal; i++)
			{
				MoveItemByIndex(m_ItemsRect[i], newIndex);
				m_ItemsScrolled[i].UpdateContent(newIndex, false);
				newIndex++;
			}
			for (int i = 0; i < originalIndex; i++)
			{
				MoveItemByIndex(m_ItemsRect[i], newIndex);
				m_ItemsScrolled[i].UpdateContent(newIndex, false);
				newIndex++;
			}
		}

		public void Expand(int pTotalSlot)
		{
			total += pTotalSlot;
			container.sizeDelta = container.sizeDelta.AddX(pTotalSlot * m_CellSizeX);
			m_HalfSizeContainer = container.sizeDelta.x * 0.5f;

			var rectZero = m_ItemsScrolled[0].GetComponent<RectTransform>();
			var prefabSize = rectZero.rect.size;

			m_OffsetVec = Vector3.right;
			m_StartPos = Vector3.zero - (m_OffsetVec * m_HalfSizeContainer) + (m_OffsetVec * (prefabSize.x * 0.5f));
			m_OptimizedTotal = Mathf.Min(total, m_TotalVisible + m_TotalBuffer);

			if (borderLeft != null && borderLeft.gameObject.activeSelf)
			{
				m_StartPos.x += borderLeft.rect.size.x;
				m_LeftBarOffset = borderLeft.rect.size.x / container.sizeDelta.x;
			}
			if (borderRight != null && borderRight.gameObject.activeSelf)
				m_RightBarOffset = borderRight.rect.size.x / container.sizeDelta.x;

			ScrollBarChanged(scrollView.horizontalScrollbar.value);
		}

		private void MoveItemByIndex(RectTransform item, int index)
		{
			item.anchoredPosition3D = m_StartPos + m_OffsetVec * (index * m_CellSizeX);
		}

		public List<OptimizedScrollItem> GetListItem()
		{
			return m_ItemsScrolled;
		}

		public void MoveToTarget(int pIndex)
		{
			pIndex = Mathf.Clamp(pIndex, 0, total - 1);

			scrollView.StopMovement();

			float contentWidth = container.rect.width;
			float contentPivotX = container.pivot.x;

			//NOTE: Anchor of container must be center to the calculation is corrected
			float contentAnchoredXMin = contentWidth * (1 - contentPivotX) * -1 + (viewRect.rect.width * 0.5f);
			float contentAnchoredXMax = contentWidth * contentPivotX - (viewRect.rect.width * 0.5f);

			var prefabRect = (prefab.transform as RectTransform);
			float x = contentAnchoredXMax - (m_CellSizeX * pIndex) + (prefabRect.pivot.x - 0.5f) * prefabRect.rect.width;
			if (x > contentAnchoredXMax)
				x = contentAnchoredXMax;
			if (x < contentAnchoredXMin)
				x = contentAnchoredXMin;

			container.anchoredPosition = new Vector2(x, container.anchoredPosition.y);
			//Debug.Log($"Min:{contentAnchoredXMin}/Max:{contentAnchoredXMax}/Target:{x}");
		}
		public void CenterChild(int pIndex)
		{
			pIndex = Mathf.Clamp(pIndex, 0, total - 1);

			scrollView.StopMovement();

			float contentWidth = container.rect.width;
			float contentPivotX = container.pivot.x;

			//NOTE: Anchor of container must be center to the calculation is corrected
			float contentAnchoredXMin = contentWidth * (1 - contentPivotX) * -1 + (viewRect.rect.width * 0.5f);
			float contentAnchoredXMax = contentWidth * contentPivotX - (viewRect.rect.width * 0.5f);
			var x = -(m_StartPos + m_OffsetVec * (pIndex * m_CellSizeX)).x;
			if (x > contentAnchoredXMax)
				x = contentAnchoredXMax;
			if (x < contentAnchoredXMin)
				x = contentAnchoredXMin;
			container.anchoredPosition = new Vector2(x, container.anchoredPosition.y);
		}

		public int TotalFullCellVisible()
		{
			var rectZero = prefab.GetComponent<RectTransform>();
			var cellSizeX = rectZero.rect.size.x + spacing;
			return Mathf.FloorToInt(viewRect.rect.size.x / cellSizeX);
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(OptimizedHorizontalScrollView))]
		public class OptimizedHorizontalScrollViewEditor : UnityEditor.Editor
		{
			private OptimizedHorizontalScrollView mScript;

			private void OnEnable()
			{
				mScript = (OptimizedHorizontalScrollView)target;
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				if (EditorHelper.Button("Move To Top"))
					mScript.MoveToTop();
				if (EditorHelper.Button("Move To Top 2"))
					mScript.MoveToTarget(0);
				if (EditorHelper.Button("Move To Top 3"))
					mScript.MoveToTarget(0);
			}
		}
#endif
	}
}