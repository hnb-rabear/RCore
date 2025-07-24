﻿/**
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
	/// An advanced optimized vertical scroll view that recycles its items.
	/// It supports multi-column grid layouts and can automatically adjust its height
	/// based on the content, within specified min/max limits.
	/// </summary>
	public class OptimizedVerticalScrollView : MonoBehaviour
	{
		/// <summary>
		/// An action invoked whenever the scroll content is updated (e.g., on scroll).
		/// </summary>
		public Action onContentUpdated;
		/// <summary>
		/// The main ScrollRect component.
		/// </summary>
		public ScrollRect scrollView;
		/// <summary>
		/// The RectTransform that holds the scrollable content.
		/// </summary>
		public RectTransform container;
		/// <summary>
		/// The prefab for a single item in the scroll view.
		/// </summary>
		public OptimizedScrollItem prefab;
		/// <summary>
		/// The total number of items in the list.
		/// </summary>
		public int total = 1;
		/// <summary>
		/// The vertical spacing between rows.
		/// </summary>
		public float spacing;
		/// <summary>
		/// The number of cells (columns) in each row. Set to 1 for a simple vertical list.
		/// </summary>
		public int totalCellOnRow = 1;
		/// <summary>
		/// A public getter for the ScrollRect's content.
		/// </summary>
		public RectTransform content => scrollView.content;

		/// <summary>
		/// The number of visible rows.
		/// </summary>
		private int m_totalVisible;
		/// <summary>
		/// The number of buffer rows to instantiate above and below the visible area.
		/// </summary>
		private int m_totalBuffer = 2;
		/// <summary>
		/// Half of the container's height, used for positioning calculations.
		/// </summary>
		private float m_halfSizeContainer;
		/// <summary>
		/// The height of a single cell row (prefab height + spacing).
		/// </summary>
		private float m_cellSizeY;
		/// <summary>
		/// The width of a single cell column (prefab width + spacing).
		/// </summary>
		private float m_prefabSizeX;

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
		private int m_optimizedTotal;
		/// <summary>
		/// The starting position for the first item.
		/// </summary>
		private Vector3 m_startPos;
		/// <summary>
		/// A vector representing the direction of scrolling (Vector3.down).
		/// </summary>
		private Vector3 m_offsetVec;
		/// <summary>
		/// The pivot of the item prefab.
		/// </summary>
		private Vector2 m_pivot;

		[Separator("Advanced Settings")]
		/// <summary>
		/// If true, the height of the ScrollRect's viewport will be adjusted to match the content height.
		/// </summary>
		public bool autoMatchHeight;
		/// <summary>
		/// The minimum height for the viewport if autoMatchHeight is true.
		/// </summary>
		public float minViewHeight;
		/// <summary>
		/// The maximum height for the viewport if autoMatchHeight is true.
		/// </summary>
		public float maxViewHeight;

		private void Start()
		{
			// Subscribe to the scroll view's value changed event.
			scrollView.onValueChanged.AddListener(ScrollBarChanged);
		}

		/// <summary>
		/// Initializes the scroll view with a specific prefab and total item count, then scrolls to a start index.
		/// </summary>
		/// <param name="pPrefab">The item prefab.</param>
		/// <param name="pTotalItems">The total number of items in the list.</param>
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
		/// handles grid layout, and positions the initial items.
		/// </summary>
		/// <param name="pTotalItems">The total number of items in the list.</param>
		/// <param name="pForce">If true, forces re-initialization even if the total item count hasn't changed.</param>
		/// <param name="startIndex">The index to scroll to after initialization. If 0, scrolls to top.</param>
		public void Init(int pTotalItems, bool pForce, int startIndex = 0)
		{
			if (pTotalItems == total && !pForce)
				return;

			m_totalBuffer = 2;
			m_itemsRect = new List<RectTransform>();

			// Initialize or reset the item pool.
			if (m_itemsScrolled == null || m_itemsScrolled.Count == 0)
			{
				m_itemsScrolled = new List<OptimizedScrollItem>();
				m_itemsScrolled.Prepare(prefab, container.parent, 5);
			}
			else
				m_itemsScrolled.Free(container);

			total = pTotalItems;
			container.anchoredPosition3D = Vector3.zero;

			// Calculate cell and container sizes based on a grid layout.
			var rectZero = m_itemsScrolled[0].GetComponent<RectTransform>();
			var prefabScale = rectZero.rect.size;
			m_cellSizeY = prefabScale.y + spacing;
			m_prefabSizeX = (totalCellOnRow > 1) ? (prefabScale.x + spacing) : prefabScale.x;
			m_pivot = rectZero.pivot;
			container.sizeDelta = new Vector2(m_prefabSizeX * totalCellOnRow, m_cellSizeY * Mathf.CeilToInt(total * 1f / totalCellOnRow));
			m_halfSizeContainer = container.rect.size.y * 0.5f;

			var scrollRect = scrollView.transform as RectTransform;

			// Auto-adjust the viewport height if enabled.
			if (autoMatchHeight)
			{
				float preferHeight = container.rect.size.y + spacing * 2;
				preferHeight = Mathf.Clamp(preferHeight, minViewHeight, maxViewHeight > 0 ? maxViewHeight : preferHeight);
				var size = scrollRect.rect.size;
				size.y = preferHeight;
				scrollRect.sizeDelta = size;
			}

			// Determine number of visible and buffered items.
			var viewport = scrollView.viewport;
			m_totalVisible = Mathf.CeilToInt(viewport.rect.size.y / m_cellSizeY) * totalCellOnRow;
			m_totalBuffer *= totalCellOnRow;

			// Determine starting position and the number of items to instantiate.
			m_offsetVec = Vector3.down;
			m_startPos = container.anchoredPosition3D - m_offsetVec * m_halfSizeContainer + m_offsetVec * (prefabScale.y * 0.5f);
			m_optimizedTotal = Mathf.Min(total, m_totalVisible + m_totalBuffer);
			
			// Instantiate and position the initial set of items in a grid.
			for (int i = 0; i < m_optimizedTotal; i++)
			{
				var item = m_itemsScrolled.Obtain(container);
				var rt = item.transform as RectTransform;
				MoveItemByIndex(rt, i); // Use helper to position item
				m_itemsRect.Add(rt);
				item.gameObject.SetActive(true);
				item.UpdateContent(i, true);
			}

			// Deactivate original prefab and set initial scroll position.
			prefab.gameObject.SetActive(false);
			if (startIndex <= 0)
				container.anchoredPosition3D += m_offsetVec * (m_halfSizeContainer - viewport.rect.size.y * 0.5f) + new Vector3(0, m_cellSizeY, 0);
			else
				ScrollToIndex(startIndex);
		}

#if ODIN_INSPECTOR
		[Button]
#endif
		/// <summary>
		/// Scrolls the view to the top.
		/// </summary>
		/// <param name="tween">If true, animates the scroll using DOTween.</param>
		public void ScrollToTop(bool tween = false)
		{
			scrollView.StopMovement();
			if (tween)
			{
#if DOTWEEN
				float fromY = scrollView.normalizedPosition.y;
				float toY = 1f;
				if (fromY != toY)
				{
					float time = Mathf.Abs(toY - fromY);
					if (time < 0.1f && time > 0)
						time = 0.1f;
					float val = fromY;
					DOTween.To(() => val, x => val = x, toY, time)
						.OnUpdate(() => scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, val));
				}
#else
				scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, 1);
#endif
			}
			else
			{
				scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, 1);
			}
		}

#if ODIN_INSPECTOR
		[Button]
#endif
		/// <summary>
		/// Scrolls the view to the bottom.
		/// </summary>
		/// <param name="tween">If true, animates the scroll using DOTween.</param>
		public void ScrollToBot(bool tween = false)
		{
			scrollView.StopMovement();
			if (tween)
			{
#if DOTWEEN
				float fromY = scrollView.normalizedPosition.y;
				float toY = 0f;
				if (fromY != toY)
				{
					float time = Mathf.Abs(toY - fromY);
					if (time < 0.1f && time > 0)
						time = 0.1f;
					float val = fromY;
					DOTween.To(() => val, x => val = x, toY, time)
						.OnUpdate(() => scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, val));
				}
#else
				scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, 0);
#endif
			}
			else
			{
				scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, 0);
			}
		}
		
		/// <summary>
		/// This is the core logic for the optimized scroll view.
		/// It is called whenever the scrollbar's value changes. It calculates which items should be visible
		/// and repositions/recycles the instantiated items to represent the correct data in a grid layout.
		/// </summary>
		/// <param name="pNormPos">The current normalized position of the scrollbar (0 to 1).</param>
		private void ScrollBarChanged(Vector2 pNormPos)
		{
			if (m_optimizedTotal <= 0)
				return;
				
			// Vertical scrollbar value is inverted (1 is top, 0 is bottom), so we invert it for calculations.
			pNormPos.y = 1f - pNormPos.y;
			// A small offset is added for multi-column grids to improve scrolling feel.
			if (totalCellOnRow > 1)
				pNormPos.y += 0.06f;

			pNormPos.y = Mathf.Clamp01(pNormPos.y);

			// Calculate the viewport bounds for visibility checks.
			var viewport = scrollView.viewport;
			var viewportCorners = new Vector3[4];
			viewport.GetWorldCorners(viewportCorners);
			var viewportRect = new Rect(viewportCorners[0], viewportCorners[2] - viewportCorners[0]);

			// Calculate the index of the first item that should be in the buffer zone.
			int numOutOfView = Mathf.CeilToInt(pNormPos.y * (total - m_totalVisible));
			int firstIndex = Mathf.Max(0, numOutOfView - m_totalBuffer);
			// Determine which of our pooled items corresponds to this new first index.
			int originalIndex = firstIndex % m_optimizedTotal;
			
			// Reposition and update the content of the pooled items based on the new scroll position.
			int newIndex = firstIndex;
			for (int i = originalIndex; i < m_optimizedTotal; i++)
			{
				MoveItemByIndex(m_itemsRect[i], newIndex);
				m_itemsScrolled[i].UpdateContent(newIndex);
				m_itemsScrolled[i].visible = IsItemVisible(viewportRect, i);
				newIndex++;
			}
			for (int i = 0; i < originalIndex; i++)
			{
				MoveItemByIndex(m_itemsRect[i], newIndex);
				m_itemsScrolled[i].UpdateContent(newIndex);
				m_itemsScrolled[i].visible = IsItemVisible(viewportRect, i);
				newIndex++;
			}
			onContentUpdated?.Invoke();
		}

		/// <summary>
		/// Iterates through all active items and checks if they are currently inside the viewport.
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

		/// <summary>
		/// Checks if a specific item is overlapping with the viewport's rectangle.
		/// </summary>
		/// <param name="viewportRect">The viewport rectangle in world space.</param>
		/// <param name="index">The index of the pooled item to check.</param>
		/// <returns>True if the item is visible, otherwise false.</returns>
		private bool IsItemVisible(Rect viewportRect, int index)
		{
			var itemCorners = new Vector3[4];
			m_itemsRect[index].GetWorldCorners(itemCorners);
			var itemRect = new Rect(itemCorners[0], itemCorners[2] - itemCorners[0]);
			return viewportRect.Overlaps(itemRect);
		}

		/// <summary>
		/// Moves a specific item's RectTransform to the position corresponding to a given data index,
		/// arranging it in a grid based on its row and column.
		/// </summary>
		/// <param name="item">The RectTransform of the item to move.</param>
		/// <param name="index">The data index this item should represent.</param>
		private void MoveItemByIndex(RectTransform item, int index)
		{
			int cellIndex = index % totalCellOnRow;
			int rowIndex = Mathf.FloorToInt(index * 1f / totalCellOnRow);
			item.anchoredPosition3D = m_startPos + m_offsetVec * rowIndex * m_cellSizeY;
			item.anchoredPosition3D = new Vector3(-container.rect.size.x / 2 + cellIndex * m_prefabSizeX + m_prefabSizeX * 0.5f,
				item.anchoredPosition3D.y,
				item.anchoredPosition3D.z);
		}

		/// <summary>
		/// Gets the list of currently active (pooled) item scripts.
		/// </summary>
		/// <returns>A list of OptimizedScrollItem components.</returns>
		public List<OptimizedScrollItem> GetListItem() => m_itemsScrolled;

#if ODIN_INSPECTOR
		[Button]
#endif
		/// <summary>
		/// Scrolls the view to make a specific item index visible.
		/// </summary>
		/// <param name="pIndex">The index of the item to scroll to.</param>
		/// <param name="pTween">If true, animates the scroll using DOTween.</param>
		/// <param name="pOnComplete">An optional action to invoke when the scroll completes.</param>
		public void ScrollToIndex(int pIndex, bool pTween = false, Action pOnComplete = null)
		{
			pIndex = Mathf.Clamp(pIndex, 0, total - 1);
			int rowIndex = Mathf.FloorToInt(pIndex * 1f / totalCellOnRow);
			// Calculate the target normalized position.
			float toY = rowIndex * m_cellSizeY / (container.rect.size.y - scrollView.viewport.rect.size.y);
			toY = Mathf.Clamp01(toY);

			if (pTween)
			{
#if DOTWEEN
				float fromY = 1 - scrollView.normalizedPosition.y;
				if (toY != fromY)
				{
					float time = Mathf.Abs(toY - fromY) * 2;
					if (time < 0.1f && time > 0)
						time = 0.1f;
					float val = fromY;
					DOTween.To(() => val, x => val = x, toY, time)
						.OnUpdate(() =>
						{
							scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, 1f - val);
						})
						.OnComplete(() =>
						{
							pOnComplete?.Invoke();
						});
				}
#else
				// Fallback to instant scroll if DOTween is not available.
				scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, 1f - toY);
				pOnComplete?.Invoke();
				ScrollBarChanged(scrollView.normalizedPosition);
#endif
			}
			else
			{
				// Instant scroll.
				scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, 1f - toY);
				pOnComplete?.Invoke();
				ScrollBarChanged(scrollView.normalizedPosition);
			}
		}
#if ODIN_INSPECTOR
		[Button]
#endif
		/// <summary>
		/// A test method, available in the editor, to demonstrate scrolling between two indices.
		/// </summary>
		public void TestMoveItemToIndex(int a, int b)
		{
			StartCoroutine(MoveItemToIndex(a, b));
		}

		/// <summary>
		/// A coroutine that animates scrolling first to index 'a', then to index 'b'.
		/// </summary>
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