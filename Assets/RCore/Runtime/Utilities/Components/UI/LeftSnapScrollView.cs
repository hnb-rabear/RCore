// Author - RadBear - nbhung71711@gmail.com - 2018

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RCore.Common;
using RCore.Inspector;
#if USE_DOTWEEN
using DG.Tweening;
#endif

namespace RCore.Components
{
    public class LeftSnapScrollView : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        private const float MIN_SPRING_SPEED = 5f;

#pragma warning disable 0649
        [SerializeField] private ScrollRect mScrollView;
        [SerializeField] private List<RectTransform> mItems;
        [SerializeField] private float mSpringSpeed = 10;
        [SerializeField] private float mMinScrollReaction = 50;

        [SerializeField, ReadOnly] private int mNearestIndex;
        [SerializeField, ReadOnly] private bool mIsSnapping;
        [SerializeField, ReadOnly] private bool mIsDraging;
#pragma warning disable 0649

        private float mContentAnchoredXMin;
        private float mContentAnchoredXMax;
        private float mDistance;

        private Vector2 mPreviousPosition;
        private Vector2 mVelocity;
        private Vector2 mBeginDragPosition;
        private float mDragDistance;
        private bool mDragLeftToRight;

        private void Update()
        {
            mVelocity = mScrollView.content.anchoredPosition - mPreviousPosition;
            mPreviousPosition = mScrollView.content.anchoredPosition;

            if (mIsSnapping || mIsDraging)
                return;

            float speedX = Mathf.Abs(mVelocity.x);
            if (speedX == 0)
                return;

            if (OutOfBoundary())
            {
                //If container position is nearly reach left/right border
                //We let ScrollRect auto do its stuff
            }
            else
            {
                var contentAnchor = mScrollView.content.anchoredPosition;
                for (int i = 0; i < mItems.Count; i++)
                {
                    float width = mItems[i].rect.width;
                    if (speedX != 0 && speedX < mSpringSpeed)
                    {
                        var itemBorderLeft = mItems[i].BotLeft().x;
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
            mItems = new List<RectTransform>();
            foreach (RectTransform item in mScrollView.content)
            {
                if (item.gameObject.activeSelf)
                    mItems.Add(item);
            }

            if (mItems == null || mItems.Count == 0)
            {
                gameObject.SetActive(false);
                return;
            }
            else
            {
                WaitUtil.Start(0.03f, (a) =>
                {
                    mContentAnchoredXMin = -100000;
                    mContentAnchoredXMax = 100000;
                    for (int i = 0; i < mItems.Count; i++)
                    {
                        float itemLeftBorder = mItems[i].BotLeft().x;
                        if (itemLeftBorder < mContentAnchoredXMax)
                            mContentAnchoredXMax = itemLeftBorder;

                        float rightBorderX = mItems[i].TopRight().x;
                        float maxRightContentX = rightBorderX - mScrollView.viewport.rect.width;
                        if (maxRightContentX > mContentAnchoredXMin)
                            mContentAnchoredXMin = maxRightContentX;
                    }
                    mContentAnchoredXMax *= -1;
                    mContentAnchoredXMin *= -1;
                    mScrollView.StopMovement();
                    mScrollView.content.SetX(mContentAnchoredXMax);
                });
            }
#if USE_DOTWEEN
            DOTween.Kill(GetInstanceID());
#endif
            mIsDraging = false;
            mIsSnapping = false;
        }

        private void FindNearestItem()
        {
            mDistance = 1000000;
            mNearestIndex = 0;
            for (int i = 0; i < mItems.Count; i++)
            {
                float itemAnchoredX = mItems[i].anchoredPosition.x;
                float itemAnchoredX_left = itemAnchoredX - mItems[mNearestIndex].rect.width * mItems[mNearestIndex].pivot.x;
                float distanceX = mScrollView.content.anchoredPosition.x - itemAnchoredX_left * -1;
                distanceX = Mathf.Abs(distanceX);
                if (mDistance > distanceX)
                {
                    mDistance = distanceX;
                    mNearestIndex = i;
                }
            }
        }

        private void MoveToNearest(bool pImediate, float pSpeed)
        {
            if (pSpeed < MIN_SPRING_SPEED)
                pSpeed = MIN_SPRING_SPEED;

            mScrollView.StopMovement();

            float itemAnchoredX = mItems[mNearestIndex].anchoredPosition.x;
            float itemBorderLeft = itemAnchoredX - mItems[mNearestIndex].rect.width * mItems[mNearestIndex].pivot.x;
            float targetAnchored = itemBorderLeft *= -1;

            if ((itemBorderLeft > mContentAnchoredXMax || itemBorderLeft < mContentAnchoredXMin)
                && (mScrollView.movementType == ScrollRect.MovementType.Elastic || mScrollView.movementType == ScrollRect.MovementType.Clamped))
            {
                return;
            }
            else
            {
                if (targetAnchored > mContentAnchoredXMax)
                    targetAnchored = mContentAnchoredXMax;
                if (targetAnchored < mContentAnchoredXMin)
                    targetAnchored = mContentAnchoredXMin;
            }

            var contentAnchored = mScrollView.content.anchoredPosition;

            if (pImediate)
            {
                contentAnchored.x = targetAnchored;
                mScrollView.content.anchoredPosition = contentAnchored;
            }
            else
            {
                mIsSnapping = false;

                float time = mDistance / (pSpeed / Time.deltaTime);
                if (time == 0)
                    return;

#if USE_DOTWEEN
                DOTween.Kill(GetInstanceID());
                mScrollView.content.DOAnchorPosX(targetAnchored, time)
                    .OnStart(() => { mIsSnapping = true; })
                    .OnComplete(() =>
                    {
                        mIsSnapping = false;
                        contentAnchored.x = targetAnchored;
                        mScrollView.content.anchoredPosition = contentAnchored;
                    }).SetId(GetInstanceID());
#else
                contentAnchored.x = targetAnchored;
                mScrollView.content.anchoredPosition = contentAnchored;
#endif
            }
        }

        public void MoveToItem(int pIndex)
        {
            mScrollView.StopMovement();

            float itemAnchorX = mItems[pIndex].anchoredPosition.x;
            float itemBorderLeft = itemAnchorX - mItems[mNearestIndex].rect.width * mItems[mNearestIndex].pivot.x;
            float targetAnchor = itemBorderLeft *= -1;

            if ((itemBorderLeft > mContentAnchoredXMax || itemBorderLeft < mContentAnchoredXMin)
                && (mScrollView.movementType == ScrollRect.MovementType.Elastic || mScrollView.movementType == ScrollRect.MovementType.Clamped))
            {
                return;
            }
            else
            {
                if (targetAnchor > mContentAnchoredXMax)
                    targetAnchor = mContentAnchoredXMax;
                if (targetAnchor < mContentAnchoredXMin)
                    targetAnchor = mContentAnchoredXMin;
            }

            var contentAnchor = mScrollView.content.anchoredPosition;
            contentAnchor.x = targetAnchor;
            mScrollView.content.anchoredPosition = contentAnchor;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
#if USE_DOTWEEN
            DOTween.Kill(GetInstanceID());
#endif
            mIsDraging = true;
            mIsSnapping = false;
            mBeginDragPosition = mScrollView.content.anchoredPosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            mIsDraging = false;

            if (OutOfBoundary())
                return;

            var endDragPosition = mScrollView.content.anchoredPosition;
            mDragLeftToRight = mBeginDragPosition.x < endDragPosition.x;
            mDragDistance = Math.Abs(mBeginDragPosition.x - endDragPosition.x);

            float speedX = Math.Abs(mVelocity.x);
            if (speedX <= mSpringSpeed)
            {
                FindNearestItem();
                CheckMinScrollReaction();
                MoveToNearest(false, speedX);
            }
        }

        private bool OutOfBoundary()
        {
            var contentAnchor = mScrollView.content.anchoredPosition;
            return contentAnchor.x >= mContentAnchoredXMax - 20 || contentAnchor.x <= mContentAnchoredXMin + 20;
        }

        public void OnDrag(PointerEventData eventData)
        {

        }

        private void CheckMinScrollReaction()
        {
            if (mDragDistance > mMinScrollReaction)
            {
                float itemAnchoredX = mItems[mNearestIndex].anchoredPosition.x;
                float itemAnchoredX_left = itemAnchoredX - mItems[mNearestIndex].rect.width * mItems[mNearestIndex].pivot.x;
                //float rightBorderX = itemAnchorX + mItems[mNearestIndex].rect.width * (1 - mItems[mNearestIndex].pivot.x);
                //float itemCenter = itemLeftBorder + mItems[mNearestIndex].rect.width / 2f;
                if (mDragLeftToRight
                    && mScrollView.content.anchoredPosition.x > itemAnchoredX_left * -1
                    && mNearestIndex > 0)
                {
                    mNearestIndex--;
                    float distanceX = mScrollView.content.anchoredPosition.x - itemAnchoredX_left * -1;
                    mDistance = Mathf.Abs(distanceX);
                }
                else if (!mDragLeftToRight
                    && mScrollView.content.anchoredPosition.x < itemAnchoredX_left * -1
                    && mNearestIndex < mItems.Count - 1)
                {
                    mNearestIndex++;
                    float distanceX = mScrollView.content.anchoredPosition.x - itemAnchoredX_left * -1;
                    mDistance = Mathf.Abs(distanceX);
                }
            }
        }
    }
}