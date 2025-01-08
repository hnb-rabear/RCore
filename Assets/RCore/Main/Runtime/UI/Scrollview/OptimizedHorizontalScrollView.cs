/**
 * Author HNB-RaBear - 2017
 **/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif

namespace RCore.UI
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

		private int m_totalBuffer = 2;
		private int m_totalVisible;
		private float m_halfSizeContainer;
		private float m_cellSizeX;
		private float m_rightBarOffset;
		private float m_leftBarOffset;

		private List<RectTransform> m_itemsRect = new List<RectTransform>();
		private List<OptimizedScrollItem> m_itemsScrolled = new List<OptimizedScrollItem>();
		private int m_optimizedTotal;
		private Vector3 m_startPos;
		private Vector3 m_offsetVec;

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
			for (int i = 0; i < m_itemsScrolled.Count; i++)
				m_itemsScrolled[i].ManualUpdate();
		}

		public void Init(int pTotalItems, bool pForce)
		{
			if (total == pTotalItems && !pForce)
				return;

			m_itemsRect = new List<RectTransform>();

			if (m_itemsScrolled == null || m_itemsScrolled.Count == 0)
			{
				m_itemsScrolled = new List<OptimizedScrollItem>();
				m_itemsScrolled.Prepare(prefab, container.parent, 5);
			}
			else
				m_itemsScrolled.Free(container);

			total = pTotalItems;

			container.anchoredPosition3D = new Vector3(0, 0, 0);

			var rectZero = m_itemsScrolled[0].GetComponent<RectTransform>();
			var prefabSize = rectZero.rect.size;
			m_cellSizeX = prefabSize.x + spacing;

			container.sizeDelta = new Vector2(m_cellSizeX * total, prefabSize.y);

			if (borderLeft != null && borderLeft.gameObject.activeSelf)
				container.sizeDelta = container.sizeDelta.AddX(borderLeft.rect.size.x);
			if (borderRight != null && borderRight.gameObject.activeSelf)
				container.sizeDelta = container.sizeDelta.AddX(borderRight.rect.size.x);

			if (borderLeft != null && borderLeft.gameObject.activeSelf)
				m_leftBarOffset = borderLeft.rect.size.x / container.sizeDelta.x;
			if (borderRight != null && borderRight.gameObject.activeSelf)
				m_rightBarOffset = borderRight.rect.size.x / container.sizeDelta.x;

			m_halfSizeContainer = container.rect.size.x * 0.5f;

			m_totalVisible = Mathf.CeilToInt(viewRect.rect.size.x / m_cellSizeX);

			m_offsetVec = Vector3.right;
			m_startPos = container.anchoredPosition3D - m_offsetVec * m_halfSizeContainer + m_offsetVec * (prefabSize.x * 0.5f);
			m_optimizedTotal = Mathf.Min(total, m_totalVisible + m_totalBuffer);

			if (borderLeft != null && borderLeft.gameObject.activeSelf)
				m_startPos.x += borderLeft.rect.size.x;

			for (int i = 0; i < m_optimizedTotal; i++)
			{
				var item = m_itemsScrolled.Obtain(container);
				var rt = item.transform as RectTransform;
				rt.anchoredPosition3D = m_startPos + m_offsetVec * (i * m_cellSizeX);
				m_itemsRect.Add(rt);

				item.gameObject.SetActive(true);
				item.UpdateContent(i, true);
			}

			prefab.gameObject.SetActive(false);
			container.anchoredPosition3D += m_offsetVec * (m_halfSizeContainer - viewRect.rect.size.x * 0.5f);
		}

		public void ScrollToTop()
		{
			scrollView.StopMovement();
			ScrollBarChanged(0);
			scrollView.horizontalScrollbar.value = 0;
		}

		public void ScrollToBot()
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
			if (m_optimizedTotal == 0)
			{
				Debug.LogError("m_OptimizedTotal should not be Zero");
				return;
			}
			float normPos = pNormPos;
			normPos += m_rightBarOffset * pNormPos;
			normPos -= m_leftBarOffset * (1 - pNormPos);
			normPos = Mathf.Clamp(normPos, 0, 1);
			int numOutOfView = Mathf.CeilToInt(normPos * (total - m_totalVisible)); //number of elements beyond the left boundary (or top)
			int firstIndex = Mathf.Max(0, numOutOfView - m_totalBuffer); //index of first element beyond the left boundary (or top)
			int originalIndex = firstIndex % m_optimizedTotal;

			int newIndex = firstIndex;
			for (int i = originalIndex; i < m_optimizedTotal; i++)
			{
				MoveItemByIndex(m_itemsRect[i], newIndex);
				m_itemsScrolled[i].UpdateContent(newIndex, false);
				newIndex++;
			}
			for (int i = 0; i < originalIndex; i++)
			{
				MoveItemByIndex(m_itemsRect[i], newIndex);
				m_itemsScrolled[i].UpdateContent(newIndex, false);
				newIndex++;
			}
		}

		public void Expand(int pTotalSlot)
		{
			total += pTotalSlot;
			container.sizeDelta = container.sizeDelta.AddX(pTotalSlot * m_cellSizeX);
			m_halfSizeContainer = container.sizeDelta.x * 0.5f;

			var rectZero = m_itemsScrolled[0].GetComponent<RectTransform>();
			var prefabSize = rectZero.rect.size;

			m_offsetVec = Vector3.right;
			m_startPos = Vector3.zero - m_offsetVec * m_halfSizeContainer + m_offsetVec * (prefabSize.x * 0.5f);
			m_optimizedTotal = Mathf.Min(total, m_totalVisible + m_totalBuffer);

			if (borderLeft != null && borderLeft.gameObject.activeSelf)
			{
				m_startPos.x += borderLeft.rect.size.x;
				m_leftBarOffset = borderLeft.rect.size.x / container.sizeDelta.x;
			}
			if (borderRight != null && borderRight.gameObject.activeSelf)
				m_rightBarOffset = borderRight.rect.size.x / container.sizeDelta.x;

			ScrollBarChanged(scrollView.horizontalScrollbar.value);
		}

		private void MoveItemByIndex(RectTransform item, int index)
		{
			item.anchoredPosition3D = m_startPos + m_offsetVec * (index * m_cellSizeX);
		}

		public List<OptimizedScrollItem> GetListItem()
		{
			return m_itemsScrolled;
		}

		public void ScrollToTarget(int pIndex)
		{
			pIndex = Mathf.Clamp(pIndex, 0, total - 1);

			scrollView.StopMovement();

			float contentWidth = container.rect.width;
			float contentPivotX = container.pivot.x;

			//NOTE: Anchor of container must be center to the calculation is corrected
			float contentAnchoredXMin = contentWidth * (1 - contentPivotX) * -1 + viewRect.rect.width * 0.5f;
			float contentAnchoredXMax = contentWidth * contentPivotX - viewRect.rect.width * 0.5f;

			var prefabRect = prefab.transform as RectTransform;
			float x = contentAnchoredXMax - m_cellSizeX * pIndex + (prefabRect.pivot.x - 0.5f) * prefabRect.rect.width;
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
			float contentAnchoredXMin = contentWidth * (1 - contentPivotX) * -1 + viewRect.rect.width * 0.5f;
			float contentAnchoredXMax = contentWidth * contentPivotX - viewRect.rect.width * 0.5f;
			var x = -(m_startPos + m_offsetVec * (pIndex * m_cellSizeX)).x;
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
#if ODIN_INSPECTOR
		public class OptimizedHorizontalScrollViewEditor : Sirenix.OdinInspector.Editor.OdinEditor
		{
			private OptimizedHorizontalScrollView m_script;

			protected override void OnEnable()
			{
				base.OnEnable();
				m_script = (OptimizedHorizontalScrollView)target;
			}
#else
		public class OptimizedHorizontalScrollViewEditor : UnityEditor.Editor
		{
			private OptimizedHorizontalScrollView m_script;

			private void OnEnable()
			{
				m_script = (OptimizedHorizontalScrollView)target;
			}
#endif
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				if (EditorHelper.Button("Move To Top"))
					m_script.ScrollToTop();
				if (EditorHelper.Button("Move To Top 2"))
					m_script.ScrollToTarget(0);
				if (EditorHelper.Button("Move To Top 3"))
					m_script.ScrollToTarget(0);
			}
		}
#endif
	}
}