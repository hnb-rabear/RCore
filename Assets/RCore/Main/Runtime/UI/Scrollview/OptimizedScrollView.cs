/***
 * Author HNB-RaBear - 2017
 **/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RCore.UI
{
	public enum ScrollDirection
	{
		Horizontal = 0,
		Vertical = 1
	}

	public class OptimizedScrollView : MonoBehaviour
	{
		public ScrollRect scrollView;
		public RectTransform container;
		public Mask mask;
		public OptimizedScrollItem prefab;
		public int total = 1;
		public float spacing;
		public ScrollDirection Direction = ScrollDirection.Horizontal;

		private RectTransform m_maskRect;
		private int m_totalVisible;
		private int m_totalBuffer = 2;
		private float m_halfSizeContainer;
		private float m_prefabSize;

		private List<RectTransform> m_itemsRect = new List<RectTransform>();
		private List<OptimizedScrollItem> m_itemsScrolled = new List<OptimizedScrollItem>();
		private int m_optimizedTotal = 0;
		private Vector3 m_startPos;
		private Vector3 m_offsetVec;

		private void Start()
		{
			if (Direction == ScrollDirection.Vertical)
				scrollView.verticalScrollbar.onValueChanged.AddListener(ScrollBarChanged);
			else if (Direction == ScrollDirection.Horizontal)
				scrollView.horizontalScrollbar.onValueChanged.AddListener(ScrollBarChanged);

			Initialize(total);
		}

		private void LateUpdate()
		{
			for (int i = 0; i < m_itemsScrolled.Count; i++)
				m_itemsScrolled[i].ManualUpdate();
		}

		public void Initialize(int pTotalItems)
		{
			if (pTotalItems == total)
				return;

			m_itemsRect = new List<RectTransform>();

			if (m_itemsScrolled == null || m_itemsScrolled.Count == 0)
			{
				m_itemsScrolled = new List<OptimizedScrollItem>();
				m_itemsScrolled.Prepare(prefab, container.parent, 5);
			}
			else
			{
				m_itemsScrolled.Free(container);
			}

			total = pTotalItems;

			container.anchoredPosition3D = new Vector3(0, 0, 0);

			if (m_maskRect == null)
				m_maskRect = mask.GetComponent<RectTransform>();

			var prefabScale = m_itemsScrolled[0].GetComponent<RectTransform>().rect.size;
			m_prefabSize = (Direction == ScrollDirection.Horizontal ? prefabScale.x : prefabScale.y) + spacing;

			container.sizeDelta = Direction == ScrollDirection.Horizontal ? (new Vector2(m_prefabSize * total, prefabScale.y)) : (new Vector2(prefabScale.x, m_prefabSize * total));
			m_halfSizeContainer = Direction == ScrollDirection.Horizontal ? (container.rect.size.x * 0.5f) : (container.rect.size.y * 0.5f);

			m_totalVisible = Mathf.CeilToInt((Direction == ScrollDirection.Horizontal ? m_maskRect.rect.size.x : m_maskRect.rect.size.y) / m_prefabSize);

			m_offsetVec = Direction == ScrollDirection.Horizontal ? Vector3.right : Vector3.down;
			m_startPos = container.anchoredPosition3D - (m_offsetVec * m_halfSizeContainer) + (m_offsetVec * ((Direction == ScrollDirection.Horizontal ? prefabScale.x : prefabScale.y) * 0.5f));
			m_optimizedTotal = Mathf.Min(total, m_totalVisible + m_totalBuffer);
			for (int i = 0; i < m_optimizedTotal; i++)
			{
				var item = m_itemsScrolled.Obtain(container);
				var rt = item.GetComponent<RectTransform>();
				rt.anchoredPosition3D = m_startPos + (m_offsetVec * i * m_prefabSize);
				m_itemsRect.Add(rt);

				item.gameObject.SetActive(true);
				item.UpdateContent(i, true);
			}

			prefab.gameObject.SetActive(false);
			container.anchoredPosition3D += m_offsetVec * (m_halfSizeContainer - ((Direction == ScrollDirection.Horizontal ? m_maskRect.rect.size.x : m_maskRect.rect.size.y) * 0.5f));
		}

		public void ScrollBarChanged(float pNormPos)
		{
			if (Direction == ScrollDirection.Vertical)
				pNormPos = 1f - pNormPos;

			if (pNormPos <= 0)
				return;

			if (pNormPos > 1)
				pNormPos = 1f;

			int numOutOfView = Mathf.CeilToInt(pNormPos * (total - m_totalVisible)); //number of elements beyond the left boundary (or top)
			int firstIndex = Mathf.Max(0, numOutOfView - m_totalBuffer); //index of first element beyond the left boundary (or top)
			int originalIndex = firstIndex % m_optimizedTotal;

			int newIndex = firstIndex;
			for (int i = originalIndex; i < m_optimizedTotal; i++)
			{
				moveItemByIndex(m_itemsRect[i], newIndex);
				m_itemsScrolled[i].UpdateContent(newIndex, false);
				newIndex++;
			}
			for (int i = 0; i < originalIndex; i++)
			{
				moveItemByIndex(m_itemsRect[i], newIndex);
				m_itemsScrolled[i].UpdateContent(newIndex, false);
				newIndex++;
			}
		}

		private void moveItemByIndex(RectTransform item, int index)
		{
			item.anchoredPosition3D = m_startPos + (m_offsetVec * index * m_prefabSize);
		}

		public List<OptimizedScrollItem> GetListItem()
		{
			return m_itemsScrolled;
		}
	}
}