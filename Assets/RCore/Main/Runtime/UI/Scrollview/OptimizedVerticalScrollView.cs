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
		/// <summary>
		/// Cached corner arrays to avoid GC allocations in scroll callbacks.
		/// </summary>
		private readonly Vector3[] m_viewportCorners = new Vector3[4];
		private readonly Vector3[] m_itemCorners = new Vector3[4];

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

#if DOTWEEN
		[Separator("Animation")]
		/// <summary>
		/// Delay before the item starts moving (allows user to see their original standing).
		/// </summary>
		public float animMoveDelay = 0.8f;

		/// <summary>
		/// Time in seconds it takes to travel ONE item slot.
		/// </summary>
		public float animMoveDurationPerItem = 0.2f;

		public float animMoveMinDuration = 1.0f;
		public float animMoveMaxDuration = 5.0f;
#endif

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
			m_itemsRect.Clear();

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
#if DOTWEEN
			TryFirePendingAnimRequest();
#endif
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
#if DOTWEEN
			DOTween.Kill(scrollView.GetInstanceID());
#endif
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
						.SetId(scrollView.GetInstanceID())
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
#if DOTWEEN
			DOTween.Kill(scrollView.GetInstanceID());
#endif
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
						.SetId(scrollView.GetInstanceID())
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

			// Calculate the viewport bounds for visibility checks (reuse cached arrays).
			var viewport = scrollView.viewport;
			viewport.GetWorldCorners(m_viewportCorners);
			var viewportRect = new Rect(m_viewportCorners[0], m_viewportCorners[2] - m_viewportCorners[0]);

			// Calculate the index of the first item that should be in the buffer zone.
			int numOutOfView = Mathf.CeilToInt(pNormPos.y * (total - m_totalVisible));
			int firstIndex = Mathf.Max(0, numOutOfView - m_totalBuffer);
			// Determine which of our pooled items corresponds to this new first index.
			int originalIndex = firstIndex % m_optimizedTotal;
			
			// Reposition and update the content of the pooled items based on the new scroll position.
			int newIndex = firstIndex;
			for (int i = originalIndex; i < m_optimizedTotal; i++)
			{
				if (newIndex >= total) break;
				MoveItemByIndex(m_itemsRect[i], newIndex);
				m_itemsScrolled[i].UpdateContent(newIndex);
				m_itemsScrolled[i].visible = IsItemVisible(viewportRect, i);
				SetItemAlphaForAnim(m_itemsScrolled[i], newIndex);
				newIndex++;
			}
			for (int i = 0; i < originalIndex; i++)
			{
				if (newIndex >= total) break;
				MoveItemByIndex(m_itemsRect[i], newIndex);
				m_itemsScrolled[i].UpdateContent(newIndex);
				m_itemsScrolled[i].visible = IsItemVisible(viewportRect, i);
				SetItemAlphaForAnim(m_itemsScrolled[i], newIndex);
				newIndex++;
			}
			onContentUpdated?.Invoke();
		}

		/// <summary>
		/// Checks if a specific item is overlapping with the viewport's rectangle.
		/// </summary>
		/// <param name="viewportRect">The viewport rectangle in world space.</param>
		/// <param name="index">The index of the pooled item to check.</param>
		/// <returns>True if the item is visible, otherwise false.</returns>
		private bool IsItemVisible(Rect viewportRect, int index)
		{
			m_itemsRect[index].GetWorldCorners(m_itemCorners);
			var itemRect = new Rect(m_itemCorners[0], m_itemCorners[2] - m_itemCorners[0]);
			return viewportRect.Overlaps(itemRect);
		}

		/// <summary>
		/// Gets the calculated local anchored position for an item at a specific data index.
		/// </summary>
		private Vector3 GetItemAnchoredPos(int index)
		{
			int cellIndex = index % totalCellOnRow;
			int rowIndex = Mathf.FloorToInt(index * 1f / totalCellOnRow);
			var rowPos = m_startPos + m_offsetVec * rowIndex * m_cellSizeY;
			return new Vector3(
				-container.rect.size.x / 2 + cellIndex * m_prefabSizeX + m_prefabSizeX * 0.5f,
				rowPos.y,
				rowPos.z);
		}

		/// <summary>
		/// Moves a specific item's RectTransform to the position corresponding to a given data index,
		/// arranging it in a grid based on its row and column.
		/// </summary>
		/// <param name="item">The RectTransform of the item to move.</param>
		/// <param name="index">The data index this item should represent.</param>
		private void MoveItemByIndex(RectTransform item, int index)
		{
			item.anchoredPosition3D = GetItemAnchoredPos(index);
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
		/// <param name="pOverrideDuration">If > 0, overrides the auto-calculated tween duration.</param>
#if DOTWEEN
		public void ScrollToIndex(int pIndex, bool pTween = false, Action pOnComplete = null, float pOverrideDuration = -1f, DG.Tweening.Ease pEase = DG.Tweening.Ease.OutQuad)
#else
		public void ScrollToIndex(int pIndex, bool pTween = false, Action pOnComplete = null, float pOverrideDuration = -1f)
#endif
		{
			pIndex = Mathf.Clamp(pIndex, 0, total - 1);
			int rowIndex = Mathf.FloorToInt(pIndex * 1f / totalCellOnRow);
			// Guard: if content fits within viewport, no scrolling needed.
			float scrollableHeight = container.rect.size.y - scrollView.viewport.rect.size.y;
			if (scrollableHeight <= 0)
			{
				pOnComplete?.Invoke();
				return;
			}
			
			// Calculate target normalized position to center the item in the viewport
			float viewHeight = scrollView.viewport != null ? scrollView.viewport.rect.size.y : GetComponent<RectTransform>().rect.size.y;
			float centerOffset = (viewHeight - m_cellSizeY) / 2f;
			float targetViewportTop = rowIndex * m_cellSizeY - centerOffset;
			
			float toY = targetViewportTop / scrollableHeight;
			toY = Mathf.Clamp01(toY);

			if (pTween)
			{
#if DOTWEEN
				DOTween.Kill(scrollView.GetInstanceID());
				float fromY = 1 - scrollView.normalizedPosition.y;
				if (toY != fromY)
				{
					float time = pOverrideDuration > 0 ? pOverrideDuration : Mathf.Abs(toY - fromY) * 2;
					if (time < 0.1f && time > 0)
						time = 0.1f;
					float val = fromY;
					var tw = DOTween.To(() => val, x => val = x, toY, time)
						.SetId(scrollView.GetInstanceID())
						.SetEase(pEase)
						.OnUpdate(() =>
						{
							scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, 1f - val);
						})
						.OnComplete(() =>
						{
							pOnComplete?.Invoke();
						});
				}
				else
				{
					pOnComplete?.Invoke();
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

#if ODIN_INSPECTOR
		[Button]
#endif
#if DOTWEEN
		/// <summary>
		/// Test the AnimateItemMove VFX in Play mode from the Inspector.
		/// Animates an item flying from index 'from' to index 'to' using the Smooth Scrolling Snapshot technique.
		/// </summary>
		public void TestAnimateItemMove(int from, int to)
		{
			AnimateItemMove(from, to, clone => { /* Custom config if needed */ });
		}
#endif

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

#if DOTWEEN
		//=========================================================================
		// Animate Item Move — Smooth Scrolling Snapshot VFX
		//=========================================================================

		private const int ANIM_MOVE_TWEEN_ID = 92701;
		private GameObject m_animClone;
		private Coroutine m_animCoroutine;
		private HashSet<int> m_hiddenIndicesForAnim = new HashSet<int>();

		private struct AnimRequest
		{
			public int from, to;
			public Action<GameObject> configureClone;
			public Action onComplete;
		}
		private AnimRequest? m_pendingAnimRequest;

		/// <summary>
		/// True if a rank-move animation is currently in progress.
		/// Use this to guard UI updates that should be paused during animation (e.g. footer show/hide).
		/// </summary>
		public bool IsAnimating => m_animCoroutine != null;

		/// <summary>
		/// Fired at the start of the animation (1 frame after AnimateItemMove begins, once layout is settled).
		/// Use to hide UI elements that overlap with the animating clone.
		/// </summary>
		public event Action onAnimationStarted;

		/// <summary>
		/// Fired when the animation finishes and cleanup is complete.
		/// Use to restore UI elements that were hidden during the animation.
		/// </summary>
		public event Action onAnimationCompleted;

		private void SetItemAlphaForAnim(OptimizedScrollItem item, int index)
		{
#if DOTWEEN
			if (m_hiddenIndicesForAnim.Contains(index))
			{
				if (!item.TryGetComponent(out CanvasGroup cg))
					cg = item.gameObject.AddComponent<CanvasGroup>();
				cg.alpha = 0f;
			}
			else if (item.TryGetComponent(out CanvasGroup cg2))
			{
				cg2.alpha = 1f;
			}
#endif
		}

		private void UpdateAllVisibleItemsAlpha()
		{
#if DOTWEEN
			foreach (var item in m_itemsScrolled)
			{
				SetItemAlphaForAnim(item, item.Index);
			}
#endif
		}

		/// <summary>
		/// Animates an item visually moving from one index position to another.
		/// Uses a Smooth Scrolling Snapshot technique: populates a clone card in local space,
		/// animates its anchored position from the start slot to the target slot, while
		/// simultaneously commanding the parent ScrollView to smooth scroll to the target slot.
		/// This prevents screen blinking and maintains perfect visual tracking.
		/// </summary>
		/// <param name="pFromIndex">The data index the item is moving from.</param>
		/// <param name="pToIndex">The data index the item is moving to.</param>
		/// <param name="pConfigureClone">Action to configure the clone (e.g. SetData) so it looks like the user's card.</param>
		/// <param name="pOnComplete">Optional callback invoked after the animation finishes.</param>
		public void AnimateItemMove(int pFromIndex, int pToIndex, Action<GameObject> pConfigureClone = null, Action pOnComplete = null)
		{
			if (pFromIndex == pToIndex || pFromIndex < 0 || pToIndex < 0 || pFromIndex >= total || pToIndex >= total)
			{
				pOnComplete?.Invoke();
				return;
			}

			StopAnimateItemMove();
			m_animCoroutine = StartCoroutine(IEAnimateItemMove(pFromIndex, pToIndex, pConfigureClone, pOnComplete));
		}

		/// <summary>
		/// Queues a rank-move animation to run as soon as the scroll view is both initialized and active.
		/// If already initialized and active, fires immediately (same as AnimateItemMove).
		/// If not yet initialized (e.g. called from BeforeShowing before Init), stores the request and
		/// auto-executes it at the end of the next Init() call or when the GameObject becomes active.
		/// Only one queued request is held at a time — calling again overwrites the previous.
		/// </summary>
		/// <param name="pFromIndex">The data index the item is moving from.</param>
		/// <param name="pToIndex">The data index the item is moving to.</param>
		/// <param name="pConfigureClone">Action to configure the clone's visual appearance.</param>
		/// <param name="pOnComplete">Optional callback invoked after the animation finishes.</param>
		public void QueueAnimateItemMove(int pFromIndex, int pToIndex, Action<GameObject> pConfigureClone = null, Action pOnComplete = null)
		{
			if (m_optimizedTotal > 0 && gameObject.activeInHierarchy)
				AnimateItemMove(pFromIndex, pToIndex, pConfigureClone, pOnComplete);
			else
				m_pendingAnimRequest = new AnimRequest { from = pFromIndex, to = pToIndex, configureClone = pConfigureClone, onComplete = pOnComplete };
		}

		private void TryFirePendingAnimRequest()
		{
			if (m_pendingAnimRequest.HasValue && m_optimizedTotal > 0 && gameObject.activeInHierarchy)
			{
				var req = m_pendingAnimRequest.Value;
				m_pendingAnimRequest = null;
				AnimateItemMove(req.from, req.to, req.configureClone, req.onComplete);
			}
		}

		/// <summary>
		/// Stops any in-progress AnimateItemMove animation and cleans up.
		/// Safe to call even if no animation is running.
		/// </summary>
		public void StopAnimateItemMove()
		{
			DOTween.Kill(ANIM_MOVE_TWEEN_ID);
			if (m_animClone != null)
			{
				Destroy(m_animClone);
				m_animClone = null;
			}
			if (m_animCoroutine != null)
			{
				StopCoroutine(m_animCoroutine);
				m_animCoroutine = null;
			}
			m_hiddenIndicesForAnim.Clear();
			UpdateAllVisibleItemsAlpha();
			
			// Failsafe unlock if stopped externally
			if (scrollView != null)
			{
				scrollView.vertical = true;
				scrollView.horizontal = totalCellOnRow > 1; // Assuming horizontal if grid, else just true/false based on standard
			}
		}

		private IEnumerator IEAnimateItemMove(int fromIndex, int toIndex, Action<GameObject> configureClone, Action onComplete)
		{
			// Lock scroll interactions so the user can't disrupt the tracking
			bool wasVertical = scrollView.vertical;
			bool wasHorizontal = scrollView.horizontal;
			scrollView.vertical = false;
			scrollView.horizontal = false;
			scrollView.StopMovement();

			// Yield 1 frame to ensure scrollview layout is fully built if called right after Init()
			yield return null;

			// Notify listeners that the animation is now starting (layout is ready, clone is about to spawn)
			onAnimationStarted?.Invoke();
			
			// Guarantee the viewport is perfectly snapped to the start position 
			// before we begin the delay, especially important if called externally (e.g., Inspector Test button)
			ScrollToIndex(fromIndex, false);

			// Get mathematical local positions of the slots
			Vector3 startAnchoredPos = GetItemAnchoredPos(fromIndex);
			Vector3 endAnchoredPos = GetItemAnchoredPos(toIndex);

			// Pre-spawn and configure the clone so it completely obscures the "from" slot during the visual delay
			var sourceItem = m_itemsScrolled.Count > 0 ? m_itemsScrolled[0] : prefab;
			if (sourceItem == null)
			{
				m_hiddenIndicesForAnim.Clear();
				UpdateAllVisibleItemsAlpha();
				
				// Unlock interactions immediately
				scrollView.vertical = wasVertical;
				scrollView.horizontal = wasHorizontal;
				
				// Must reset coroutine ref and fire completed event — same cleanup as normal exit
				m_animCoroutine = null;
				onAnimationCompleted?.Invoke();
				onComplete?.Invoke();
				yield break;
			}

			// Parent to container so it scrolls WITH the content seamlessly
			m_animClone = Instantiate(sourceItem.gameObject, container);
			m_animClone.SetActive(true);

			// Ask caller to configure it (e.g. inject user's LeaderboardRecordData)
			configureClone?.Invoke(m_animClone);

			// Position clone at start slot exactly
			var cloneRT = m_animClone.transform as RectTransform;
			cloneRT.anchoredPosition3D = startAnchoredPos;
			cloneRT.SetAsLastSibling(); // Draw on top of all other items

			// Hide the real items at both 'from' and 'to' slots during the delay
			m_hiddenIndicesForAnim.Add(fromIndex);
			m_hiddenIndicesForAnim.Add(toIndex);
			UpdateAllVisibleItemsAlpha();

			// Optional: Wait for the user to register their current position
			if (animMoveDelay > 0)
			{
				float t = 0;
				while (t < animMoveDelay)
				{
					t += Time.unscaledDeltaTime;
					yield return null;
				}
			}

			// The delay is over. Reveal the 'from' slot (the new owner of the rank takes their spot underneath the flying clone)
			m_hiddenIndicesForAnim.Remove(fromIndex);
			UpdateAllVisibleItemsAlpha();

			// Calculate dynamic duration based on physical node distance
			int distance = Mathf.Abs(toIndex - fromIndex);
			float calculatedDuration = Mathf.Clamp(distance * animMoveDurationPerItem, animMoveMinDuration, animMoveMaxDuration);
			float duration = calculatedDuration;

			// Synchronize easing for both movement and scrolling to ensure perfect visual framing at all times
			var syncEase = Ease.InOutCubic;

			// Animate clone moving to end slot in local space
			bool animDone = false;
			var seq = DOTween.Sequence().SetId(ANIM_MOVE_TWEEN_ID).SetUpdate(true);
			seq.Append(cloneRT.DOLocalMove(endAnchoredPos, duration).SetEase(syncEase));
			seq.Join(cloneRT.DOPunchScale(Vector3.one * 0.12f, duration, 1, 0f));
			seq.OnComplete(() => animDone = true);

			// Smooth scroll the camera viewport to destination at the identical speed and Ease
			ScrollToIndex(toIndex, true, null, duration, syncEase);

			yield return new WaitUntil(() => animDone);

			// Reveal real target
			m_hiddenIndicesForAnim.Remove(toIndex);
			UpdateAllVisibleItemsAlpha();

			// Pop the real target to emphasize the landing
			var targetItem = FindItem(toIndex);
			if (targetItem != null)
			{
				int popId = targetItem.GetInstanceID() + 200;
				DOTween.Kill(popId);
				targetItem.transform.DOPunchScale(Vector3.one * 0.08f, 0.3f, 2, 0f)
					.SetUpdate(true)
					.SetId(popId)
					.OnComplete(() =>
					{
						if (targetItem != null)
							targetItem.transform.localScale = Vector3.one;
					});
			}

			if (m_animClone != null)
			{
				Destroy(m_animClone);
				m_animClone = null;
			}
			m_animCoroutine = null;

			// Unlock interactions
			scrollView.vertical = wasVertical;
			scrollView.horizontal = wasHorizontal;

			// Notify listeners before invoking the direct callback
			onAnimationCompleted?.Invoke();
			onComplete?.Invoke();
		}

		private OptimizedScrollItem FindItem(int targetIndex)
		{
			for (int i = 0; i < m_itemsScrolled.Count; i++)
			{
				var item = m_itemsScrolled[i];
				if (item.Index == targetIndex)
					return item;
			}
			return null;
		}

		private void OnEnable()
		{
			TryFirePendingAnimRequest();
		}

		private void OnDisable()
		{
			StopAnimateItemMove();
		}
#endif
	}
}