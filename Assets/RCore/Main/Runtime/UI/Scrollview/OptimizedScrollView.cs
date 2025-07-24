/***
 * Author HNB-RaBear - 2017
 **/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RCore.UI
{
	/// <summary>
	/// Defines the scrolling direction for the OptimizedScrollView.
	/// </summary>
	public enum ScrollDirection
	{
		Horizontal = 0,
		Vertical = 1
	}

	/// <summary>
	/// A generic optimized scroll view that recycles its items. It can be configured
	/// to scroll either horizontally or vertically. This is a base for more specialized
	/// scroll views and is useful for lists with many items to improve performance.
	/// </summary>
	public class OptimizedScrollView : MonoBehaviour
	{
		/// <summary>
		/// The main ScrollRect component.
		/// </summary>
		public ScrollRect scrollView;
		/// <summary>
		/// The RectTransform that holds the scrollable content.
		/// </summary>
		public RectTransform container;
		/// <summary>
		/// The Mask component that defines the visible area.
		/// </summary>
		public Mask mask;
		/// <summary>
		/// The prefab for a single item in the scroll view.
		/// </summary>
		public OptimizedScrollItem prefab;
		/// <summary>
		/// The total number of items in the list.
		/// </summary>
		public int total = 1;
		/// <summary>
		/// The spacing between items.
		/// </summary>
		public float spacing;
		/// <summary>
		/// The direction of scrolling (Horizontal or Vertical).
		/// </summary>
		public ScrollDirection Direction = ScrollDirection.Horizontal;

		/// <summary>
		/// The RectTransform of the mask, used to determine viewport size.
		/// </summary>
		private RectTransform m_maskRect;
		/// <summary>
		/// The number of items that can be visible in the viewport at one time.
		/// </summary>
		private int m_totalVisible;
		/// <summary>
		/// The number of items to instantiate on either side of the visible area as a buffer.
		/// </summary>
		private int m_totalBuffer = 2;
		/// <summary>
		/// Half of the container's size in the scrolling direction.
		/// </summary>
		private float m_halfSizeContainer;
		/// <summary>
		/// The size of a single cell (prefab size + spacing) in the scrolling direction.
		/// </summary>
		private float m_prefabSize;

		/// <summary>
		/// A list of the RectTransforms of the recycled item instances.
		/// </summary>
		private List<RectTransform> m_itemsRect = new List<RectTransform>();
		/// <summary>
		/// A list of the script components of the recycled item instances.
		/// </summary>
		private List<OptimizedScrollItem> m_itemsScrolled = new List<OptimizedScrollItem>();
		/// <summary>
		/// The number of items that are actually instantiated (visible items + buffer).
		/// </summary>
		private int m_optimizedTotal = 0;
		/// <summary>
		/// The starting position for the first item.
		/// </summary>
		private Vector3 m_startPos;
		/// <summary>
		/// A vector representing the direction of scrolling (e.g., Vector3.right or Vector3.down).
		/// </summary>
		private Vector3 m_offsetVec;

		private void Start()
		{
			// Subscribe to the appropriate scrollbar's value changed event.
			if (Direction == ScrollDirection.Vertical)
				scrollView.verticalScrollbar.onValueChanged.AddListener(ScrollBarChanged);
			else if (Direction == ScrollDirection.Horizontal)
				scrollView.horizontalScrollbar.onValueChanged.AddListener(ScrollBarChanged);

			Initialize(total);
		}

		/// <summary>
		/// Called every frame after all Update functions have been called.
		/// Used to manually update the visible scroll items.
		/// </summary>
		private void LateUpdate()
		{
			for (int i = 0; i < m_itemsScrolled.Count; i++)
				m_itemsScrolled[i].ManualUpdate();
		}

		/// <summary>
		/// Initializes or re-initializes the scroll view. Sets up item pooling, calculates container size,
		/// and positions the initial items based on the specified scroll direction.
		/// </summary>
		/// <param name="pTotalItems">The total number of items in the list.</param>
		public void Initialize(int pTotalItems)
		{
			if (pTotalItems == total)
				return;

			m_itemsRect = new List<RectTransform>();

			// Initialize or reset the item pool.
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
			container.anchoredPosition3D = Vector3.zero;

			if (m_maskRect == null)
				m_maskRect = mask.GetComponent<RectTransform>();

			// Calculate cell and container sizes based on scroll direction.
			var prefabScale = m_itemsScrolled[0].GetComponent<RectTransform>().rect.size;
			m_prefabSize = (Direction == ScrollDirection.Horizontal ? prefabScale.x : prefabScale.y) + spacing;
			container.sizeDelta = Direction == ScrollDirection.Horizontal ? (new Vector2(m_prefabSize * total, prefabScale.y)) : (new Vector2(prefabScale.x, m_prefabSize * total));
			m_halfSizeContainer = Direction == ScrollDirection.Horizontal ? (container.rect.size.x * 0.5f) : (container.rect.size.y * 0.5f);

			// Determine number of visible items.
			m_totalVisible = Mathf.CeilToInt((Direction == ScrollDirection.Horizontal ? m_maskRect.rect.size.x : m_maskRect.rect.size.y) / m_prefabSize);

			// Determine starting position and the number of items to instantiate.
			m_offsetVec = Direction == ScrollDirection.Horizontal ? Vector3.right : Vector3.down;
			m_startPos = container.anchoredPosition3D - (m_offsetVec * m_halfSizeContainer) + (m_offsetVec * ((Direction == ScrollDirection.Horizontal ? prefabScale.x : prefabScale.y) * 0.5f));
			m_optimizedTotal = Mathf.Min(total, m_totalVisible + m_totalBuffer);
			
			// Instantiate and position the initial set of items.
			for (int i = 0; i < m_optimizedTotal; i++)
			{
				var item = m_itemsScrolled.Obtain(container);
				var rt = item.GetComponent<RectTransform>();
				rt.anchoredPosition3D = m_startPos + (m_offsetVec * i * m_prefabSize);
				m_itemsRect.Add(rt);
				item.gameObject.SetActive(true);
				item.UpdateContent(i, true);
			}

			// Deactivate the original prefab and set the initial scroll position.
			prefab.gameObject.SetActive(false);
			container.anchoredPosition3D += m_offsetVec * (m_halfSizeContainer - ((Direction == ScrollDirection.Horizontal ? m_maskRect.rect.size.x : m_maskRect.rect.size.y) * 0.5f));
		}

		/// <summary>
		/// This is the core logic for the optimized scroll view.
		/// It is called whenever the scrollbar's value changes. It calculates which items should be visible
		/// and repositions/recycles the instantiated items to represent the correct data.
		/// </summary>
		/// <param name="pNormPos">The current normalized position of the scrollbar (0 to 1).</param>
		public void ScrollBarChanged(float pNormPos)
		{
			// Vertical scrollbar value is inverted (1 is top, 0 is bottom).
			if (Direction == ScrollDirection.Vertical)
				pNormPos = 1f - pNormPos;

			if (pNormPos <= 0)
				return;
			if (pNormPos > 1)
				pNormPos = 1f;

			// Calculate the index of the first item that should be in the buffer zone.
			int numOutOfView = Mathf.CeilToInt(pNormPos * (total - m_totalVisible));
			int firstIndex = Mathf.Max(0, numOutOfView - m_totalBuffer);
			// Determine which of our pooled items corresponds to this new first index.
			int originalIndex = firstIndex % m_optimizedTotal;

			// Reposition and update the content of the pooled items based on the new scroll position.
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

		/// <summary>
		/// Moves a specific item's RectTransform to the position corresponding to a given data index.
		/// </summary>
		/// <param name="item">The RectTransform of the item to move.</param>
		/// <param name="index">The data index this item should represent.</param>
		private void moveItemByIndex(RectTransform item, int index)
		{
			item.anchoredPosition3D = m_startPos + (m_offsetVec * index * m_prefabSize);
		}

		/// <summary>
		/// Gets the list of currently active (pooled) item scripts.
		/// </summary>
		/// <returns>A list of OptimizedScrollItem components.</returns>
		public List<OptimizedScrollItem> GetListItem()
		{
			return m_itemsScrolled;
		}
	}
}