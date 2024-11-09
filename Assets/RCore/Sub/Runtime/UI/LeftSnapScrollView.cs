// Author - RaBear - 2018

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RCore.Inspector;
#if DOTWEEN
using DG.Tweening;
#endif

namespace RCore.UI
{
    public class LeftSnapScrollView : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        private const float MIN_SPRING_SPEED = 5f;

#pragma warning disable 0649
        [SerializeField] private ScrollRect m_scrollView;
        [SerializeField] private List<RectTransform> m_items;
        [SerializeField] private float m_springSpeed = 10;
        [SerializeField] private float m_scrollReaction = 50;

        [SerializeField, ReadOnly] private int m_nearestIndex;
        [SerializeField, ReadOnly] private bool m_isSnapping;
        [SerializeField, ReadOnly] private bool m_isDragging;
#pragma warning disable 0649

        private float m_contentAnchoredXMin;
        private float m_contentAnchoredXMax;
        private float m_distance;

        private Vector2 m_previousPosition;
        private Vector2 m_velocity;
        private Vector2 m_beginDragPosition;
        private float m_dragDistance;
        private bool m_dragLeftToRight;

        private void Update()
        {
            m_velocity = m_scrollView.content.anchoredPosition - m_previousPosition;
            m_previousPosition = m_scrollView.content.anchoredPosition;

            if (m_isSnapping || m_isDragging)
                return;

            float speedX = Mathf.Abs(m_velocity.x);
            if (speedX == 0)
                return;

            if (OutOfBoundary())
            {
                //If container position is nearly reach left/right border
                //We let ScrollRect auto do its stuff
            }
            else
            {
                var contentAnchor = m_scrollView.content.anchoredPosition;
                for (int i = 0; i < m_items.Count; i++)
                {
                    float width = m_items[i].rect.width;
                    if (speedX != 0 && speedX < m_springSpeed)
                    {
                        var itemBorderLeft = m_items[i].BotLeft().x;
                        itemBorderLeft *= -1;
                        if (contentAnchor.x < itemBorderLeft
                            && contentAnchor.x > itemBorderLeft - width * 0.5f)
                        {
                            FindNearestItem();
                            MoveToNearest(false, speedX);
                        }
                    }
                }
            }
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            m_items = new List<RectTransform>();
            foreach (RectTransform item in m_scrollView.content)
            {
                if (item.gameObject.activeSelf)
                    m_items.Add(item);
            }

            if (m_items == null || m_items.Count == 0)
            {
                gameObject.SetActive(false);
                return;
            }
            else
            {
                TimerEventsInScene.Instance.WaitForSeconds(0.03f, (a) =>
                {
                    m_contentAnchoredXMin = -100000;
                    m_contentAnchoredXMax = 100000;
                    for (int i = 0; i < m_items.Count; i++)
                    {
                        float itemLeftBorder = m_items[i].BotLeft().x;
                        if (itemLeftBorder < m_contentAnchoredXMax)
                            m_contentAnchoredXMax = itemLeftBorder;

                        float rightBorderX = m_items[i].TopRight().x;
                        float maxRightContentX = rightBorderX - m_scrollView.viewport.rect.width;
                        if (maxRightContentX > m_contentAnchoredXMin)
                            m_contentAnchoredXMin = maxRightContentX;
                    }
                    m_contentAnchoredXMax *= -1;
                    m_contentAnchoredXMin *= -1;
                    m_scrollView.StopMovement();
                    m_scrollView.content.SetX(m_contentAnchoredXMax);
                });
            }
#if DOTWEEN
            DOTween.Kill(GetInstanceID());
#endif
            m_isDragging = false;
            m_isSnapping = false;
        }

        private void FindNearestItem()
        {
            m_distance = 1000000;
            m_nearestIndex = 0;
            for (int i = 0; i < m_items.Count; i++)
            {
                float itemAnchoredX = m_items[i].anchoredPosition.x;
                float itemAnchoredX_left = itemAnchoredX - m_items[m_nearestIndex].rect.width * m_items[m_nearestIndex].pivot.x;
                float distanceX = m_scrollView.content.anchoredPosition.x - itemAnchoredX_left * -1;
                distanceX = Mathf.Abs(distanceX);
                if (m_distance > distanceX)
                {
                    m_distance = distanceX;
                    m_nearestIndex = i;
                }
            }
        }

        private void MoveToNearest(bool pImediate, float pSpeed)
        {
            if (pSpeed < MIN_SPRING_SPEED)
                pSpeed = MIN_SPRING_SPEED;

            m_scrollView.StopMovement();

            float itemAnchoredX = m_items[m_nearestIndex].anchoredPosition.x;
            float itemBorderLeft = itemAnchoredX - m_items[m_nearestIndex].rect.width * m_items[m_nearestIndex].pivot.x;
            float targetAnchored = itemBorderLeft *= -1;

            if ((itemBorderLeft > m_contentAnchoredXMax || itemBorderLeft < m_contentAnchoredXMin)
                && (m_scrollView.movementType == ScrollRect.MovementType.Elastic || m_scrollView.movementType == ScrollRect.MovementType.Clamped))
            {
                return;
            }
            else
            {
                if (targetAnchored > m_contentAnchoredXMax)
                    targetAnchored = m_contentAnchoredXMax;
                if (targetAnchored < m_contentAnchoredXMin)
                    targetAnchored = m_contentAnchoredXMin;
            }

            var contentAnchored = m_scrollView.content.anchoredPosition;

            if (pImediate)
            {
                contentAnchored.x = targetAnchored;
                m_scrollView.content.anchoredPosition = contentAnchored;
            }
            else
            {
                m_isSnapping = false;

                float time = m_distance / (pSpeed / Time.deltaTime);
                if (time == 0)
                    return;

#if DOTWEEN
                var fromPos = m_scrollView.content.anchoredPosition;
                float lerp = 0;
                DOTween.Kill(GetInstanceID());
                DOTween.To(() => lerp, x => lerp = x, 1f, time)
                    .OnStart(() => { m_isSnapping = true; })
                    .OnUpdate(() =>
                    {
                        contentAnchored.x = Mathf.Lerp(fromPos.x, targetAnchored, lerp);
                        m_scrollView.content.anchoredPosition = contentAnchored;
                    })
                    .OnComplete(() =>
                    {
                        m_isSnapping = false;
                        contentAnchored.x = targetAnchored;
                        m_scrollView.content.anchoredPosition = contentAnchored;
                    })
                    .SetUpdate(true)
                    .SetId(GetInstanceID());
#else
                contentAnchored.x = targetAnchored;
                m_scrollView.content.anchoredPosition = contentAnchored;
#endif
            }
        }

        public void MoveToItem(int pIndex)
        {
            m_scrollView.StopMovement();

            float itemAnchorX = m_items[pIndex].anchoredPosition.x;
            float itemBorderLeft = itemAnchorX - m_items[m_nearestIndex].rect.width * m_items[m_nearestIndex].pivot.x;
            float targetAnchor = itemBorderLeft *= -1;

            if ((itemBorderLeft > m_contentAnchoredXMax || itemBorderLeft < m_contentAnchoredXMin)
                && (m_scrollView.movementType == ScrollRect.MovementType.Elastic || m_scrollView.movementType == ScrollRect.MovementType.Clamped))
            {
                return;
            }
            else
            {
                if (targetAnchor > m_contentAnchoredXMax)
                    targetAnchor = m_contentAnchoredXMax;
                if (targetAnchor < m_contentAnchoredXMin)
                    targetAnchor = m_contentAnchoredXMin;
            }

            var contentAnchor = m_scrollView.content.anchoredPosition;
            contentAnchor.x = targetAnchor;
            m_scrollView.content.anchoredPosition = contentAnchor;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
#if DOTWEEN
            DOTween.Kill(GetInstanceID());
#endif
            m_isDragging = true;
            m_isSnapping = false;
            m_beginDragPosition = m_scrollView.content.anchoredPosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            m_isDragging = false;

            if (OutOfBoundary())
                return;

            var endDragPosition = m_scrollView.content.anchoredPosition;
            m_dragLeftToRight = m_beginDragPosition.x < endDragPosition.x;
            m_dragDistance = Math.Abs(m_beginDragPosition.x - endDragPosition.x);

            float speedX = Math.Abs(m_velocity.x);
            if (speedX <= m_springSpeed)
            {
                FindNearestItem();
                CheckMinScrollReaction();
                MoveToNearest(false, speedX);
            }
        }

        private bool OutOfBoundary()
        {
            var contentAnchor = m_scrollView.content.anchoredPosition;
            return contentAnchor.x >= m_contentAnchoredXMax - 20 || contentAnchor.x <= m_contentAnchoredXMin + 20;
        }

        public void OnDrag(PointerEventData eventData)
        {

        }

        private void CheckMinScrollReaction()
        {
            if (m_dragDistance > m_scrollReaction)
            {
                float itemAnchoredX = m_items[m_nearestIndex].anchoredPosition.x;
                float itemAnchoredX_left = itemAnchoredX - m_items[m_nearestIndex].rect.width * m_items[m_nearestIndex].pivot.x;
                //float rightBorderX = itemAnchorX + mItems[mNearestIndex].rect.width * (1 - mItems[mNearestIndex].pivot.x);
                //float itemCenter = itemLeftBorder + mItems[mNearestIndex].rect.width / 2f;
                if (m_dragLeftToRight
                    && m_scrollView.content.anchoredPosition.x > itemAnchoredX_left * -1
                    && m_nearestIndex > 0)
                {
                    m_nearestIndex--;
                    float distanceX = m_scrollView.content.anchoredPosition.x - itemAnchoredX_left * -1;
                    m_distance = Mathf.Abs(distanceX);
                }
                else if (!m_dragLeftToRight
                    && m_scrollView.content.anchoredPosition.x < itemAnchoredX_left * -1
                    && m_nearestIndex < m_items.Count - 1)
                {
                    m_nearestIndex++;
                    float distanceX = m_scrollView.content.anchoredPosition.x - itemAnchoredX_left * -1;
                    m_distance = Mathf.Abs(distanceX);
                }
            }
        }
    }
}