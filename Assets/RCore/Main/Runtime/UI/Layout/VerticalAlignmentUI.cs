/***
 * Author RaBear - HNB - 2017
 **/

using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
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

		[FormerlySerializedAs("alignmentType")]
		[SerializeField] private Alignment m_alignmentType;
		[FormerlySerializedAs("rowDistance")]
		[SerializeField] private float m_rowDistance;
		[FormerlySerializedAs("tweenTime")]
		[SerializeField] private float m_tweenTime = 0.25f;
		[FormerlySerializedAs("autoWhenStart")]
		[SerializeField] private bool m_autoWhenStart;
		[FormerlySerializedAs("lerp")]
		[SerializeField, Range(0, 1f)] private float m_lerp;
		[FormerlySerializedAs("moveFromRoot")]
		[SerializeField] private bool m_moveFromRoot;
		[FormerlySerializedAs("animCurve")]
		[SerializeField] private AnimationCurve m_animCurve;

		[Header("Optional")]
		[FormerlySerializedAs("xOffset")]
		[SerializeField] private float m_xOffset;

		private RectTransform[] m_children;
		private Vector3[] m_childrenPrePosition;
		private Vector3[] m_childrenNewPosition;
		private AnimationCurve m_animCurveTemp;
#if DOTWEEN
		private Tweener m_tweener;
#else
        private Coroutine m_coroutine;
#endif

		private void Start()
		{
			if (m_autoWhenStart)
				Align();
		}

		private void OnValidate()
		{
			if (Application.isPlaying)
				return;

			Init();
			RefreshPositions();

			float t = m_lerp;
			if (m_animCurve.length > 1)
				t = m_animCurve.Evaluate(m_lerp);
			for (int j = 0; j < m_children.Length; j++)
			{
				var pos = Vector3.Lerp(m_childrenPrePosition[j], m_childrenNewPosition[j], t);
				m_children[j].anchoredPosition = pos;
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
			m_children = list.ToArray();
		}

		private void RefreshPositions()
		{
			m_childrenPrePosition = new Vector3[m_children.Length];
			m_childrenNewPosition = new Vector3[m_children.Length];
			switch (m_alignmentType)
			{
				case Alignment.Top:
					for (int i = 0; i < m_childrenNewPosition.Length; i++)
					{
						if (!m_moveFromRoot)
							m_childrenPrePosition[i] = m_children[i].anchoredPosition;
						m_childrenNewPosition[i] = i * new Vector3(m_xOffset, m_rowDistance, 0);
					}
					break;

				case Alignment.Bottom:
					for (int i = 0; i < m_childrenNewPosition.Length; i++)
					{
						if (!m_moveFromRoot)
							m_childrenPrePosition[i] = m_children[i].anchoredPosition;
						m_childrenNewPosition[i] = new Vector3(m_xOffset, m_rowDistance, 0) * ((m_childrenNewPosition.Length - 1 - i) * -1);
					}
					break;

				case Alignment.Center:
					for (int i = 0; i < m_childrenNewPosition.Length; i++)
					{
						if (!m_moveFromRoot)
							m_childrenPrePosition[i] = m_children[i].anchoredPosition;
						m_childrenNewPosition[i] = i * new Vector3(m_xOffset, m_rowDistance, 0);
					}
					for (int i = 0; i < m_childrenNewPosition.Length; i++)
					{
						m_childrenNewPosition[i] = new Vector3(
							m_childrenNewPosition[i].x + m_xOffset,
							m_childrenNewPosition[i].y - m_childrenNewPosition[m_childrenNewPosition.Length - 1].y / 2,
							m_childrenNewPosition[i].z);
					}
					break;
			}
		}

		public void Align()
		{
			Init();
			RefreshPositions();
			m_lerp = 1;

			for (int i = 0; i < m_children.Length; i++)
				m_children[i].anchoredPosition = m_childrenNewPosition[i];
		}

		[InspectorButton]
		private void AlignByTweener()
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
			m_animCurveTemp = pCurve ?? this.m_animCurve;
#if DOTWEEN
			bool waiting = true;
			m_lerp = 0;
			m_tweener.Kill();
			m_tweener = DOTween.To(val => m_lerp = val, 0f, 1f, m_tweenTime)
				.OnUpdate(() =>
				{
					float t = m_lerp;
					if (m_animCurveTemp.length > 1)
						t = m_animCurveTemp.Evaluate(m_lerp);
					for (int j = 0; j < m_children.Length; j++)
					{
						var pos = Vector3.Lerp(m_childrenPrePosition[j], m_childrenNewPosition[j], t);
						m_children[j].anchoredPosition = pos;
					}
				})
				.OnComplete(() =>
				{
					waiting = false;
				})
				.SetUpdate(true);
			if (m_animCurveTemp == null)
				m_tweener.SetEase(Ease.InQuint);
			while (waiting)
				yield return null;
#else
            if (m_coroutine != null)
                StopCoroutine(m_coroutine);
            m_coroutine = StartCoroutine(IEArrangeChildren(m_childrenPrePosition, m_childrenNewPosition, m_tweenTime));
            yield return m_coroutine;
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
				m_lerp = time / pDuration;
				float t = m_lerp;
				if (m_animCurveTemp.length > 1)
					t = m_animCurveTemp.Evaluate(m_lerp);
				for (int j = 0; j < m_children.Length; j++)
				{
					var pos = Vector3.Lerp(pChildrenPrePosition[j], pChildrenNewPosition[j], t);
					m_children[j].anchoredPosition = pos;
				}
				if (m_lerp >= 1)
					break;
				yield return null;
				time += Time.deltaTime;
			}
		}
	}
}