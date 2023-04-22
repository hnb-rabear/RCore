/**
 * Author RadBear - nbhung71711 @gmail.com - 2017
 **/
#pragma warning disable 0649
//#define USE_DOTWEEN

#if USE_DOTWEEN
using DG.Tweening;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Components
{
    class HorizontalAlignmentUI : MyAlignment
    {
        public enum Alignment
        {
            Left,
            Right,
            Center,
        }

        public float maxContainerWidth;
        public Alignment alignmentType;
        public float cellDistance;
        public float tweenTime = 0.25f;
        public bool autoWhenStart;
        [Header("Optional")]
        public float yOffset;
        [Range(0, 1f)] public float lerp;
        public bool moveFromRoot;
        public AnimationCurve animCurve;

        private List<RectTransform> mChildren = new List<RectTransform>();
        private List<bool> mIndexesChanged = new List<bool>();
        private List<int> mChildrenId = new List<int>();
        private Vector2[] mChildrenPrePosition;
        private Vector2[] mChildrenNewPosition;
#if USE_DOTWEEN
        private Tweener mTweener;
#else
        private Coroutine mCoroutine;
#endif
        private void Start()
        {
            if (autoWhenStart)
                Align();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            Init();
            RefreshPositions();

            float t = lerp;
            if (animCurve.length > 1)
                t = animCurve.Evaluate(lerp);
            for (int j = 0; j < mChildren.Count; j++)
            {
                var pos = Vector3.Lerp(mChildrenPrePosition[j], mChildrenNewPosition[j], t);
                mChildren[j].anchoredPosition = pos;
            }
        }

        private void Init()
        {
            var childrenTemp = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.gameObject.activeSelf)
                    childrenTemp.Add(child);
            }
            for (int i = 0; i < childrenTemp.Count; i++)
            {
                if (i > mChildren.Count - 1)
                    mChildren.Add(null);

                if (i > mIndexesChanged.Count - 1)
                    mIndexesChanged.Add(true);

                if (i > mChildrenId.Count - 1)
                    mChildrenId.Add(0);

                if (mChildrenId[i] != childrenTemp[i].gameObject.GetInstanceID())
                {
                    mChildrenId[i] = childrenTemp[i].gameObject.GetInstanceID();
                    mIndexesChanged[i] = true;
                }
                else
                {
                    mIndexesChanged[i] = false;
                }
            }
            for (int i = mChildrenId.Count - 1; i >= 0; i--)
            {
                if (i > childrenTemp.Count - 1)
                {
                    mChildrenId.RemoveAt(i);
                    mChildren.RemoveAt(i);
                    mIndexesChanged.RemoveAt(i);
                    continue;
                }
                if (mIndexesChanged[i] || mChildren[i] == null)
                {
                    mChildren[i] = childrenTemp[i].transform as RectTransform;
                    mIndexesChanged[i] = false;
                }
            }
        }

        private void RefreshPositions()
        {
            if (Math.Abs(mChildren.Count * cellDistance) > maxContainerWidth && maxContainerWidth > 0)
                cellDistance *= maxContainerWidth / (Math.Abs(mChildren.Count * cellDistance));

            mChildrenNewPosition = new Vector2[mChildren.Count];
            mChildrenPrePosition = new Vector2[mChildren.Count];
            switch (alignmentType)
            {
                case Alignment.Left:
                    for (int i = 0; i < mChildren.Count; i++)
                    {
                        if (!moveFromRoot)
                            mChildrenPrePosition[i] = mChildren[i].anchoredPosition;
                        mChildrenNewPosition[i] = i * new Vector2(cellDistance, yOffset);
                    }
                    break;

                case Alignment.Right:
                    for (int i = 0; i < mChildren.Count; i++)
                    {
                        if (!moveFromRoot)
                            mChildrenPrePosition[i] = mChildren[i].anchoredPosition;
                        mChildrenNewPosition[i] = (mChildren.Count - 1 - i) * new Vector2(cellDistance, yOffset) * -1;
                    }
                    break;

                case Alignment.Center:
                    for (int i = 0; i < mChildren.Count; i++)
                    {
                        if (!moveFromRoot)
                            mChildrenPrePosition[i] = mChildren[i].anchoredPosition;
                        mChildrenNewPosition[i] = i * new Vector2(cellDistance, yOffset);
                    }
                    for (int i = 0; i < mChildren.Count; i++)
                    {
                        mChildrenNewPosition[i] = new Vector2(
                            mChildrenNewPosition[i].x - mChildrenNewPosition[mChildren.Count - 1].x / 2,
                            mChildrenNewPosition[i].y + yOffset);
                    }
                    break;
            }
        }

        public override void Align()
        {
            Init();
            RefreshPositions();
            lerp = 1;

            for (int i = 0; i < mChildren.Count; i++)
                mChildren[i].anchoredPosition = mChildrenNewPosition[i];
        }

        public override void AlignByTweener(Action onFinish, AnimationCurve pCurve = null)
        {
            StartCoroutine(IEAlignByTweener(onFinish, pCurve));
        }

        private IEnumerator IEAlignByTweener(Action onFinish, AnimationCurve pCurve = null)
        {
            Init();
            RefreshPositions();
            if (pCurve != null)
                animCurve = pCurve;
#if USE_DOTWEEN
            bool waiting = true;
            lerp = 0;
            mTweener.Kill();
            mTweener = DOTween.To(tweenVal => lerp = tweenVal, 0f, 1f, tweenTime)
                .OnUpdate(() =>
                {
                    float t = lerp;
                    if (animCurve.length > 1)
                        t = animCurve.Evaluate(lerp);
                    for (int j = 0; j < mChildren.Count; j++)
                    {
                        Vector2 pos = Vector2.Lerp(mChildrenPrePosition[j], mChildrenNewPosition[j], t);
                        mChildren[j].anchoredPosition = pos;
                    }
                })
                .OnComplete(() =>
                {
                    waiting = false;
                })
                .SetUpdate(true);
            if (animCurve == null)
                mTweener.SetEase(Ease.InQuint);
            while (waiting)
                yield return null;
#else
            if (mCoroutine != null)
                StopCoroutine(mCoroutine);
            mCoroutine = StartCoroutine(IEArrangeChildren(mChildrenPrePosition, mChildrenNewPosition, tweenTime));
            yield return mCoroutine;
#endif
            onFinish?.Invoke();
        }

        private IEnumerator IEArrangeChildren(Vector2[] pChildrenPrePosition, Vector2[] pChildrenNewPosition, float pDuration)
        {
            float time = 0;
            while (true)
            {
                if (time >= pDuration)
                    time = pDuration;
                lerp = time / pDuration;
                float t = lerp;
                if (animCurve.length > 1)
                    t = animCurve.Evaluate(lerp);
                for (int j = 0; j < mChildren.Count; j++)
                {
                    var pos = Vector2.Lerp(pChildrenPrePosition[j], pChildrenNewPosition[j], t);
                    mChildren[j].anchoredPosition = pos;
                }
                if (lerp >= 1)
                    break;
                yield return null;
                time += Time.unscaledDeltaTime;
            }
        }
    }
}
