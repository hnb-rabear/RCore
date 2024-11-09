// Author - RaBear - 2020

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RCore.Inspector;
#if DOTWEEN
using DG.Tweening;
#endif
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif

namespace RCore.UI
{
    public class VerticalSnapScrollView : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        #region Members

        public Action<int> onIndexChanged;

#pragma warning disable 0649
        [SerializeField] private float m_MinSpringTime = 0.25f;
        [SerializeField] private float m_MaxSpringTime = 0.75f;
        [SerializeField] private ScrollRect m_ScrollView;
        [SerializeField] private List<RectTransform> m_Items;
        [SerializeField] private float m_SpringThreshold = 15; //If speed is below this valud, auto snap will happen
        [SerializeField] private bool m_AutoSetMinScollRection = true;
        [SerializeField] private float m_MinScrollReaction = 10;
        [SerializeField] private Vector2 m_TargetPosOffset;
        [SerializeField] private bool m_ReverseList;  //TRUE: If the items are ordered from bot to top
        [SerializeField] private RectTransform m_PointToCheckDistanceToCenter; //To find the nearest item
#pragma warning disable 0649

        [SerializeField, ReadOnly] private float m_ContentAnchoredYMin;
        [SerializeField, ReadOnly] private float m_ContentAnchoredYMax;
        [SerializeField, ReadOnly] private int m_FocusedItemIndex = -1;
        [SerializeField, ReadOnly] private bool m_IsSnapping;
        [SerializeField, ReadOnly] private bool m_IsDraging;

        private Vector2 m_PreviousPosition;
        private float m_DragDistance;
        private Vector2 m_Velocity;
        private float m_Distance;
        private Vector2 m_BeginDragPosition;
        private bool m_DragFromBottom;
        private bool m_CheckBoundary;
        private bool m_Validated;
        private bool m_CheckStop;

        private RectTransform Content => m_ScrollView.content;
        public int CurrentIndex => m_FocusedItemIndex;
        public int TotalItems => m_Items.Count;
        public bool IsSnapping => m_IsSnapping;
        public bool IsDragging => m_IsDraging;

        #endregion

        //=============================================

        #region MonoBehaviour

        private void OnEnable()
        {
            m_Validated = false;
            m_CheckBoundary = m_ScrollView.movementType == ScrollRect.MovementType.Unrestricted;
        }

        private void OnValidate()
        {
            Validate();
        }

        private void Update()
        {
            if (!m_Validated)
            {
                Validate();
                m_Validated = true;
            }

            m_Velocity = Content.anchoredPosition - m_PreviousPosition;
            m_PreviousPosition = Content.anchoredPosition;

            if (m_IsDraging || m_IsSnapping)
                return;

            float speedY = Mathf.Abs(m_Velocity.y);
            if (speedY == 0)
            {
                if (m_CheckStop)
                {
                    FindNearestItem();
                    m_CheckStop = false;
                }
                return;
            }
            else
                m_CheckStop = true;

            if (m_CheckBoundary && OutOfBoundary())
            {
                //If container position is nearly reach top/bottom/left/right border
                //We let ScrollRect auto do its stuff
            }
            else
            {
                if (speedY > 0 && speedY <= m_SpringThreshold)
                {
                    FindNearestItem();
                    int index = m_FocusedItemIndex;
                    var targetPos = m_Items[index].CovertAnchoredPosFromChildToParent(m_ScrollView.content);
                    if (m_DragFromBottom)
                    {
                        if (Content.anchoredPosition.y > targetPos.y + m_MinScrollReaction)
                        {
                            if (m_ReverseList)
                                index--;
                            else
                                index++;
                        }
                    }
                    else
                    {
                        if (Content.anchoredPosition.y < targetPos.y - m_MinScrollReaction)
                        {
                            if (m_ReverseList)
                                index++;
                            else
                                index--;
                        }
                    }
                    if (index < 0)
                        index = 0;
                    else if (index >= m_Items.Count - 1)
                        index = m_Items.Count - 1;
                    SetFocusedIndex(index);
                    MoveToFocusedItem(false, speedY);
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
#if DOTWEEN
            DOTween.Kill(GetInstanceID());
#endif
            m_IsDraging = true;
            m_IsSnapping = false;
            m_BeginDragPosition = Content.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            m_IsDraging = true;
            FindNearestItem();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            m_IsDraging = false;

            if (m_CheckBoundary && OutOfBoundary())
                return;

            var endDragPosition = Content.anchoredPosition;
            m_DragDistance = Mathf.Abs(m_BeginDragPosition.y - endDragPosition.y);
            m_DragFromBottom = m_BeginDragPosition.y < endDragPosition.y;
            //float speedY = Mathf.Abs(m_Velocity.y);
            //if (speedY <= m_SpringThreshold)
            //{
            //    FindNearestItem();
            //    CheckScrollReaction();
            //    MoveToFocusedItem(false, speedY);
            //}
        }

        #endregion

        //=============================================

        #region Public

        public void Init(List<RectTransform> pItems)
        {
            m_Items = pItems;

            Validate();
        }

        public void MoveToItem(int pIndex, bool pImmediately)
        {
            if (pIndex < 0 || pIndex >= m_Items.Count)
                return;

            if (m_Validated)
            {
                SetFocusedIndex(pIndex);
                MoveToFocusedItem(pImmediately, m_SpringThreshold);
            }
            else
            {
                TimerEventsInScene.Instance.WaitForCondition(() => m_Validated, () =>
                {
                    SetFocusedIndex(pIndex);
                    MoveToFocusedItem(pImmediately, m_SpringThreshold);
                });
            }
        }

        #endregion

        //==============================================

        #region Private

        private void Validate()
        {
#if UNITY_EDITOR
            string str = "Content Top Right: " + Content.TopRight()
                + "\nContent Bot Left: " + Content.BotLeft()
                + "\nContent Center: " + Content.Center()
                + "\nContent Size" + Content.sizeDelta
                + "\nContent Pivot" + Content.pivot
                + "\nViewPort Size" + m_ScrollView.viewport.rect.size;
            //Debug.Log(str);
#endif
            var contentFilter = Content.GetComponent<ContentSizeFitter>();
            float contentHeight = 0;
            float contentWidth = 0;
            if (contentFilter != null)
            {
                var verticalLayout = Content.GetComponent<VerticalLayoutGroup>();
                float paddingTop = 0;
                float paddingBottom = 0;
                float spacing = 0;
                if (verticalLayout != null)
                {
                    paddingTop = verticalLayout.padding.top;
                    paddingBottom = verticalLayout.padding.bottom;
                    spacing = verticalLayout.spacing;
                }
                for (int i = 0; i < m_Items.Count; i++)
                {
                    if (m_Items[i].gameObject.activeSelf)
                    {
                        var itemSize = m_Items[i].rect.size;
                        contentHeight += itemSize.y;
                        if (contentWidth < itemSize.x)
                            contentWidth = itemSize.x;
                    }
                }
                contentHeight += paddingTop + paddingBottom;
                contentHeight += spacing * (m_Items.Count - 1);
            }
            else
                contentHeight = Content.rect.height;

            float contentPivotY = Content.pivot.y;
            float viewPortOffsetY = m_ScrollView.viewport.rect.height / 2f;
            m_ContentAnchoredYMin = (contentHeight - contentHeight * contentPivotY - viewPortOffsetY) * -1;
            m_ContentAnchoredYMax = (0 - contentHeight * contentPivotY + viewPortOffsetY) * -1;

            if (m_MinScrollReaction < 10)
                m_MinScrollReaction = 10;
        }

        private void MoveToFocusedItem(bool pImmediately, float pSpeed)
        {
            m_ScrollView.StopMovement();

            var targetAnchored = m_Items[m_FocusedItemIndex].CovertAnchoredPosFromChildToParent(m_ScrollView.content);
            targetAnchored.x -= m_TargetPosOffset.x;
            targetAnchored.y -= m_TargetPosOffset.y;
            if (targetAnchored.y > m_ContentAnchoredYMax)
                targetAnchored.y = m_ContentAnchoredYMax;
            if (targetAnchored.y < m_ContentAnchoredYMin)
                targetAnchored.y = m_ContentAnchoredYMin;

            var contentAnchored = Content.anchoredPosition;
            if (pImmediately || !Application.isPlaying)
            {
                contentAnchored.y = targetAnchored.y;
                Content.anchoredPosition = contentAnchored;
            }
            else
            {
                m_IsSnapping = false;
#if DOTWEEN
                if (m_Distance == 0)
                    m_Distance = Vector2.Distance(m_PointToCheckDistanceToCenter.position, m_Items[m_FocusedItemIndex].position);
                float time = m_Distance / (pSpeed / Time.deltaTime);
                if (time == 0)
                    return;
                if (time < m_MinSpringTime)
                    time = m_MinSpringTime;
                else if (time > m_MaxSpringTime)
                    time = m_MaxSpringTime;

                var fromPos = Content.anchoredPosition;
                float lerp = 0;
                DOTween.Kill(GetInstanceID());
                DOTween.To(() => lerp, x => lerp = x, 1f, time)
                    .OnStart(() => m_IsSnapping = true)
                    .OnUpdate(() =>
                    {
                        contentAnchored.y = Mathf.Lerp(fromPos.y, targetAnchored.y, lerp);
                        Content.anchoredPosition = contentAnchored;
                    })
                    .OnComplete(() =>
                    {
                        m_IsSnapping = false;
                        contentAnchored.y = targetAnchored.y;
                        Content.anchoredPosition = contentAnchored;
                    })
                    .SetUpdate(true)
                    .SetId(GetInstanceID());
#else
                contentAnchored.y = targetAnchored.y;
                Content.anchoredPosition = contentAnchored;
#endif
            }
        }

        private void FindNearestItem()
        {
            m_Distance = 1000000;
            int nearestItemIndex = 0;
            for (int i = 0; i < m_Items.Count; i++)
            {
                float distance = Vector2.Distance(m_PointToCheckDistanceToCenter.position, m_Items[i].position);
                distance = Mathf.Abs(distance);
                if (m_Distance > distance)
                {
                    m_Distance = distance;
                    nearestItemIndex = i;
                }
            }
            SetFocusedIndex(nearestItemIndex);
        }

        /// <summary>
        /// Used in case we have custom top/bottom/left/right border instead of auto size component of unity
        /// </summary>
        /// <returns></returns>
        private bool OutOfBoundary()
        {
            var contentAnchored = Content.anchoredPosition;
            if (contentAnchored.y < m_ContentAnchoredYMin || contentAnchored.y > m_ContentAnchoredYMax)
            {
                Debug.Log("Out of boundary");
                return true;
            }
            return false;
        }

        private void CheckScrollReaction()
        {
            if (m_DragDistance > m_MinScrollReaction)
            {
                int index = m_FocusedItemIndex;
                //Get one down item
                if (m_DragFromBottom)
                {
                    if (m_ReverseList)
                        index -= 1;
                    else
                        index += 1;
                }
                // Get one up item
                else
                {
                    if (m_ReverseList)
                        index += 1;
                    else
                        index -= 1;
                }
                if (index < 0)
                    index = 0;
                else if (index >= m_Items.Count - 1)
                    index = m_Items.Count - 1;
                SetFocusedIndex(index);
            }
        }

        private void SetFocusedIndex(int pIndex)
        {
            if (m_FocusedItemIndex == pIndex)
                return;
            m_FocusedItemIndex = pIndex;
            onIndexChanged?.Invoke(pIndex);

            if (m_AutoSetMinScollRection)
                m_MinScrollReaction = m_Items[m_FocusedItemIndex].rect.height / 20f;
        }

        #endregion

#if UNITY_EDITOR
        [CustomEditor(typeof(VerticalSnapScrollView))]
        private class BottomSnapScrollViewEditor : UnityEditor.Editor
        {
            private VerticalSnapScrollView m_Target;
            private int m_ItemIndex;

            private void OnEnable()
            {
                m_Target = target as VerticalSnapScrollView;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (EditorHelper.Button("Validate"))
                    m_Target.Validate();
                EditorHelper.BoxHorizontal(() =>
                {
                    m_ItemIndex = EditorHelper.IntField(m_ItemIndex, "Item Index");
                    if (EditorHelper.Button("MoveToItem"))
                        m_Target.MoveToItem(m_ItemIndex, false);
                });
            }
        }
#endif
    }
}