/***
 * Author RadBear - nbhung71711 @gmail.com - 2017
 **/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using RCore.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace RCore
{
	public class HorizontalAlignment : MonoBehaviour, IAligned
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
		[Range(0, 1f)] public float lerp;
		public bool moveFromRoot;
		public AnimationCurve animCurve;

		[Header("Optional")] 
		public float yOffset;

		private Transform[] mChildren;
		private Vector3[] mChildrenNewPosition;
		private Vector3[] mChildrenPrePosition;
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
			if (animCurve != null && animCurve.length > 1)
				t = animCurve.Evaluate(lerp);
			for (int j = 0; j < mChildren.Length; j++)
			{
				var pos = Vector3.Lerp(mChildrenPrePosition[j], mChildrenNewPosition[j], t);
				mChildren[j].localPosition = pos;
			}
		}

		private void Init()
		{
			var list = new List<Transform>();
			for (int i = 0; i < transform.childCount; i++)
			{
				var child = transform.GetChild(i);
				if (child.gameObject.activeSelf)
					list.Add(transform.GetChild(i));
			}
			mChildren = list.ToArray();
		}

		private void RefreshPositions()
		{
			if (Math.Abs(mChildren.Length * cellDistance) > maxContainerWidth && maxContainerWidth > 0)
				cellDistance *= maxContainerWidth / Math.Abs(mChildren.Length * cellDistance);

			mChildrenPrePosition = new Vector3[mChildren.Length];
			mChildrenNewPosition = new Vector3[mChildren.Length];
			switch (alignmentType)
			{
				case Alignment.Left:
					for (int i = 0; i < mChildren.Length; i++)
					{
						if (!moveFromRoot)
							mChildrenPrePosition[i] = mChildren[i].localPosition;
						mChildrenNewPosition[i] = i * new Vector3(cellDistance, yOffset, 0);
					}
					break;

				case Alignment.Right:
					for (int i = 0; i < mChildren.Length; i++)
					{
						if (!moveFromRoot)
							mChildrenPrePosition[i] = mChildren[i].localPosition;
						mChildrenNewPosition[i] = new Vector3(cellDistance, yOffset, 0) * ((mChildren.Length - 1 - i) * -1);
					}
					break;

				case Alignment.Center:
					for (int i = 0; i < mChildren.Length; i++)
					{
						if (!moveFromRoot)
							mChildrenPrePosition[i] = mChildren[i].localPosition;
						mChildrenNewPosition[i] = i * new Vector3(cellDistance, yOffset, 0);
					}
					for (int i = 0; i < mChildren.Length; i++)
					{
						mChildrenNewPosition[i] = new Vector3(
						mChildrenNewPosition[i].x - mChildrenNewPosition[mChildren.Length - 1].x / 2,
						mChildrenNewPosition[i].y + yOffset,
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
				mChildren[i].localPosition = mChildrenNewPosition[i];
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
            mTweener = DOTween.To(tweenVal => lerp = tweenVal, 0f, 1f, tweenTime)
                .OnUpdate(() =>
                {
                    float t = lerp;
                    if (animCurve.length > 1)
                        t = animCurve.Evaluate(lerp);
                    for (int j = 0; j < mChildren.Length; j++)
                    {
                        var pos = Vector2.Lerp(mChildrenPrePosition[j], mChildrenNewPosition[j], t);
                        mChildren[j].localPosition = pos;
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
					mChildren[j].localPosition = pos;
				}
				if (lerp >= 1)
					break;
				yield return null;
				time += Time.deltaTime;
			}
		}
	}
}