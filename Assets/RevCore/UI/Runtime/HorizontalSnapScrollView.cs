using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace RevCore.UI
{
    /// <summary>
    /// Horizontal "paginated" scroll view that snaps to one <see cref="SnapScrollItem"/> at a time
    /// (carousel / onboarding pattern). Items can be wider than the viewport; the snap target is
    /// the item whose center is nearest to <c>m_PointToCheckDistanceToCenter</c>. Fires events on
    /// scroll start / end and on index change.
    /// </summary>
    public class HorizontalSnapScrollView : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        /// <summary>Fired with the new focused index after each snap.</summary>
        public Action<int> onIndexChanged;
        /// <summary>Fired when the snap animation completes.</summary>
        public Action onScrollEnd;
        /// <summary>Fired when the user begins a drag.</summary>
        public Action onScrollStart;

        [Tooltip("The index of the item to be displayed first when the scene loads.")]
        [SerializeField] private int m_StartIndex;
        [Tooltip("The minimum duration of the snapping animation.")]
        [SerializeField] private float m_MinSpringTime = 0.5f;
        [Tooltip("The maximum duration of the snapping animation.")]
        [SerializeField] private float m_MaxSpringTime = 1f;
        [Tooltip("The velocity threshold below which the scroll view will start snapping to the nearest item.")]
        [SerializeField] private float m_SpringThreshold = 15;
        [Tooltip("If true, m_MinScrollReaction will be automatically calculated based on the focused item's width.")]
        [SerializeField] private bool m_AutoSetMinScrollReaction = true;
        [Tooltip("The minimum distance the user must drag to trigger a snap to the next/previous item. Only used if m_AutoSetMinScrollReaction is false.")]
        [SerializeField] private float m_MinScrollReaction = 10;
        [Tooltip("An offset applied to the final snapped position of the content.")]
        [SerializeField] private Vector2 m_TargetPosOffset;
        [Tooltip("Reference to the ScrollRect component.")]
        [SerializeField] private ScrollRect m_ScrollView;
        [Tooltip("The array of items within the scroll view. This is populated automatically.")]
        [SerializeField] private SnapScrollItem[] m_Items;
        [Tooltip("Set to TRUE if the items are ordered from right to left in the hierarchy.")]
        [SerializeField] private bool m_ReverseList;
        [Tooltip("A RectTransform used as a reference point (usually the center of the viewport) to determine the nearest item to snap to.")]
        [SerializeField] private RectTransform m_PointToCheckDistanceToCenter;

        [SerializeField, ReadOnly] private float m_ContentAnchoredXMin;
        [SerializeField, ReadOnly] private float m_ContentAnchoredXMax;
        [SerializeField, ReadOnly] private int m_FocusedItemIndex = -1;
        [SerializeField, ReadOnly] private int m_PreviousItemIndex = -1;
        [SerializeField, ReadOnly] private bool m_IsSnapping;
        [SerializeField, ReadOnly] private bool m_IsDragging;
        [SerializeField, ReadOnly] private bool m_Validated;
        private bool m_validateNextFrame;
        private Vector2 m_previousPosition;
        private float m_dragDistance;
        private Vector2 m_velocity;
        private float m_distance;
        private Vector2 m_beginDragPosition;
        private bool m_dragFromLeft;
        private bool m_checkBoundary;
        private bool m_checkStop;

        /// <summary>The scroll rect's content rect transform.</summary>
        public RectTransform Content => m_ScrollView.content;
        /// <summary>Index of the currently-focused item, or -1 before first focus is established.</summary>
        public int FocusedItemIndex => m_FocusedItemIndex;
        /// <summary>Number of items in the scroll view.</summary>
        public int TotalItems => m_Items.Length;
        /// <summary>True during the snap animation that follows a drag end.</summary>
        public bool IsSnapping => m_IsSnapping;
        /// <summary>True while the user is actively dragging.</summary>
        public bool IsDragging => m_IsDragging;
        /// <summary>Backing array of items in hierarchy order.</summary>
        public SnapScrollItem[] Items => m_Items;
        /// <summary>The currently-focused item. Undefined when no item has been focused yet.</summary>
        public SnapScrollItem FocusedItem => m_Items[m_FocusedItemIndex];

        private void Start()
        {
            float parentWidth = m_ScrollView.viewport.rect.width;
            foreach (var item in m_Items)
            {
                var itemRectTransform = item.GetComponent<RectTransform>();
                if (itemRectTransform != null)
                    itemRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentWidth);
            }

            Validate();
            m_PreviousItemIndex = m_StartIndex;
            MoveToItem(m_StartIndex, true);
        }

        private void OnEnable()
        {
            m_checkBoundary = m_ScrollView.movementType == ScrollRect.MovementType.Unrestricted;
        }

        private void Update()
        {
            if (!m_Validated || !m_ScrollView.horizontal)
                return;

            if (m_validateNextFrame)
            {
                m_validateNextFrame = false;
                Validate();
            }

            if (m_Items == null || m_Items.Length == 0)
                return;

            m_velocity = Content.anchoredPosition - m_previousPosition;
            m_previousPosition = Content.anchoredPosition;

            if (m_IsDragging || m_IsSnapping)
                return;

            float speedX = Mathf.Abs(m_velocity.x);
            if (speedX == 0)
            {
                if (m_checkStop)
                {
                    FindNearestItem();
                    m_checkStop = false;
                }
                return;
            }
            m_checkStop = true;

            if (m_checkBoundary && OutOfBoundary()) { }
            else
            {
                if (speedX > 0 && speedX <= m_SpringThreshold)
                {
                    FindNearestItem();
                    int index = m_FocusedItemIndex;
                    var targetPos = m_Items[index].RectTransform.CovertAnchoredPosFromChildToParent(m_ScrollView.content);

                    if (m_dragFromLeft)
                    {
                        if (Content.anchoredPosition.x > targetPos.x + m_MinScrollReaction)
                            index += m_ReverseList ? 1 : -1;
                    }
                    else
                    {
                        if (Content.anchoredPosition.x < targetPos.x - m_MinScrollReaction)
                            index += m_ReverseList ? -1 : 1;
                    }

                    if (index < 0)
                        index = 0;
                    else if (index >= m_Items.Length - 1)
                        index = m_Items.Length - 1;

                    SetFocusedIndex(index);
                    MoveToFocusedItem(false, speedX);
                }
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            Validate();
            if (m_Items.Length > 0)
            {
                m_FocusedItemIndex = m_Items.Length / 2;
                m_PreviousItemIndex = m_Items.Length / 2;
                MoveToFocusedItem();
            }
        }

        /// <inheritdoc />
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!m_ScrollView.horizontal || m_IsSnapping)
                return;

#if DOTWEEN
            DOTween.Kill(GetInstanceID());
#endif
            m_IsDragging = true;
            m_IsSnapping = false;
            m_beginDragPosition = Content.anchoredPosition;
            onScrollStart?.Invoke();

            foreach (var item in m_Items)
                if (item.gameObject.activeSelf)
                    item.OnBeginDrag();
        }

        /// <inheritdoc />
        public void OnDrag(PointerEventData eventData)
        {
            if (!m_ScrollView.horizontal)
                return;

            if (!m_IsDragging)
            {
                onScrollStart?.Invoke();
                foreach (var item in m_Items)
                    if (item.gameObject.activeSelf)
                        item.OnBeginDrag();
            }
            m_IsDragging = true;
            FindNearestItem();

            if (m_beginDragPosition.x < Content.anchoredPosition.x)
                m_Items[Mathf.Clamp(m_PreviousItemIndex - 1, 0, m_Items.Length - 1)].Show();
            else if (m_beginDragPosition.x > Content.anchoredPosition.x)
                m_Items[Mathf.Clamp(m_PreviousItemIndex + 1, 0, m_Items.Length - 1)].Show();
        }

        /// <inheritdoc />
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!m_ScrollView.horizontal)
                return;

            m_IsDragging = false;

            if (m_checkBoundary && OutOfBoundary())
                return;

            var endDragPosition = Content.anchoredPosition;
            m_dragDistance = Mathf.Abs(m_beginDragPosition.x - endDragPosition.x);
            m_dragFromLeft = m_beginDragPosition.x < endDragPosition.x;
        }

        /// <summary>Snaps to <paramref name="item"/>. <paramref name="pImmediately"/> skips the snap animation.</summary>
        public void MoveToItem(SnapScrollItem item, bool pImmediately = false)
        {
            int index = m_Items.IndexOf(item);
            if (index >= 0)
                MoveToItem(index, pImmediately);
        }

        /// <summary>Snaps to the item at <paramref name="pIndex"/>. <paramref name="pImmediately"/> skips the snap animation.</summary>
        public void MoveToItem(int pIndex, bool pImmediately = false)
        {
            if (pIndex < 0 || pIndex >= m_Items.Length || !m_Validated)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                SetFocusedIndex(pIndex);
                MoveToFocusedItem(false, 0);
                return;
            }
#endif
            SetFocusedIndex(pIndex);
            MoveToFocusedItem(pImmediately, m_SpringThreshold);
        }

        /// <summary>Queues a <see cref="Validate"/> call for the next frame — used when item active state changes mid-frame.</summary>
        public void ValidateNextFrame()
        {
            if (m_validateNextFrame)
                return;
            m_validateNextFrame = true;
        }

        /// <summary>Recomputes content bounds, item positions, and focused index. Called once after item set changes.</summary>
        public void Validate()
        {
            SnapScrollItem focusedItem = null;
            if (m_Items != null && m_FocusedItemIndex >= 0 && m_FocusedItemIndex < m_Items.Length)
                focusedItem = m_Items[m_FocusedItemIndex];

            m_Items = gameObject.GetComponentsInChildren<SnapScrollItem>();
            if (focusedItem != null)
            {
                int index = Array.IndexOf(m_Items, focusedItem);
                if (index != -1)
                    m_FocusedItemIndex = index;
                else
                    m_FocusedItemIndex = Mathf.Clamp(m_FocusedItemIndex, 0, Mathf.Max(0, m_Items.Length - 1));
            }

            Content.TryGetComponent(out ContentSizeFitter contentFilter);
            float contentWidth = 0;
            if (contentFilter != null)
            {
                Content.TryGetComponent(out HorizontalLayoutGroup horizontalLayout);
                float paddingLeft = 0;
                float paddingRight = 0;
                float spacing = 0;
                if (horizontalLayout != null)
                {
                    paddingLeft = horizontalLayout.padding.left;
                    paddingRight = horizontalLayout.padding.right;
                    spacing = horizontalLayout.spacing;
                }
                for (int i = 0; i < m_Items.Length; i++)
                {
                    if (m_Items[i].gameObject.activeSelf)
                        contentWidth += m_Items[i].RectTransform.rect.size.x;
                }
                contentWidth += paddingLeft + paddingRight;
                contentWidth += spacing * (m_Items.Length - 1);
            }
            else
                contentWidth = Content.rect.width;

            float contentPivotX = Content.pivot.x;
            float viewPortOffsetX = m_ScrollView.viewport.rect.width / 2f;
            m_ContentAnchoredXMin = (contentWidth - contentWidth * contentPivotX - viewPortOffsetX) * -1;
            m_ContentAnchoredXMax = (0 - contentWidth * contentPivotX + viewPortOffsetX) * -1;

            if (m_MinScrollReaction < 10)
                m_MinScrollReaction = 10;
            m_Validated = true;

            if (!Application.isPlaying && m_StartIndex != m_PreviousItemIndex)
                MoveToItem(m_StartIndex, true);
        }

        private void MoveToFocusedItem(bool pImmediately, float pSpeed)
        {
            if (m_Items == null || m_Items.Length == 0)
                return;

            m_ScrollView.StopMovement();

            var targetAnchored = m_Items[m_FocusedItemIndex].RectTransform.CovertAnchoredPosFromChildToParent(m_ScrollView.content);
            targetAnchored.x -= m_TargetPosOffset.x;
            targetAnchored.y -= m_TargetPosOffset.y;

            if (targetAnchored.x > m_ContentAnchoredXMax)
                targetAnchored.x = m_ContentAnchoredXMax;
            if (targetAnchored.x < m_ContentAnchoredXMin)
                targetAnchored.x = m_ContentAnchoredXMin;

            var contentAnchored = Content.anchoredPosition;
            if (pImmediately)
            {
                contentAnchored.x = targetAnchored.x;
                Content.anchoredPosition = contentAnchored;
                onScrollEnd?.Invoke();

                for (int i = 0; i < m_Items.Length; i++)
                {
                    if (i == m_FocusedItemIndex)
                        m_Items[i].Show();
                    else
                        m_Items[i].Hide();
                }
                m_PreviousItemIndex = m_FocusedItemIndex;
            }
            else
            {
                m_IsSnapping = false;

#if DOTWEEN
                if (m_distance == 0)
                    m_distance = Vector2.Distance(m_PointToCheckDistanceToCenter.position, m_Items[m_FocusedItemIndex].RectTransform.position);
                float time = m_distance / (pSpeed / Time.deltaTime);
                if (time == 0)
                    return;
                time = Mathf.Clamp(time, m_MinSpringTime, m_MaxSpringTime);

                bool moveToLeft = Content.anchoredPosition.x < targetAnchored.x;
                for (int i = 0; i < m_Items.Length; i++)
                {
                    if (moveToLeft && i > m_PreviousItemIndex || !moveToLeft && i < m_PreviousItemIndex || moveToLeft && i < m_FocusedItemIndex || !moveToLeft && i > m_FocusedItemIndex)
                        m_Items[i].Hide();
                    else
                        m_Items[i].Show();
                }

                var fromPos = Content.anchoredPosition;
                float lerp = 0;
                DOTween.Kill(GetInstanceID());
                DOTween.To(() => lerp, x => lerp = x, 1f, time)
                    .OnStart(() =>
                    {
                        m_IsSnapping = true;
                        onScrollStart?.Invoke();
                        foreach (var item in m_Items)
                            if (item.gameObject.activeSelf)
                                item.OnBeginDrag();
                    })
                    .OnUpdate(() =>
                    {
                        contentAnchored.x = Mathf.Lerp(fromPos.x, targetAnchored.x, lerp);
                        Content.anchoredPosition = contentAnchored;
                    })
                    .OnComplete(() =>
                    {
                        for (int i = 0; i < m_Items.Length; i++)
                        {
                            if (i == m_FocusedItemIndex)
                                m_Items[i].Show();
                            else
                                m_Items[i].Hide();
                        }

                        m_PreviousItemIndex = m_FocusedItemIndex;
                        m_IsSnapping = false;
                        contentAnchored.x = targetAnchored.x;
                        Content.anchoredPosition = contentAnchored;
                        onScrollEnd?.Invoke();
                    })
                    .SetId(GetInstanceID());
#else
                for (int i = 0; i < m_Items.Length; i++)
                {
                    if (i == m_FocusedItemIndex)
                        m_Items[i].Show();
                    else
                        m_Items[i].Hide();
                }
                m_PreviousItemIndex = m_FocusedItemIndex;
                contentAnchored.x = targetAnchored.x;
                Content.anchoredPosition = contentAnchored;
                onScrollEnd?.Invoke();
#endif
            }
        }

        private void MoveToFocusedItem()
        {
            if (m_Items.Length == 0)
                return;

            for (int i = 0; i < m_Items.Length; i++)
                m_Items[i].Show();

            var targetAnchored = m_Items[m_FocusedItemIndex].RectTransform.CovertAnchoredPosFromChildToParent(m_ScrollView.content);
            targetAnchored.x -= m_TargetPosOffset.x;
            targetAnchored.y -= m_TargetPosOffset.y;
            if (targetAnchored.x > m_ContentAnchoredXMax)
                targetAnchored.x = m_ContentAnchoredXMax;
            if (targetAnchored.x < m_ContentAnchoredXMin)
                targetAnchored.x = m_ContentAnchoredXMin;

            var contentAnchored = Content.anchoredPosition;
            contentAnchored.x = targetAnchored.x;
            Content.anchoredPosition = contentAnchored;
            m_PreviousItemIndex = m_FocusedItemIndex;
        }

        private void SetFocusedIndex(int pIndex)
        {
            if (m_FocusedItemIndex == pIndex)
                return;

            m_FocusedItemIndex = pIndex;
            onIndexChanged?.Invoke(pIndex);

            if (m_AutoSetMinScrollReaction && m_Items != null && m_Items.Length > 0 && m_FocusedItemIndex < m_Items.Length)
                m_MinScrollReaction = m_Items[m_FocusedItemIndex].RectTransform.rect.width / 20f;
        }

        private void FindNearestItem()
        {
            m_distance = 1000000;
            int nearestItemIndex = 0;
            for (int i = 0; i < m_Items.Length; i++)
            {
                float distance = Vector2.Distance(m_PointToCheckDistanceToCenter.position, m_Items[i].RectTransform.position);
                distance = Mathf.Abs(distance);

                if (m_distance > distance)
                {
                    m_distance = distance;
                    nearestItemIndex = i;
                }
            }
            SetFocusedIndex(nearestItemIndex);
        }

        private bool OutOfBoundary()
        {
            var contentAnchored = Content.anchoredPosition;
            if (contentAnchored.x < m_ContentAnchoredXMin || contentAnchored.x > m_ContentAnchoredXMax)
            {
                Debug.Log("Out of boundary");
                return true;
            }
            return false;
        }
    }
}
