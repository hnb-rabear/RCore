/**
 * Author HNB-RaBear - 2017
 **/

#if DOTWEEN
using DG.Tweening;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RCore.Inspector;
using System;
using System.Collections;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace RCore.UI
{
	/// <summary>
	/// Provides an optimized solution for displaying a large number of items in a vertical scroll view
	/// without creating a GameObject for every single item. It uses a recycling pattern (also known as virtualization)
	/// where a small pool of visible item objects are created and their content is updated as the user scrolls.
	/// This dramatically improves performance for long lists.
	/// </summary>
	public class OptimizedVerticalScrollView : MonoBehaviour
	{
		#region Public Fields

		/// <summary>An action that is invoked whenever the content items are updated due to scrolling.</summary>
		public Action onContentUpdated;
		/// <summary>The main ScrollRect component that this script controls.</summary>
		[Tooltip("The main ScrollRect component that this script controls.")]
		public ScrollRect scrollView;
		/// <summary>The RectTransform that holds the visible, recycled item GameObjects. This should be a child of the ScrollRect's content.</summary>
		[Tooltip("The RectTransform that holds the visible, recycled item GameObjects. This should be a child of the ScrollRect's content.")]
		public RectTransform container;
		/// <summary>The prefab for a single item in the scroll view. It must have an OptimizedScrollItem component.</summary>
		[Tooltip("The prefab for a single item in the scroll view. It must have an OptimizedScrollItem component.")]
		public OptimizedScrollItem prefab;
		/// <summary>The total number of items in the virtual list.</summary>
		[Tooltip("The total number of items in the virtual list.")]
		public int total = 1;
		/// <summary>The vertical and horizontal spacing between items.</summary>
		[Tooltip("The vertical and horizontal spacing between items.")]
		public float spacing;
		/// <summary>The number of cells per row, allowing for a grid layout. Set to 1 for a simple vertical list.</summary>
		[Tooltip("The number of cells per row, allowing for a grid layout. Set to 1 for a simple vertical list.")]
		public int totalCellOnRow = 1;
		/// <summary>Read-only access to the ScrollRect's content RectTransform.</summary>
		public RectTransform content => scrollView.content;
		
		[Separator("Advanced Settings")]
		/// <summary>If true, the height of the ScrollRect's viewport will be automatically adjusted to fit the content, within the min/max limits.</summary>
		[Tooltip("If true, the height of the ScrollRect's viewport will be automatically adjusted to fit the content, within the min/max limits.")]
		public bool autoMatchHeight;
		/// <summary>The minimum height of the viewport when autoMatchHeight is true.</summary>
		[Tooltip("The minimum height of the viewport when autoMatchHeight is true.")]
		public float minViewHeight;
		/// <summary>The maximum height of the viewport when autoMatchHeight is true.</summary>
		[Tooltip("The maximum height of the viewport when autoMatchHeight is true.")]
		public float maxViewHeight;

		#endregion

		#region Private Fields

		private int m_totalVisible;
		private int m_totalBuffer = 2;
		private float m_halfSizeContainer;
		private float m_cellSizeY;
		private float m_prefabSizeX;

		private List<RectTransform> m_itemsRect = new List<RectTransform>();
		private List<OptimizedScrollItem> m_itemsScrolled = new List<OptimizedScrollItem>();
		private int m_optimizedTotal;
		private Vector3 m_startPos;
		private Vector3 m_offsetVec;
		private Vector2 m_pivot;
		
		#endregion

		private void Start()
		{
			if (scrollView != null)
				scrollView.onValueChanged.AddListener(ScrollBarChanged);
		}
		
		private void LateUpdate()
		{
			// Manually call update on the visible items.
			// This can be useful if an item's logic needs to run every frame.
			for (int i = 0; i < m_itemsScrolled.Count; i++)
				m_itemsScrolled[i].ManualUpdate();
		}
		
		/// <summary>
		/// Initializes or re-initializes the scroll view with a new prefab and total item count.
		/// </summary>
		/// <param name="pPrefab">The new item prefab.</param>
		/// <param name="pTotalItems">The new total number of items.</param>
		/// <param name="pForce">If true, forces re-initialization even if the total item count hasn't changed.</param>
		/// <param name="startIndex">The index to scroll to after initialization.</param>
		public void Init(OptimizedScrollItem pPrefab, int pTotalItems, bool pForce, int startIndex)
		{
			prefab = pPrefab;
			m_itemsScrolled.Free();
			for (int i = 0; i < m_itemsScrolled.Count; i++)
				m_itemsScrolled[i].Refresh();

			Init(pTotalItems, pForce, startIndex);
		}
		
		/// <summary>
		/// Initializes or re-initializes the scroll view. Calculates content size and creates the initial pool of visible items.
		/// </summary>
		/// <param name="pTotalItems">The total number of items the scroll view should represent.</param>
		/// <param name="pForce">If true, forces re-initialization even if the total item count hasn't changed.</param>
		/// <param name="startIndex">The index to scroll to after initialization. Defaults to 0 (top).</param>
		public void Init(int pTotalItems, bool pForce, int startIndex = 0)
		{
			if (pTotalItems == total && !pForce)
				return;
				
			if (prefab == null)
			{
				Debug.LogError("OptimizedVerticalScrollView: Prefab is not assigned.");
				return;
			}

			m_totalBuffer = 2;
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
			var prefabScale = rectZero.rect.size;
			m_cellSizeY = prefabScale.y + spacing;
			m_prefabSizeX = (totalCellOnRow > 1) ? (prefabScale.x + spacing) : prefabScale.x;
			m_pivot = rectZero.pivot;

			// Calculate and set the total size of the scrollable content area
			container.sizeDelta = new Vector2(m_prefabSizeX * totalCellOnRow, m_cellSizeY * Mathf.CeilToInt(total * 1f / totalCellOnRow));
			m_halfSizeContainer = container.rect.size.y * 0.5f;

			var scrollRectTransform = scrollView.transform as RectTransform;

			// Auto-adjust the viewport height if enabled
			if (autoMatchHeight)
			{
				float preferHeight = container.rect.size.y + spacing * 2;
				if (maxViewHeight > 0 && preferHeight > maxViewHeight)
					preferHeight = maxViewHeight;
				else if (minViewHeight > 0 && preferHeight < minViewHeight)
					preferHeight = minViewHeight;

				var size = scrollRectTransform.rect.size;
				size.y = preferHeight;
				scrollRectTransform.sizeDelta = size;
			}

			// Calculate how many items need to be instantiated
			var viewport = scrollView.viewport;
			m_totalVisible = Mathf.CeilToInt(viewport.rect.size.y / m_cellSizeY) * totalCellOnRow;
			m_totalBuffer *= totalCellOnRow;
			m_optimizedTotal = Mathf.Min(total, m_totalVisible + m_totalBuffer);
			
			// Calculate the starting position for the first item
			m_offsetVec = Vector3.down;
			m_startPos = container.anchoredPosition3D - m_offsetVec * m_halfSizeContainer + m_offsetVec * (prefabScale.y * 0.5f);

			// Instantiate and position the initial set of visible items
			for (int i = 0; i < m_optimizedTotal; i++)
			{
				var item = m_itemsScrolled.Obtain(container);
				var rt = item.transform as RectTransform;
				MoveItemByIndex(rt, i); // Position the new item
				m_itemsRect.Add(rt);
				item.gameObject.SetActive(true);
				item.UpdateContent(i, true);
			}

			prefab.gameObject.SetActive(false);
			
			if (startIndex <= 0)
			{
				// Adjust initial position to be at the top
				container.anchoredPosition3D += m_offsetVec * (m_halfSizeContainer - viewport.rect.size.y * 0.5f) + new Vector3(0, m_cellSizeY, 0);
			}
			else
			{
				ScrollToIndex(startIndex);
			}
		}

		#region Scrolling Control

#if ODIN_INSPECTOR
		[Button]
#endif
		/// <summary>
		/// Scrolls the view to the very top.
		/// </summary>
		/// <param name="tween">If true, animates the scroll using DOTween. If false, jumps instantly.</param>
		public void ScrollToTop(bool tween = false)
		{
			ScrollToNormalizedPosition(1f, tween);
		}

#if ODIN_INSPECTOR
		[Button]
#endif
		/// <summary>
		/// Scrolls the view to the very bottom.
		/// </summary>
		/// <param name="tween">If true, animates the scroll using DOTween. If false, jumps instantly.</param>
		public void ScrollToBot(bool tween = false)
		{
			ScrollToNormalizedPosition(0f, tween);
		}

		/// <summary>
		/// Scrolls the view to a specific item index.
		/// </summary>
		/// <param name="pIndex">The index of the item to scroll to.</param>
		/// <param name="pTween">If true, animates the scroll using DOTween. If false, jumps instantly.</param>
		/// <param name="pOnComplete">An action to invoke when the scroll completes.</param>
		public void ScrollToIndex(int pIndex, bool pTween = false, Action pOnComplete = null)
		{
			pIndex = Mathf.Clamp(pIndex, 0, total - 1);
			int rowIndex = Mathf.FloorToInt(pIndex * 1f / totalCellOnRow);
			
			float contentHeight = container.rect.size.y;
			float viewportHeight = scrollView.viewport.rect.size.y;

			if (contentHeight <= viewportHeight)
			{
				// If content is smaller than the view, normalized position doesn't work well.
				// We can just stay at the top.
				ScrollToNormalizedPosition(1f, pTween, pOnComplete);
				return;
			}
			
			float targetY = rowIndex * m_cellSizeY;
			// Convert the target position to a normalized value (0-1)
			float toY = targetY / (contentHeight - viewportHeight);
			toY = Mathf.Clamp01(toY);

			// Normalized position is 1 at the top and 0 at the bottom, so we invert our calculation.
			ScrollToNormalizedPosition(1f - toY, pTween, pOnComplete);
		}

		private void ScrollToNormalizedPosition(float pNormY, bool pTween, Action pOnComplete = null)
		{
			scrollView.StopMovement();
			pNormY = Mathf.Clamp01(pNormY);
			
			if (pTween)
			{
#if DOTWEEN
				float fromY = scrollView.normalizedPosition.y;
				if (Mathf.Approximately(fromY, pNormY))
				{
					pOnComplete?.Invoke();
					return;
				}
				
				float time = Mathf.Abs(pNormY - fromY) * 2; // Duration based on distance
				time = Mathf.Max(0.1f, time); // Minimum duration
				
				DOTween.To(() => scrollView.normalizedPosition, x => scrollView.normalizedPosition = x, new Vector2(scrollView.normalizedPosition.x, pNormY), time)
					.SetEase(Ease.OutCubic)
					.OnComplete(() => pOnComplete?.Invoke());
#else
				scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, pNormY);
				pOnComplete?.Invoke();
				ScrollBarChanged(scrollView.normalizedPosition);
#endif
			}
			else
			{
				scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, pNormY);
				pOnComplete?.Invoke();
				// Manually call to update view instantly
				ScrollBarChanged(scrollView.normalizedPosition);
			}
		}

		#endregion

		/// <summary>
		/// The core logic for recycling items. This is called when the scrollbar value changes.
		/// It calculates the new position of the virtual items and repositions the pooled GameObjects,
		/// then updates their content.
		/// </summary>
		private void ScrollBarChanged(Vector2 pNormPos)
		{
			if (m_optimizedTotal <= 0 || total <= 0)
				return;

			// Normalized position is 1 at the top, 0 at the bottom. We want to work with 0 at the top.
			float normY = 1f - pNormPos.y;

			// Calculate how many full rows are scrolled out of view at the top.
			// This determines the index of the first item that should be in our data set.
			int numRowsOutOfView = Mathf.FloorToInt(normY * (Mathf.Ceil(total * 1f / totalCellOnRow) - m_totalVisible / (float)totalCellOnRow));
			int firstIndex = Mathf.Max(0, (numRowsOutOfView * totalCellOnRow) - m_totalBuffer);
			
			// Determine which recycled item corresponds to the start of the list now
			int originalIndex = firstIndex % m_optimizedTotal;
			if(originalIndex < 0) originalIndex += m_optimizedTotal;

			int newIndex = firstIndex;
			// Reposition and update content for the recycled items
			for (int i = originalIndex; i < m_optimizedTotal; i++)
			{
				if (newIndex >= total) break;
				MoveItemByIndex(m_itemsRect[i], newIndex);
				m_itemsScrolled[i].UpdateContent(newIndex);
				newIndex++;
			}
			for (int i = 0; i < originalIndex; i++)
			{
				if (newIndex >= total) break;
				MoveItemByIndex(m_itemsRect[i], newIndex);
				m_itemsScrolled[i].UpdateContent(newIndex);
				newIndex++;
			}
			
			onContentUpdated?.Invoke();
			CheckItemsInViewPort();
		}

		/// <summary>
		/// Iterates through the visible items and updates their 'visible' property based on whether
		/// they are currently inside the scroll view's viewport.
		/// </summary>
		private void CheckItemsInViewPort()
		{
			var viewport = scrollView.viewport;

			var viewportCorners = new Vector3[4];
			viewport.GetWorldCorners(viewportCorners);
			var viewportRect = new Rect(viewportCorners[0], viewportCorners[2] - viewportCorners[0]);

			for (var i = 0; i < m_itemsRect.Count; i++)
				m_itemsScrolled[i].visible = IsItemVisible(viewportRect, i);
		}

		private bool IsItemVisible(Rect viewportRect, int index)
		{
			var itemCorners = new Vector3[4];
			m_itemsRect[index].GetWorldCorners(itemCorners);
			var itemRect = new Rect(itemCorners[0], itemCorners[2] - itemCorners[0]);
			return viewportRect.Overlaps(itemRect);
		}
		
		/// <summary>
		/// Calculates and sets the anchoredPosition of a given item RectTransform based on its virtual index.
		/// </summary>
		/// <param name="item">The RectTransform of the item to move.</param>
		/// <param name="index">The virtual index of the item in the complete list.</param>
		private void MoveItemByIndex(RectTransform item, int index)
		{
			int cellIndex = index % totalCellOnRow;
			int rowIndex = Mathf.FloorToInt(index * 1f / totalCellOnRow);
			
			float posX = -container.rect.size.x / 2 + cellIndex * m_prefabSizeX + m_prefabSizeX * 0.5f;
			float posY = m_startPos.y + (rowIndex * m_cellSizeY) * m_offsetVec.y;
			
			item.anchoredPosition3D = new Vector3(posX, posY, m_startPos.z);
		}

		/// <summary>
		/// Returns the list of currently active (recycled) OptimizedScrollItem instances.
		/// </summary>
		public List<OptimizedScrollItem> GetListItem() => m_itemsScrolled;

#if ODIN_INSPECTOR
		[Button]
#endif
		/// <summary>(Editor-Only) A test function to demonstrate scrolling from one index to another.</summary>
		public void TestMoveItemToIndex(int a, int b)
		{
			StartCoroutine(MoveItemToIndex(a, b));
		}

		/// <summary>A coroutine to demonstrate scrolling from one index to another, used by the test function.</summary>
		public IEnumerator MoveItemToIndex(int a, int b)
		{
			if (a < 0 || a >= total || b < 0 || b >= total || a == b)
				yield break;

			bool wait = true;
			ScrollToIndex(a, true, () => wait = false);
			yield return new WaitUntil(() => !wait);
			ScrollToIndex(b, true);
		}
	}
}