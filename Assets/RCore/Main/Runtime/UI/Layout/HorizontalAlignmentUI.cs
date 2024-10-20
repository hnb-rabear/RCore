/**
 * Author RadBear - nbhung71711 @gmail.com - 2017
 **/
#pragma warning disable 0649

#if DOTWEEN
using DG.Tweening;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.UI
{
	public class HorizontalAlignmentUI : MonoBehaviour, IAligned
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

		private List<RectTransform> m_children = new List<RectTransform>();
		private List<bool> m_indexesChanged = new List<bool>();
		private List<int> m_childrenId = new List<int>();
		private Vector2[] m_childrenPrePosition;
		private Vector2[] m_childrenNewPosition;
#if DOTWEEN
		private Tweener m_tweener;
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
			for (int j = 0; j < m_children.Count; j++)
			{
				var pos = Vector3.Lerp(m_childrenPrePosition[j], m_childrenNewPosition[j], t);
				m_children[j].anchoredPosition = pos;
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
				if (i > m_children.Count - 1)
					m_children.Add(null);

				if (i > m_indexesChanged.Count - 1)
					m_indexesChanged.Add(true);

				if (i > m_childrenId.Count - 1)
					m_childrenId.Add(0);

				if (m_childrenId[i] != childrenTemp[i].gameObject.GetInstanceID())
				{
					m_childrenId[i] = childrenTemp[i].gameObject.GetInstanceID();
					m_indexesChanged[i] = true;
				}
				else
				{
					m_indexesChanged[i] = false;
				}
			}
			for (int i = m_childrenId.Count - 1; i >= 0; i--)
			{
				if (i > childrenTemp.Count - 1)
				{
					m_childrenId.RemoveAt(i);
					m_children.RemoveAt(i);
					m_indexesChanged.RemoveAt(i);
					continue;
				}
				if (m_indexesChanged[i] || m_children[i] == null)
				{
					m_children[i] = childrenTemp[i].transform as RectTransform;
					m_indexesChanged[i] = false;
				}
			}
		}

		private void RefreshPositions()
		{
			if (Math.Abs(m_children.Count * cellDistance) > maxContainerWidth && maxContainerWidth > 0)
				cellDistance *= maxContainerWidth / (Math.Abs(m_children.Count * cellDistance));
			
			m_childrenNewPosition = new Vector2[m_children.Count];
			m_childrenPrePosition = new Vector2[m_children.Count];
			switch (alignmentType)
			{
				case Alignment.Left:
					for (int i = 0; i < m_children.Count; i++)
					{
						if (!moveFromRoot)
							m_childrenPrePosition[i] = m_children[i].anchoredPosition;
						m_childrenNewPosition[i] = i * new Vector2(cellDistance, yOffset);
					}
					break;

				case Alignment.Right:
					for (int i = 0; i < m_children.Count; i++)
					{
						if (!moveFromRoot)
							m_childrenPrePosition[i] = m_children[i].anchoredPosition;
						m_childrenNewPosition[i] = new Vector2(cellDistance, yOffset) * ((m_children.Count - 1 - i) * -1);
					}
					break;

				case Alignment.Center:
					for (int i = 0; i < m_children.Count; i++)
					{
						if (!moveFromRoot)
							m_childrenPrePosition[i] = m_children[i].anchoredPosition;
						m_childrenNewPosition[i] = i * new Vector2(cellDistance, yOffset);
					}
					for (int i = 0; i < m_children.Count; i++)
					{
						m_childrenNewPosition[i] = new Vector2(
							m_childrenNewPosition[i].x - m_childrenNewPosition[m_children.Count - 1].x / 2,
							m_childrenNewPosition[i].y + yOffset);
					}
					break;
			}
		}

		public void Align()
		{
			Init();
			RefreshPositions();
			lerp = 1;

			for (int i = 0; i < m_children.Count; i++)
				m_children[i].anchoredPosition = m_childrenNewPosition[i];
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
			m_tweener.Kill();
			m_tweener = DOTween.To(tweenVal => lerp = tweenVal, 0f, 1f, tweenTime)
				.OnUpdate(() =>
				{
					float t = lerp;
					if (animCurve.length > 1)
						t = animCurve.Evaluate(lerp);
					for (int j = 0; j < m_children.Count; j++)
					{
						var pos = Vector2.Lerp(m_childrenPrePosition[j], m_childrenNewPosition[j], t);
						m_children[j].anchoredPosition = pos;
					}
				})
				.OnComplete(() =>
				{
					waiting = false;
				})
				.SetUpdate(true);
			if (animCurve == null)
				m_tweener.SetEase(Ease.InQuint);
			while (waiting)
				yield return null;
#else
            if (mCoroutine != null)
                StopCoroutine(mCoroutine);
            mCoroutine = StartCoroutine(IEArrangeChildren(m_childrenPrePosition, m_childrenNewPosition, tweenTime));
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
				for (int j = 0; j < m_children.Count; j++)
				{
					var pos = Vector2.Lerp(pChildrenPrePosition[j], pChildrenNewPosition[j], t);
					m_children[j].anchoredPosition = pos;
				}
				if (lerp >= 1)
					break;
				yield return null;
				time += Time.unscaledDeltaTime;
			}
		}
	}
}