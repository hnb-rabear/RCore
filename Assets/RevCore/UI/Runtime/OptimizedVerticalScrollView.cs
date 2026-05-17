#if DOTWEEN
using DG.Tweening;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore.UI
{
    /// <summary>
    /// Virtualized vertical scroll view supporting multi-column grids (<see cref="totalCellOnRow"/> > 1)
    /// and optional auto-sizing of the viewport between min/max bounds. With DOTWEEN, programmatic
    /// scroll calls can animate over a duration scaled by the distance.
    /// </summary>
    public class OptimizedVerticalScrollView : MonoBehaviour
    {
        /// <summary>Invoked when the content set changes (Init / Expand / programmatic refresh).</summary>
        public Action onContentUpdated;
        /// <summary>The underlying ScrollRect.</summary>
        public ScrollRect scrollView;
        /// <summary>Content container whose height grows to the virtual total.</summary>
        public RectTransform container;
        /// <summary>Prefab cloned for items.</summary>
        public OptimizedScrollItem prefab;
        /// <summary>Total virtual item count.</summary>
        public int total = 1;
        /// <summary>Vertical spacing between items.</summary>
        public float spacing;
        /// <summary>Items per row. Set > 1 for grid layouts.</summary>
        public int totalCellOnRow = 1;

        /// <summary>Shortcut to <c>scrollView.content</c>.</summary>
        public RectTransform content => scrollView.content;

        private int m_totalVisible;
        private int m_totalBuffer = 2;
        private float m_halfSizeContainer;
        private float m_cellSizeY;
        private float m_prefabSizeX;
        private List<RectTransform> m_itemsRect = new();
        private List<OptimizedScrollItem> m_itemsScrolled = new();
        private int m_optimizedTotal;
        private Vector3 m_startPos;
        private Vector3 m_offsetVec;
        private Vector2 m_pivot;
        private readonly Vector3[] m_viewportCorners = new Vector3[4];
        private readonly Vector3[] m_itemCorners = new Vector3[4];

        /// <summary>When true, the viewport height clamps to the content height (within min/max bounds).</summary>
        [Separator("Advanced Settings")]
        public bool autoMatchHeight;
        /// <summary>Minimum auto-matched viewport height.</summary>
        public float minViewHeight;
        /// <summary>Maximum auto-matched viewport height. 0 = unlimited.</summary>
        public float maxViewHeight;

#if DOTWEEN
        /// <summary>Delay before <see cref="MoveItemToIndex"/> animation begins.</summary>
        [Separator("Animation")]
        public float animMoveDelay = 0.8f;
        /// <summary>Per-item animation duration multiplier — total = items * this.</summary>
        public float animMoveDurationPerItem = 0.2f;
        /// <summary>Floor on animation duration regardless of distance.</summary>
        public float animMoveMinDuration = 1.0f;
        /// <summary>Ceiling on animation duration regardless of distance.</summary>
        public float animMoveMaxDuration = 5.0f;
#endif

        private void Start()
        {
            scrollView.onValueChanged.AddListener(ScrollBarChanged);
        }

        /// <summary>Assigns a new prefab then forwards to <see cref="Init(int, bool, int)"/>.</summary>
        public void Init(OptimizedScrollItem itemPrefab, int totalItems, bool force, int startIndex)
        {
            prefab = itemPrefab;
            m_itemsScrolled.Free();
            for (int i = 0; i < m_itemsScrolled.Count; i++)
                m_itemsScrolled[i].Refresh();

            Init(totalItems, force, startIndex);
        }

        private void LateUpdate()
        {
            for (int i = 0; i < m_itemsScrolled.Count; i++)
                m_itemsScrolled[i].ManualUpdate();
        }

        /// <summary>
        /// (Re)builds the scroll view for <paramref name="totalItems"/> items, optionally starting at
        /// <paramref name="startIndex"/>. Returns cleanly without crashing on <c>totalItems &lt;= 0</c>
        /// (Phase 3 fix).
        /// </summary>
        public void Init(int totalItems, bool force, int startIndex = 0)
        {
            if (totalItems == total && !force)
                return;

            if (totalItems <= 0)
            {
                if (m_itemsScrolled != null)
                    m_itemsScrolled.Free(container);
                m_itemsRect.Clear();
                total = 0;
                m_optimizedTotal = 0;
                container.sizeDelta = new Vector2(container.sizeDelta.x, 0);
                return;
            }

            m_totalBuffer = 2;
            m_itemsRect.Clear();

            if (m_itemsScrolled == null || m_itemsScrolled.Count == 0)
            {
                m_itemsScrolled = new List<OptimizedScrollItem>();
                m_itemsScrolled.Prepare(prefab, container.parent, 5);
            }
            else
                m_itemsScrolled.Free(container);

            total = totalItems;
            container.anchoredPosition3D = Vector3.zero;

            var rectZero = m_itemsScrolled[0].GetComponent<RectTransform>();
            var prefabScale = rectZero.rect.size;
            m_cellSizeY = prefabScale.y + spacing;
            m_prefabSizeX = totalCellOnRow > 1 ? prefabScale.x + spacing : prefabScale.x;
            m_pivot = rectZero.pivot;
            container.sizeDelta = new Vector2(m_prefabSizeX * totalCellOnRow, m_cellSizeY * Mathf.CeilToInt(total * 1f / totalCellOnRow));
            m_halfSizeContainer = container.rect.size.y * 0.5f;

            var scrollRect = scrollView.transform as RectTransform;

            if (autoMatchHeight)
            {
                float preferHeight = container.rect.size.y + spacing * 2;
                preferHeight = Mathf.Clamp(preferHeight, minViewHeight, maxViewHeight > 0 ? maxViewHeight : preferHeight);
                var size = scrollRect.rect.size;
                size.y = preferHeight;
                scrollRect.sizeDelta = size;
            }

            var viewport = scrollView.viewport;
            m_totalVisible = Mathf.CeilToInt(viewport.rect.size.y / m_cellSizeY) * totalCellOnRow;
            m_totalBuffer *= totalCellOnRow;

            m_offsetVec = Vector3.down;
            m_startPos = container.anchoredPosition3D - m_offsetVec * m_halfSizeContainer + m_offsetVec * (prefabScale.y * 0.5f);
            m_optimizedTotal = Mathf.Min(total, m_totalVisible + m_totalBuffer);

            for (int i = 0; i < m_optimizedTotal; i++)
            {
                var item = m_itemsScrolled.Obtain(container);
                var rt = item.transform as RectTransform;
                MoveItemByIndex(rt, i);
                m_itemsRect.Add(rt);
                item.gameObject.SetActive(true);
                item.UpdateContent(i, true);
            }

            prefab.gameObject.SetActive(false);
            ScrollToIndex(Mathf.Max(0, startIndex));
#if DOTWEEN
            TryFirePendingAnimRequest();
#endif
        }

        /// <summary>Scrolls to the topmost item. <paramref name="tween"/> animates over a short duration (requires DOTWEEN).</summary>
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
                scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, 1);
        }

        /// <summary>Scrolls to the bottommost item. <paramref name="tween"/> animates over a short duration (requires DOTWEEN).</summary>
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
                scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, 0);
        }

        private void ScrollBarChanged(Vector2 normPos)
        {
            if (m_optimizedTotal <= 0)
                return;

            normPos.y = 1f - normPos.y;
            if (totalCellOnRow > 1)
                normPos.y += 0.06f;

            normPos.y = Mathf.Clamp01(normPos.y);

            var viewport = scrollView.viewport;
            viewport.GetWorldCorners(m_viewportCorners);
            var viewportRect = new Rect(m_viewportCorners[0], m_viewportCorners[2] - m_viewportCorners[0]);

            int numOutOfView = Mathf.CeilToInt(normPos.y * (total - m_totalVisible));
            int firstIndex = Mathf.Max(0, numOutOfView - m_totalBuffer);
            int originalIndex = firstIndex % m_optimizedTotal;

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

        private bool IsItemVisible(Rect viewportRect, int index)
        {
            m_itemsRect[index].GetWorldCorners(m_itemCorners);
            var itemRect = new Rect(m_itemCorners[0], m_itemCorners[2] - m_itemCorners[0]);
            return viewportRect.Overlaps(itemRect);
        }

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

        private void MoveItemByIndex(RectTransform item, int index)
        {
            item.anchoredPosition3D = GetItemAnchoredPos(index);
        }

        /// <summary>Returns the active item pool. Live view.</summary>
        public List<OptimizedScrollItem> GetListItem() => m_itemsScrolled;

#if DOTWEEN
        /// <summary>
        /// Scrolls so item <paramref name="index"/> is visible. <paramref name="tween"/> animates the
        /// transition; <paramref name="overrideDuration"/> overrides the auto-computed duration when
        /// non-negative. <paramref name="ease"/> sets the easing curve (DOTWEEN only).
        /// </summary>
        public void ScrollToIndex(int index, bool tween = false, Action onComplete = null, float overrideDuration = -1f, Ease ease = Ease.OutQuad)
#else
        /// <summary>Scrolls so item <paramref name="index"/> is visible. <paramref name="tween"/> animates the transition.</summary>
        public void ScrollToIndex(int index, bool tween = false, Action onComplete = null, float overrideDuration = -1f)
#endif
        {
            index = Mathf.Clamp(index, 0, total - 1);
            int rowIndex = Mathf.FloorToInt(index * 1f / totalCellOnRow);
            float scrollableHeight = container.rect.size.y - scrollView.viewport.rect.size.y;
            if (scrollableHeight <= 0)
            {
                onComplete?.Invoke();
                return;
            }

            float viewHeight = scrollView.viewport != null ? scrollView.viewport.rect.size.y : GetComponent<RectTransform>().rect.size.y;
            float centerOffset = (viewHeight - m_cellSizeY) / 2f;
            float targetViewportTop = rowIndex * m_cellSizeY - centerOffset;
            float toY = Mathf.Clamp01(targetViewportTop / scrollableHeight);

            if (tween)
            {
#if DOTWEEN
                DOTween.Kill(scrollView.GetInstanceID());
                float fromY = 1 - scrollView.normalizedPosition.y;
                if (toY != fromY)
                {
                    float time = overrideDuration > 0 ? overrideDuration : Mathf.Abs(toY - fromY) * 2;
                    if (time < 0.1f && time > 0)
                        time = 0.1f;
                    float val = fromY;
                    DOTween.To(() => val, x => val = x, toY, time)
                        .SetId(scrollView.GetInstanceID())
                        .SetEase(ease)
                        .OnUpdate(() => scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, 1f - val))
                        .OnComplete(() => onComplete?.Invoke());
                }
                else
                    onComplete?.Invoke();
#else
                scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, 1f - toY);
                onComplete?.Invoke();
                ScrollBarChanged(scrollView.normalizedPosition);
#endif
            }
            else
            {
                scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, 1f - toY);
                onComplete?.Invoke();
                ScrollBarChanged(scrollView.normalizedPosition);
            }
        }

        /// <summary>Coroutine that animates an item visually moving from index <paramref name="a"/> to <paramref name="b"/>. Used for re-order animations.</summary>
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
        private const int ANIM_MOVE_TWEEN_ID = 92701;
        private GameObject m_animClone;
        private Coroutine m_animCoroutine;
        private HashSet<int> m_hiddenIndicesForAnim = new();

        private struct AnimRequest
        {
            public int from, to;
            public Action<GameObject> configureClone;
            public Action onComplete;
        }
        private AnimRequest? m_pendingAnimRequest;

        /// <summary>True while an <see cref="AnimateItemMove"/> tween is in flight.</summary>
        public bool IsAnimating => m_animCoroutine != null;
        /// <summary>Fired when an <see cref="AnimateItemMove"/> tween begins.</summary>
        public event Action onAnimationStarted;
        /// <summary>Fired when an <see cref="AnimateItemMove"/> tween completes (or is cancelled).</summary>
        public event Action onAnimationCompleted;

        private void SetItemAlphaForAnim(OptimizedScrollItem item, int index)
        {
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
        }

        private void UpdateAllVisibleItemsAlpha()
        {
            foreach (var item in m_itemsScrolled)
                SetItemAlphaForAnim(item, item.Index);
        }

        /// <summary>
        /// Animates a clone of the item at <paramref name="fromIndex"/> visually moving to
        /// <paramref name="toIndex"/>'s position. The clone is destroyed on completion;
        /// <paramref name="configureClone"/> lets you customize the clone (e.g. swap material)
        /// before the tween starts. Requires DOTWEEN.
        /// </summary>
        public void AnimateItemMove(int fromIndex, int toIndex, Action<GameObject> configureClone = null, Action onComplete = null)
        {
            if (fromIndex == toIndex || fromIndex < 0 || toIndex < 0 || fromIndex >= total || toIndex >= total)
            {
                onComplete?.Invoke();
                return;
            }

            StopAnimateItemMove();
            m_animCoroutine = StartCoroutine(IEAnimateItemMove(fromIndex, toIndex, configureClone, onComplete));
        }

        /// <summary>Like <see cref="AnimateItemMove"/> but defers if the scroll view is not yet active or the optimized count is 0. Useful for queued animations during scene transitions.</summary>
        public void QueueAnimateItemMove(int fromIndex, int toIndex, Action<GameObject> configureClone = null, Action onComplete = null)
        {
            if (m_optimizedTotal > 0 && gameObject.activeInHierarchy)
                AnimateItemMove(fromIndex, toIndex, configureClone, onComplete);
            else
                m_pendingAnimRequest = new AnimRequest { from = fromIndex, to = toIndex, configureClone = configureClone, onComplete = onComplete };
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

        /// <summary>Cancels any in-flight item-move animation, destroys the clone, and restores all items' alpha.</summary>
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

            if (scrollView != null)
            {
                scrollView.vertical = true;
                scrollView.horizontal = totalCellOnRow > 1;
            }
        }

        private IEnumerator IEAnimateItemMove(int fromIndex, int toIndex, Action<GameObject> configureClone, Action onComplete)
        {
            bool wasVertical = scrollView.vertical;
            bool wasHorizontal = scrollView.horizontal;
            scrollView.vertical = false;
            scrollView.horizontal = false;
            scrollView.StopMovement();

            yield return null;

            onAnimationStarted?.Invoke();
            ScrollToIndex(fromIndex, false);

            Vector3 startAnchoredPos = GetItemAnchoredPos(fromIndex);
            Vector3 endAnchoredPos = GetItemAnchoredPos(toIndex);

            var sourceItem = m_itemsScrolled.Count > 0 ? m_itemsScrolled[0] : prefab;
            if (sourceItem == null)
            {
                m_hiddenIndicesForAnim.Clear();
                UpdateAllVisibleItemsAlpha();
                scrollView.vertical = wasVertical;
                scrollView.horizontal = wasHorizontal;
                m_animCoroutine = null;
                onAnimationCompleted?.Invoke();
                onComplete?.Invoke();
                yield break;
            }

            m_animClone = Instantiate(sourceItem.gameObject, container);
            m_animClone.SetActive(true);
            configureClone?.Invoke(m_animClone);

            var cloneRT = m_animClone.transform as RectTransform;
            cloneRT.anchoredPosition3D = startAnchoredPos;
            cloneRT.SetAsLastSibling();

            m_hiddenIndicesForAnim.Add(fromIndex);
            m_hiddenIndicesForAnim.Add(toIndex);
            UpdateAllVisibleItemsAlpha();

            if (animMoveDelay > 0)
            {
                float t = 0;
                while (t < animMoveDelay)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            m_hiddenIndicesForAnim.Remove(fromIndex);
            UpdateAllVisibleItemsAlpha();

            int distance = Mathf.Abs(toIndex - fromIndex);
            float duration = Mathf.Clamp(distance * animMoveDurationPerItem, animMoveMinDuration, animMoveMaxDuration);
            var syncEase = Ease.InOutCubic;

            bool animDone = false;
            var seq = DOTween.Sequence().SetId(ANIM_MOVE_TWEEN_ID).SetUpdate(true);
            seq.Append(cloneRT.DOLocalMove(endAnchoredPos, duration).SetEase(syncEase));
            seq.Join(cloneRT.DOPunchScale(Vector3.one * 0.12f, duration, 1, 0f));
            seq.OnComplete(() => animDone = true);

            ScrollToIndex(toIndex, true, null, duration, syncEase);

            yield return new WaitUntil(() => animDone);

            m_hiddenIndicesForAnim.Remove(toIndex);
            UpdateAllVisibleItemsAlpha();

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

            scrollView.vertical = wasVertical;
            scrollView.horizontal = wasHorizontal;

            onAnimationCompleted?.Invoke();
            onComplete?.Invoke();
        }

        private OptimizedScrollItem FindItem(int targetIndex)
        {
            for (int i = 0; i < m_itemsScrolled.Count; i++)
                if (m_itemsScrolled[i].Index == targetIndex)
                    return m_itemsScrolled[i];
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
#else
        private void SetItemAlphaForAnim(OptimizedScrollItem item, int index) { }
#endif
    }
}
