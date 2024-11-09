/***
 * Author RaBear - HNB - 2019
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RCore.Inspector;
using RCore.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace RCore
{
	public class TableAlignment : MonoBehaviour, IAligned
	{
		public enum TableLayoutType
		{
			Horizontal,
			Vertical
		}

		public enum Alignment
		{
			Top,
			Bottom,
			Left,
			Right,
			Center,
		}

		public TableLayoutType tableLayoutType;
		public Alignment alignmentType;
		public float tweenTime = 0.25f;
		public AnimationCurve animCurve;

		[Space(10)]
		public int maxRow;

		[Space(10)]
		public int maxColumn;

		[Space(10)]
		public float columnDistance;
		public float rowDistance;

		[ReadOnly] public float width;
		[ReadOnly] public float height;

		private Dictionary<int, List<Transform>> m_childrenGroup;
		private AnimationCurve m_animCurveTemp;

		public void Init()
		{
			int totalRow = 0;
			int totalCol = 0;

			var allChildren = new List<Transform>();
			for (int i = 0; i < transform.childCount; i++)
			{
				var child = transform.GetChild(i);
				if (child.gameObject.activeSelf)
					allChildren.Add(transform.GetChild(i));
			}

			m_childrenGroup = new Dictionary<int, List<Transform>>();
			if (tableLayoutType == TableLayoutType.Horizontal)
			{
				if (maxColumn == 0)
					maxColumn = 1;

				totalRow = Mathf.CeilToInt(allChildren.Count * 1f / maxColumn);
				totalCol = Mathf.CeilToInt(allChildren.Count * 1f / totalRow);
				int row = 0;
				while (allChildren.Count > 0)
				{
					for (int i = 0; i < maxColumn; i++)
					{
						if (allChildren.Count == 0)
							break;

						if (!m_childrenGroup.ContainsKey(row))
							m_childrenGroup.Add(row, new List<Transform>());
						m_childrenGroup[row].Add(allChildren[0]);
						allChildren.RemoveAt(0);
					}
					row++;
				}
			}
			else
			{
				if (maxRow == 0)
					maxRow = 1;

				totalCol = Mathf.CeilToInt(allChildren.Count * 1f / maxRow);
				totalRow = Mathf.CeilToInt(allChildren.Count * 1f / totalCol);
				int col = 0;
				while (allChildren.Count > 0)
				{
					for (int i = 0; i < maxRow; i++)
					{
						if (allChildren.Count == 0)
							break;

						if (!m_childrenGroup.ContainsKey(col))
							m_childrenGroup.Add(col, new List<Transform>());
						m_childrenGroup[col].Add(allChildren[0]);
						allChildren.RemoveAt(0);
					}
					col++;
				}
			}

			width = (totalCol - 1) * columnDistance;
			height = (totalRow - 1) * rowDistance;
		}

		public void Align()
		{
			Init();

			if (tableLayoutType == TableLayoutType.Horizontal)
			{
				foreach (var a in m_childrenGroup)
				{
					var children = a.Value;
					float y = a.Key * rowDistance;

					switch (alignmentType)
					{
						case Alignment.Left:
							for (int i = 0; i < children.Count; i++)
							{
								var pos = i * new Vector3(columnDistance, 0, 0);
								pos.y = y - height / 2f;
								children[i].localPosition = pos;
							}
							break;

						case Alignment.Right:
							for (int i = 0; i < children.Count; i++)
							{
								var pos = (children.Count - 1 - i) * new Vector3(columnDistance, 0, 0) * -1;
								pos.y = y - height / 2f;
								children[i].localPosition = pos;
							}
							break;

						case Alignment.Center:
							for (int i = 0; i < children.Count; i++)
							{
								var pos = i * new Vector3(columnDistance, 0, 0);
								pos.y = y - height / 2f;
								children[i].localPosition = pos;
							}
							for (int i = 0; i < children.Count; i++)
							{
								children[i].localPosition = new Vector3(
									children[i].localPosition.x - children[children.Count - 1].localPosition.x / 2,
									children[i].localPosition.y,
									children[i].localPosition.z);
							}
							break;
					}
				}
			}
			else
			{
				foreach (var a in m_childrenGroup)
				{
					var children = a.Value;
					float x = a.Key * columnDistance;

					switch (alignmentType)
					{
						case Alignment.Top:
							for (int i = 0; i < children.Count; i++)
							{
								var pos = (children.Count - 1 - i) * new Vector3(0, rowDistance, 0) * -1;
								pos.x = x - width / 2f;
								children[i].transform.localPosition = pos;
							}
							break;

						case Alignment.Bottom:
							for (int i = 0; i < children.Count; i++)
							{
								var pos = i * new Vector3(0, rowDistance, 0);
								pos.x = x - width / 2f;
								children[i].transform.localPosition = pos;
							}
							break;

						case Alignment.Center:
							for (int i = 0; i < children.Count; i++)
							{
								var pos = i * new Vector3(0, rowDistance, 0);
								pos.x = x - width / 2f;
								children[i].transform.localPosition = pos;
							}
							for (int i = 0; i < children.Count; i++)
							{
								children[i].transform.localPosition = new Vector3(
									children[i].localPosition.x,
									children[i].localPosition.y - children[children.Count - 1].localPosition.y / 2,
									children[i].localPosition.z);
							}
							break;
					}
				}
			}
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
			m_animCurveTemp = pCurve ?? this.animCurve;
			if (tableLayoutType == TableLayoutType.Horizontal)
			{
				foreach (var a in m_childrenGroup)
				{
					var children = a.Value;
					float y = a.Key * rowDistance;

					var childrenNewPosition = new Vector3[children.Count];
					var childrenPrePosition = new Vector3[children.Count];
					switch (alignmentType)
					{
						case Alignment.Left:
							for (int i = 0; i < children.Count; i++)
							{
								childrenPrePosition[i] = children[i].localPosition;
								var pos = i * new Vector3(columnDistance, 0, 0);
								pos.y = y - height / 2f;
								childrenNewPosition[i] = pos;
							}
							break;

						case Alignment.Right:
							for (int i = 0; i < children.Count; i++)
							{
								childrenPrePosition[i] = children[i].localPosition;
								var pos = (children.Count - 1 - i) * new Vector3(columnDistance, 0, 0) * -1;
								pos.y = y - height / 2f;
								childrenNewPosition[i] = pos;
							}
							break;

						case Alignment.Center:
							for (int i = 0; i < children.Count; i++)
							{
								childrenPrePosition[i] = children[i].localPosition;
								var pos = i * new Vector3(columnDistance, 0, 0);
								pos.y = y - height / 2f;
								childrenNewPosition[i] = pos;
							}
							for (int i = 0; i < childrenNewPosition.Length; i++)
							{
								childrenNewPosition[i] = new Vector3(
									childrenNewPosition[i].x - childrenNewPosition[children.Count - 1].x / 2,
									childrenNewPosition[i].y,
									childrenNewPosition[i].z);
							}
							break;
					}

#if DOTWEEN
					bool waiting = true;
					float lerp = 0;
					DOTween.Kill(GetInstanceID() + a.Key);
					DOTween.To(val => lerp = val, 0f, 1f, tweenTime)
						.OnUpdate(() =>
						{
							float t = lerp;
							if (m_animCurveTemp.length > 1)
								t = m_animCurveTemp.Evaluate(lerp);
							for (int j = 0; j < children.Count; j++)
							{
								var pos = Vector2.Lerp(childrenPrePosition[j], childrenNewPosition[j], t);
								children[j].localPosition = pos;
							}
						})
						.OnComplete(() =>
						{
							waiting = false;
						})
						.SetUpdate(true)
						.SetEase(Ease.InQuint)
						.SetId(GetInstanceID() + a.Key);
					while (waiting)
						yield return null;
#else
                    yield return StartCoroutine(IEArrangeChildren(children, childrenPrePosition, childrenNewPosition, tweenTime));
#endif
				}
			}
			else
			{
				foreach (var a in m_childrenGroup)
				{
					var children = a.Value;
					float x = a.Key * columnDistance;

					var childrenPrePosition = new Vector3[children.Count];
					var childrenNewPosition = new Vector3[children.Count];
					switch (alignmentType)
					{
						case Alignment.Top:
							for (int i = 0; i < children.Count; i++)
							{
								childrenPrePosition[i] = children[i].localPosition;
								var pos = i * new Vector3(0, rowDistance, 0);
								pos.x = x - width / 2f;
								childrenNewPosition[i] = pos;
							}
							break;

						case Alignment.Bottom:
							for (int i = 0; i < children.Count; i++)
							{
								childrenPrePosition[i] = children[i].localPosition;
								var pos = (childrenNewPosition.Length - 1 - i) * new Vector3(0, rowDistance, 0) * -1;
								pos.x = x - width / 2f;
								childrenNewPosition[i] = pos;
							}
							break;

						case Alignment.Center:
							for (int i = 0; i < children.Count; i++)
							{
								childrenPrePosition[i] = children[i].localPosition;
								var pos = i * new Vector3(0, rowDistance, 0);
								pos.x = x - width / 2f;
								childrenNewPosition[i] = pos;
							}
							for (int i = 0; i < childrenNewPosition.Length; i++)
							{
								childrenNewPosition[i] = new Vector3(
									childrenNewPosition[i].x,
									childrenNewPosition[i].y - childrenNewPosition[childrenNewPosition.Length - 1].y / 2,
									childrenNewPosition[i].z);
							}
							break;
					}

#if DOTWEEN
					bool waiting = true;
					float lerp = 0;
					DOTween.Kill(GetInstanceID() + a.Key);
					DOTween.To(val => lerp = val, 0f, 1f, tweenTime)
						.OnUpdate(() =>
						{
							float t = lerp;
							if (m_animCurveTemp.length > 1)
								t = m_animCurveTemp.Evaluate(lerp);
							for (int j = 0; j < children.Count; j++)
							{
								var pos = Vector3.Lerp(childrenPrePosition[j], childrenNewPosition[j], t);
								children[j].localPosition = pos;
							}
						})
						.OnComplete(() =>
						{
							waiting = false;
						})
						.SetUpdate(true)
						.SetEase(Ease.InQuint)
						.SetId(GetInstanceID() + a.Key);
					while (waiting)
						yield return null;
#else
                    yield return StartCoroutine(IEArrangeChildren(children, childrenPrePosition, childrenNewPosition, tweenTime));
#endif
				}
			}

			onFinish?.Invoke();
		}

		private IEnumerator IEArrangeChildren(List<Transform> pObjs, Vector3[] pChildrenPrePosition, Vector3[] pChildrenNewPosition, float pDuration)
		{
			float time = 0;
			while (true)
			{
				if (time >= pDuration)
					time = pDuration;
				float lerp = time / pDuration;
				float t = lerp;
				if (m_animCurveTemp.length > 1)
					t = m_animCurveTemp.Evaluate(lerp);
				for (int j = 0; j < pObjs.Count; j++)
				{
					var pos = Vector3.Lerp(pChildrenPrePosition[j], pChildrenNewPosition[j], t);
					pObjs[j].localPosition = pos;
				}
				if (lerp >= 1)
					break;
				yield return null;
				time += Time.deltaTime;
			}
		}
	}
}