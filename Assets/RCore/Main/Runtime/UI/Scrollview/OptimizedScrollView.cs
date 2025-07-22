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
	/// Provides a performance-optimized scroll view that recycles a small pool of UI elements
	/// to display a large virtual list. This avoids the high cost of instantiating a GameObject
	/// for every single item, making it suitable for long lists of data.
	/// This is a basic implementation that supports both horizontal and vertical scrolling.
	/// For more advanced features like grid layouts, see OptimizedVerticalScrollView.
	/// </summary>
	public class OptimizedScrollView : MonoBehaviour
	{
		#region Public Fields
		
		/// <summary>The main ScrollRect component that this script controls.</summary>
		[Tooltip("The main ScrollRect component that this script controls.")]
		public ScrollRect scrollView;
		/// <summary>The RectTransform that holds the visible, recycled item GameObjects. This should be the content of the ScrollRect.</summary>
		[Tooltip("The RectTransform that holds the visible, recycled item GameObjects. This should be the content of the ScrollRect.")]
		public RectTransform container;
		/// <summary>The UI Mask component that defines the visible area of the scroll view.</summary>
		[Tooltip("The UI Mask component that defines the visible area of the scroll view.")]
		public Mask mask;
		/// <summary>The prefab for a single item in the scroll view. It must have an OptimizedScrollItem component.</summary>
		[Tooltip("The prefab for a single item in the scroll view. It must have an OptimizedScrollItem component.")]
		public OptimizedScrollItem prefab;
		/// <summary>The total number of items in the virtual list.</summary>
		[Tooltip("The total number of items in the virtual list.")]
		public int total = 1;
		/// <summary>The spacing between each item.</summary>
		[Tooltip("The spacing between each item.")]
		public float spacing;
		/// <summary>The scrolling direction (Horizontal or Vertical).</summary>
		[Tooltip("The scrolling direction (Horizontal or Vertical).")]
		public ScrollDirection Direction = ScrollDirection.Horizontal;
		
		#endregion

		#region Private Fields
		
		private RectTransform m_maskRect;
		private int m_totalVisible;
		private int m_totalBuffer = 2; // Extra items on each side to prevent visual pop-ins during fast scrolls
		private float m_halfSizeContainer;
		private float m_prefabSize; // The size of one item plus spacing

		private List<RectTransform> m_itemsRect = new List<RectTransform>();
		private List<OptimizedScrollItem> m_itemsScrolled = new List<OptimizedScrollItem>();
		private int m_optimizedTotal = 0; // The actual number of GameObjects instantiated (visible items + buffer)
		private Vector3 m_startPos;
		private Vector3 m_offsetVec; // A directional vector (right or down) used for positioning
		
		#endregion

		private void Start()
		{
			// Subscribe to the appropriate scrollbar's value changed event
			if (Direction == ScrollDirection.Vertical)
				scrollView.verticalScrollbar.onValueChanged.AddListener(ScrollBarChanged);
			else if (Direction == ScrollDirection.Horizontal)
				scrollView.horizontalScrollbar.onValueChanged.AddListener(ScrollBarChanged);

			Initialize(total);
		}

		private void LateUpdate()
		{
			// Provide a manual update tick to all active items.
			// This can be used for per-frame logic within the OptimizedScrollItem script.
			for (int i = 0; i < m_itemsScrolled.Count; i++)
				m_itemsScrolled[i].ManualUpdate();
		}

		/// <summary>
		/// Initializes or re-initializes the scroll view. This sets up the content size,
		/// determines the number of items to pool, and instantiates them.
		/// </summary>
		/// <param name="pTotalItems">The total number of items the scroll view should represent.</param>
		public void Initialize(int pTotalItems)
		{
			if (pTotalItems == total && m_itemsScrolled.Count > 0)
				return;
			
			if (prefab == null)
			{
				Debug.LogError("OptimizedScrollView: Prefab is not assigned.");
				return;
			}

			m_itemsRect = new List<RectTransform>();

			// Prepare the object pool for the items
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

			// Calculate the size of a single cell (item + spacing)
			var prefabScale = m_itemsScrolled[0].GetComponent<RectTransform>().rect.size;
			m_prefabSize = (Direction == ScrollDirection.Horizontal ? prefabScale.x : prefabScale.y) + spacing;
			
			// Set the total size of the content container to simulate the full list
			container.sizeDelta = Direction == ScrollDirection.Horizontal ? (new Vector2(m_prefabSize * total, prefabScale.y)) : (new Vector2(prefabScale.x, m_prefabSize * total));
			m_halfSizeContainer = Direction == ScrollDirection.Horizontal ? (container.rect.size.x * 0.5f) : (container.rect.size.y * 0.5f);

			// Calculate how many items are visible at once and the total number to pool
			m_totalVisible = Mathf.CeilToInt((Direction == ScrollDirection.Horizontal ? m_maskRect.rect.size.x : m_maskRect.rect.size.y) / m_prefabSize);
			m_optimizedTotal = Mathf.Min(total, m_totalVisible + m_totalBuffer);
			
			// Determine the direction vector and calculate the starting position for the first item
			m_offsetVec = Direction == ScrollDirection.Horizontal ? Vector3.right : Vector3.down;
			m_startPos = container.anchoredPosition3D - (m_offsetVec * m_halfSizeContainer) + (m_offsetVec * ((Direction == ScrollDirection.Horizontal ? prefabScale.x : prefabScale.y) * 0.5f));
			
			// Instantiate and position the initial set of pooled items
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
			
			// Adjust the container's initial position to start at the beginning of the list
			container.anchoredPosition3D += m_offsetVec * (m_halfSizeContainer - ((Direction == ScrollDirection.Horizontal ? m_maskRect.rect.size.x : m_maskRect.rect.size.y) * 0.5f));
		}

		/// <summary>
		/// The core recycling logic, called when the scrollbar's value changes.
		/// It calculates the new virtual position and repositions/updates the pooled items.
		/// </summary>
		/// <param name="pNormPos">The normalized position of the scrollbar (0 to 1).</param>
		public void ScrollBarChanged(float pNormPos)
		{
			if (m_optimizedTotal == 0) return;
			
			// For vertical scroll, the normalized position is inverted (1 is top, 0 is bottom).
			if (Direction == ScrollDirection.Vertical)
				pNormPos = 1f - pNormPos;
			
			pNormPos = Mathf.Clamp01(pNormPos);

			// Calculate how many items are scrolled past the visible area's start
			int numOutOfView = Mathf.CeilToInt(pNormPos * (total - m_totalVisible));
			// Determine the index of the first item that should be represented by the pool
			int firstIndex = Mathf.Max(0, numOutOfView - m_totalBuffer);
			// Find which pooled item corresponds to this new first index using a modulo operation
			int originalIndex = firstIndex % m_optimizedTotal;

			// Reposition and update the content of all pooled items based on the new virtual starting index
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
		/// A helper method to calculate and set the anchored position of a pooled item.
		/// </summary>
		/// <param name="item">The RectTransform of the item to move.</param>
		/// <param name="index">The virtual index of the item in the complete list.</param>
		private void moveItemByIndex(RectTransform item, int index)
		{
			item.anchoredPosition3D = m_startPos + (m_offsetVec * index * m_prefabSize);
		}

		/// <summary>
		/// Returns the list of currently active (pooled) OptimizedScrollItem instances.
		/// </summary>
		/// <returns>A list of the active OptimizedScrollItem components.</returns>
		public List<OptimizedScrollItem> GetListItem()
		{
			return m_itemsScrolled;
		}
	}
}