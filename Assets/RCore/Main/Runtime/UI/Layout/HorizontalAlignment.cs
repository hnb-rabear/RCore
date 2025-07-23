/***
 * Author HNB-RaBear - 2017
 **/

using RCore.Inspector;
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

		private Transform[] m_children;
		private Vector3[] m_childrenNewPosition;
		private Vector3[] m_childrenPrePosition;
#if DOTWEEN
		private Tweener m_tweener;
#else
		private Coroutine m_coroutine;
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
			for (int j = 0; j < m_children.Length; j++)
			{
				var pos = Vector3.Lerp(m_childrenPrePosition[j], m_childrenNewPosition[j], t);
				m_children[j].localPosition = pos;
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
			m_children = list.ToArray();
		}

		private void RefreshPositions()
		{
			if (Math.Abs(m_children.Length * cellDistance) > maxContainerWidth && maxContainerWidth > 0)
				cellDistance *= maxContainerWidth / Math.Abs(m_children.Length * cellDistance);

			m_childrenPrePosition = new Vector3[m_children.Length];
			m_childrenNewPosition = new Vector3[m_children.Length];
			switch (alignmentType)
			{
				case Alignment.Left:
					for (int i = 0; i < m_children.Length; i++)
					{
						if (!moveFromRoot)
							m_childrenPrePosition[i] = m_children[i].localPosition;
						m_childrenNewPosition[i] = i * new Vector3(cellDistance, yOffset, 0);
					}
					break;

				case Alignment.Right:
					for (int i = 0; i < m_children.Length; i++)
					{
						if (!moveFromRoot)
							m_childrenPrePosition[i] = m_children[i].localPosition;
						m_childrenNewPosition[i] = new Vector3(cellDistance, yOffset, 0) * ((m_children.Length - 1 - i) * -1);
					}
					break;

				case Alignment.Center:
					for (int i = 0; i < m_children.Length; i++)
					{
						if (!moveFromRoot)
							m_childrenPrePosition[i] = m_children[i].localPosition;
						m_childrenNewPosition[i] = i * new Vector3(cellDistance, yOffset, 0);
					}
					for (int i = 0; i < m_children.Length; i++)
					{
						m_childrenNewPosition[i] = new Vector3(
							m_childrenNewPosition[i].x - m_childrenNewPosition[m_children.Length - 1].x / 2,
							m_childrenNewPosition[i].y + yOffset,
							m_childrenNewPosition[i].z);
					}
					break;
			}
		}

		public void Align()
		{
			Init();
			RefreshPositions();
			lerp = 1;

			for (int i = 0; i < m_children.Length; i++)
				m_children[i].localPosition = m_childrenNewPosition[i];
		}

		[InspectorButton]
		private void AlignByTweener()
		{
			AlignByTweener(null);
		}

		public void AlignByTweener(Action onFinish)
		{
			StartCoroutine(IEAlignByTweener(onFinish));
		}

		private IEnumerator IEAlignByTweener(Action onFinish)
		{
			Init();
			RefreshPositions();
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
					for (int j = 0; j < m_children.Length; j++)
					{
						var pos = Vector2.Lerp(m_childrenPrePosition[j], m_childrenNewPosition[j], t);
						m_children[j].localPosition = pos;
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
			if (m_coroutine != null)
				StopCoroutine(m_coroutine);
			m_coroutine = StartCoroutine(IEArrangeChildren(m_childrenPrePosition, m_childrenNewPosition, tweenTime));
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
				lerp = time / pDuration;
				float t = lerp;
				if (animCurve.length > 1)
					t = animCurve.Evaluate(lerp);
				for (int j = 0; j < m_children.Length; j++)
				{
					var pos = Vector3.Lerp(pChildrenPrePosition[j], pChildrenNewPosition[j], t);
					m_children[j].localPosition = pos;
				}
				if (lerp >= 1)
					break;
				yield return null;
				time += Time.deltaTime;
			}
		}
	}
}