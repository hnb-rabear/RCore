/**
 * Author HNB-RaBear - 2017
 **/
#pragma warning disable 0649

#if DOTWEEN
using DG.Tweening;
#endif
using RCore.Inspector;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

		[SerializeField] private float m_maxContainerWidth;
		[SerializeField] private Alignment m_alignmentType;
		[SerializeField] private float m_cellDistance;
		[SerializeField] private float m_tweenTime = 0.25f;
		[SerializeField] private bool m_autoWhenStart;
		
		[Separator("Optional Config")]
		[SerializeField] private float m_height;
		[SerializeField] private AnimationCurve m_heightCurve;
		[SerializeField] private bool m_moveFromRoot;
		[SerializeField] private AnimationCurve m_animCurve;
		[SerializeField, Range(0, 1f)] private float m_lerp;

		private List<RectTransform> m_children = new List<RectTransform>();
		private List<bool> m_indexesChanged = new List<bool>();
		private List<int> m_childrenId = new List<int>();
		private Vector2[] m_childrenPrePosition;
		private Vector2[] m_childrenNewPosition;
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
			if (Math.Abs(m_children.Count * m_cellDistance) > m_maxContainerWidth && m_maxContainerWidth > 0)
				m_cellDistance *= m_maxContainerWidth / Math.Abs(m_children.Count * m_cellDistance);
			
			m_childrenNewPosition = new Vector2[m_children.Count];
			m_childrenPrePosition = new Vector2[m_children.Count];
			switch (m_alignmentType)
			{
				case Alignment.Left:
					for (int i = 0; i < m_children.Count; i++)
					{
						if (!m_moveFromRoot)
							m_childrenPrePosition[i] = m_children[i].anchoredPosition;
						float height = m_height;
						if (m_height > 0 && m_heightCurve.length > 1)
						{
							if (m_children.Count > 2)
								height = Mathf.Lerp(0, m_height, m_heightCurve.Evaluate(i * 1f / (m_children.Count - 1)));
							else
								height = Mathf.Lerp(0, m_height, m_heightCurve.Evaluate(0.5f));
						}
						m_childrenNewPosition[i] = i * new Vector2(m_cellDistance, 0) + new Vector2(0, height);
					}
					break;

				case Alignment.Right:
					for (int i = 0; i < m_children.Count; i++)
					{
						if (!m_moveFromRoot)
							m_childrenPrePosition[i] = m_children[i].anchoredPosition;
						float height = m_height;
						if (m_height > 0 && m_heightCurve.length > 1)
						{
							if (m_children.Count > 2)
								height = Mathf.Lerp(0, m_height, m_heightCurve.Evaluate(i * 1f / (m_children.Count - 1)));
							else
								height = Mathf.Lerp(0, m_height, m_heightCurve.Evaluate(0.5f));
						}
						m_childrenNewPosition[i] = new Vector2(m_cellDistance, 0) * ((m_children.Count - 1 - i) * -1) + new Vector2(0, height);
					}
					break;

				case Alignment.Center:
					for (int i = 0; i < m_children.Count; i++)
					{
						if (!m_moveFromRoot)
							m_childrenPrePosition[i] = m_children[i].anchoredPosition;
						m_childrenNewPosition[i] = i * new Vector2(m_cellDistance, 0);
					}
					for (int i = 0; i < m_children.Count; i++)
					{
						float height = m_height;
						if (m_height > 0 && m_heightCurve.length > 1)
						{
							if (m_children.Count > 2)
								height = Mathf.Lerp(0, m_height, m_heightCurve.Evaluate(i * 1f / (m_children.Count - 1)));
							else
								height = Mathf.Lerp(0, m_height, m_heightCurve.Evaluate(0.5f));
						}
						m_childrenNewPosition[i] = new Vector2(
							m_childrenNewPosition[i].x - m_childrenNewPosition[m_children.Count - 1].x / 2,
							m_childrenNewPosition[i].y + height);
					}
					break;
			}
		}

#if ODIN_INSPECTOR
		[Button]
#else
        [InspectorButton]
#endif
		public void Align()
		{
			Init();
			RefreshPositions();
			m_lerp = 1;

			for (int i = 0; i < m_children.Count; i++)
				m_children[i].anchoredPosition = m_childrenNewPosition[i];
		}

#if ODIN_INSPECTOR
		[Button]
#else
        [InspectorButton]
#endif
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
			m_tweener = DOTween.To(tweenVal => m_lerp = tweenVal, 0f, 1f, m_tweenTime)
				.OnStart(() =>
				{
					foreach (var t in m_children)
						if(t.TryGetComponent(out ITweenItem item))
							item.OnStart();
				})
				.OnUpdate(() =>
				{
					float t = m_lerp;
					if (m_animCurveTemp.length > 1)
						t = m_animCurveTemp.Evaluate(m_lerp);
					for (int j = 0; j < m_children.Count; j++)
					{
						var pos = Vector2.Lerp(m_childrenPrePosition[j], m_childrenNewPosition[j], t);
						m_children[j].anchoredPosition = pos;
					}
				})
				.OnComplete(() =>
				{
					waiting = false;
					
					foreach (var t in m_children)
						if(t.TryGetComponent(out ITweenItem item))
							item.OnFinish();
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

		private IEnumerator IEArrangeChildren(Vector2[] pChildrenPrePosition, Vector2[] pChildrenNewPosition, float pDuration)
		{
			foreach (var t in m_children)
				if(t.TryGetComponent(out ITweenItem item))
					item.OnStart();
			
			float time = 0;
			while (true)
			{
				if (time >= pDuration)
					time = pDuration;
				m_lerp = time / pDuration;
				float t = m_lerp;
				if (m_animCurveTemp.length > 1)
					t = m_animCurveTemp.Evaluate(m_lerp);
				for (int j = 0; j < m_children.Count; j++)
				{
					var pos = Vector2.Lerp(pChildrenPrePosition[j], pChildrenNewPosition[j], t);
					m_children[j].anchoredPosition = pos;
				}
				if (m_lerp >= 1)
					break;
				yield return null;
				time += Time.unscaledDeltaTime;
			}
			
			foreach (var t in m_children)
				if(t.TryGetComponent(out ITweenItem item))
					item.OnFinish();
		}
	}
}