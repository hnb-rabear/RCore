/***
 * Author RadBear - nbhung71711 @gmail.com - 2017
 **/

using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
#if DOTWEEN
using DG.Tweening;
#endif

namespace RCore.UI
{
    public class VerticalAlignmentUI : MonoBehaviour, IAligned
    {
        public enum Alignment
        {
            Top,
            Bottom,
            Center,
        }

        public Alignment alignmentType;
        public float rowDistance;
        public float tweenTime = 0.25f;
        public bool autoWhenStart;
        [Range(0, 1f)] public float lerp;
        public bool moveFromRoot;
        public AnimationCurve animCurve;

        [Header("Optional")]
        public float xOffset;

        private RectTransform[] mChildren;
        private Vector3[] mChildrenPrePosition;
        private Vector3[] mChildrenNewPosition;
#if DOTWEEN
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
            for (int j = 0; j < mChildren.Length; j++)
            {
                var pos = Vector3.Lerp(mChildrenPrePosition[j], mChildrenNewPosition[j], t);
                mChildren[j].anchoredPosition = pos;
            }
        }

        private void Init()
        {
            var list = new List<RectTransform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.gameObject.activeSelf)
                    list.Add(transform.GetChild(i) as RectTransform);
            }
            mChildren = list.ToArray();
        }

        private void RefreshPositions()
        {
            mChildrenPrePosition = new Vector3[mChildren.Length];
            mChildrenNewPosition = new Vector3[mChildren.Length];
            switch (alignmentType)
            {
                case Alignment.Top:
                    for (int i = 0; i < mChildrenNewPosition.Length; i++)
                    {
                        if (!moveFromRoot)
                            mChildrenPrePosition[i] = mChildren[i].anchoredPosition;
                        mChildrenNewPosition[i] = i * new Vector3(xOffset, rowDistance, 0);
                    }
                    break;

                case Alignment.Bottom:
                    for (int i = 0; i < mChildrenNewPosition.Length; i++)
                    {
                        if (!moveFromRoot)
                            mChildrenPrePosition[i] = mChildren[i].anchoredPosition;
                        mChildrenNewPosition[i] = new Vector3(xOffset, rowDistance, 0) * ((mChildrenNewPosition.Length - 1 - i) * -1);
                    }
                    break;

                case Alignment.Center:
                    for (int i = 0; i < mChildrenNewPosition.Length; i++)
                    {
                        if (!moveFromRoot)
                            mChildrenPrePosition[i] = mChildren[i].anchoredPosition;
                        mChildrenNewPosition[i] = i * new Vector3(xOffset, rowDistance, 0);
                    }
                    for (int i = 0; i < mChildrenNewPosition.Length; i++)
                    {
                        mChildrenNewPosition[i] = new Vector3(
                            mChildrenNewPosition[i].x + xOffset,
                            mChildrenNewPosition[i].y - mChildrenNewPosition[mChildrenNewPosition.Length - 1].y / 2,
                            mChildrenNewPosition[i].z);
                    }
                    break;
            }
        }

        public void Align()
        {
            Init();
            RefreshPositions();
            lerp = 1;

            for (int i = 0; i < mChildren.Length; i++)
                mChildren[i].anchoredPosition = mChildrenNewPosition[i];
        }

        [InspectorButton]
        public void AlignByTweener()
        {
	        AlignByTweener(null);
        }
        
        public void AlignByTweener(Action onFinish, AnimationCurve pCurve = null)
        {
            StartCoroutine(IEAlignByTweener(onFinish, pCurve));
        }

        private IEnumerator IEAlignByTweener(Action onFinish, AnimationCurve pCurve = null)
        {
            Init();
            RefreshPositions();
            if (pCurve != null)
                animCurve = pCurve;
#if DOTWEEN
            bool waiting = true;
            lerp = 0;
            mTweener.Kill();
            mTweener = DOTween.To(val => lerp = val, 0f, 1f, tweenTime)
                .OnUpdate(() =>
                {
                    float t = lerp;
                    if (animCurve.length > 1)
                        t = animCurve.Evaluate(lerp);
                    for (int j = 0; j < mChildren.Length; j++)
                    {
                        var pos = Vector3.Lerp(mChildrenPrePosition[j], mChildrenNewPosition[j], t);
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

        private IEnumerator IEArrangeChildren(Vector3[] pChildrenPrePosition, Vector3[] pChildrenNewPosition, float pDuration)
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
                for (int j = 0; j < mChildren.Length; j++)
                {
                    var pos = Vector3.Lerp(pChildrenPrePosition[j], pChildrenNewPosition[j], t);
                    mChildren[j].anchoredPosition = pos;
                }
                if (lerp >= 1)
                    break;
                yield return null;
                time += Time.deltaTime;
            }
        }
    }
}