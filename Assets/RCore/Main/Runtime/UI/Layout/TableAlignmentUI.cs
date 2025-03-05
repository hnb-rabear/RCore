/***
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
#if DOTWEEN
using DG.Tweening;
#endif

namespace RCore.UI
{
	public class TableAlignmentUI : MonoBehaviour, IAligned
	{
		public enum TableLayoutType
		{
			Horizontal,
			Vertical
		}

		public enum AlignmentVertical
		{
			Top,
			Bottom,
			Center,
		}

		public enum AlignmentHorizontal
		{
			Left,
			Right,
			Center,
			CenterTop,
			CenterBot,
		}

		public TableLayoutType tableLayoutType;
		public float tweenTime = 0.25f;
		public AnimationCurve animCurve;
		public float columnDistance;
		public float rowDistance;

		[Header("Vertical Layout")]
		public AlignmentVertical verticalLayout;
		public int maxRow;

		[Header("Horizontal Layout")]
		public AlignmentHorizontal horizontalLayout;
		public int maxColumn;
		public bool reverseY = true;

		[Header("Additional settings")]
		[SerializeField] private bool m_AutoResizeContentX;
		[SerializeField] private bool m_AutoResizeContentY;
		[SerializeField] private Vector2 m_ContentSizeBonus;
		[SerializeField] private List<Transform> m_IgnoredObjects;

		private float m_width;
		private float m_height;
		private int m_firstGroupIndex;
		private int m_lastGroupIndex;
		private Coroutine m_coroutine;
		private Vector2 m_childTopRight = Vector2.zero;
		private Vector2 m_childBotLeft = Vector2.zero;

		private Dictionary<int, List<RectTransform>> m_childrenGroup;

		private void OnEnable()
		{
			m_childTopRight = Vector2.zero;
			m_childBotLeft = Vector2.zero;
			if (m_AutoResizeContentX || m_AutoResizeContentY)
				AutoResizeContent();
		}

		private void OnValidate()
		{
			if (Application.isPlaying)
				return;

			Align();
		}

		public void Init()
		{
			int totalRow = 0;
			int totalCol = 0;

			var allChildren = new List<RectTransform>();
			for (int i = 0; i < transform.childCount; i++)
			{
				var child = transform.GetChild(i);
				if (child.gameObject.activeSelf)
				{
					if (m_IgnoredObjects != null && m_IgnoredObjects.Contains(child))
						continue;
					allChildren.Add(transform.GetChild(i) as RectTransform);
				}
			}

			m_childrenGroup = new Dictionary<int, List<RectTransform>>();
			if (tableLayoutType == TableLayoutType.Horizontal)
			{
				if (maxColumn == 0)
					maxColumn = 1;

				totalRow = Mathf.CeilToInt(allChildren.Count * 1f / maxColumn);
				totalCol = Mathf.CeilToInt(allChildren.Count * 1f / totalRow);
				int row = 0;
				while (allChildren.Count > 0)
				{
					if (row == 0) m_firstGroupIndex = row;
					if (row > 0) m_lastGroupIndex = row;
					for (int i = 0; i < maxColumn; i++)
					{
						if (allChildren.Count == 0)
							break;

						if (!m_childrenGroup.ContainsKey(row))
							m_childrenGroup.Add(row, new List<RectTransform>());
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
					if (col == 0) m_firstGroupIndex = col;
					if (col > 0) m_lastGroupIndex = col;
					for (int i = 0; i < maxRow; i++)
					{
						if (allChildren.Count == 0)
							break;

						if (!m_childrenGroup.ContainsKey(col))
							m_childrenGroup.Add(col, new List<RectTransform>());
						m_childrenGroup[col].Add(allChildren[0]);
						allChildren.RemoveAt(0);
					}
					col++;
				}
			}

			m_width = (totalCol - 1) * columnDistance;
			m_height = (totalRow - 1) * rowDistance;
		}

#if ODIN_INSPECTOR
		[Button]
#else
		[InspectorButton]
#endif
		public void Align()
		{
			Init();

			if (tableLayoutType == TableLayoutType.Horizontal)
			{
				foreach (var a in m_childrenGroup)
				{
					var children = a.Value;
					float y = a.Key * rowDistance;
					if (reverseY) y = -y + m_height;

					switch (horizontalLayout)
					{
						case AlignmentHorizontal.Left:
							for (int i = 0; i < children.Count; i++)
							{
								var pos = i * new Vector3(columnDistance, 0, 0);
								pos.y = y - m_height / 2f;
								children[i].anchoredPosition = pos;
							}
							break;

						case AlignmentHorizontal.Right:
							for (int i = 0; i < children.Count; i++)
							{
								var pos = new Vector3(columnDistance, 0, 0) * ((children.Count - 1 - i) * -1);
								pos.y = y - m_height / 2f;
								children[i].anchoredPosition = pos;
							}
							break;

						case AlignmentHorizontal.Center:
							for (int i = 0; i < children.Count; i++)
							{
								var pos = i * new Vector3(columnDistance, 0, 0);
								pos.y = y - m_height / 2f;
								children[i].anchoredPosition = pos;
							}
							if (a.Key == m_lastGroupIndex)
							{
								foreach (var t in children)
								{
									var pos = t.anchoredPosition;
									pos.x -= columnDistance * (children.Count - 1) / 2f;
									t.anchoredPosition = pos;
								}
							}
							else
								foreach (var t in children)
								{
									t.anchoredPosition = new Vector3(
										t.anchoredPosition.x - children[children.Count - 1].anchoredPosition.x / 2,
										t.anchoredPosition.y);
								}
							break;

						case AlignmentHorizontal.CenterTop:
						{
							for (int i = 0; i < children.Count; i++)
							{
								var pos = i * new Vector3(columnDistance, 0, 0);
								pos.y = y - m_height;
								children[i].anchoredPosition = pos;
							}
							if (a.Key == m_lastGroupIndex)
							{
								foreach (var t in children)
								{
									var pos = t.anchoredPosition;
									pos.x -= columnDistance * (children.Count - 1) / 2f;
									t.anchoredPosition = pos;
								}
							}
							else
								foreach (var t in children)
								{
									t.anchoredPosition = new Vector3(
										t.anchoredPosition.x - children[children.Count - 1].anchoredPosition.x / 2,
										t.anchoredPosition.y);
								}
							break;
						}

						case AlignmentHorizontal.CenterBot:
						{
							for (int i = 0; i < children.Count; i++)
							{
								var pos = i * new Vector3(columnDistance, 0, 0);
								pos.y = y + m_height - rowDistance;
								children[i].anchoredPosition = pos;
							}
							if (a.Key == m_lastGroupIndex)
							{
								foreach (var t in children)
								{
									var pos = t.anchoredPosition;
									pos.x -= columnDistance * (children.Count - 1) / 2f;
									t.anchoredPosition = pos;
								}
							}
							else
								foreach (var t in children)
								{
									t.anchoredPosition = new Vector3(
										t.anchoredPosition.x - children[children.Count - 1].anchoredPosition.x / 2,
										t.anchoredPosition.y);
								}
							break;
						}
					}

					for (int i = 0; i < children.Count; i++)
					{
						var topRight = children[i].TopRight();
						if (topRight.x > m_childTopRight.x)
							m_childTopRight.x = topRight.x;
						if (topRight.y > m_childTopRight.y)
							m_childTopRight.y = topRight.y;

						var botLeft = children[i].BotLeft();
						if (botLeft.x < m_childBotLeft.x)
							m_childBotLeft.x = botLeft.x;
						if (botLeft.y < m_childBotLeft.y)
							m_childBotLeft.y = botLeft.y;
					}
				}
			}
			else
			{
				foreach (var a in m_childrenGroup)
				{
					var children = a.Value;
					float x = a.Key * columnDistance;

					switch (verticalLayout)
					{
						case AlignmentVertical.Top:
							for (int i = 0; i < children.Count; i++)
							{
								var pos = new Vector3(0, rowDistance, 0) * ((children.Count - 1 - i) * -1);
								pos.x = x - m_width / 2f;
								children[i].anchoredPosition = pos;
							}
							break;

						case AlignmentVertical.Bottom:
							for (int i = 0; i < children.Count; i++)
							{
								var pos = i * new Vector3(0, rowDistance, 0);
								pos.x = x - m_width / 2f;
								children[i].anchoredPosition = pos;
							}
							break;

						case AlignmentVertical.Center:
							for (int i = 0; i < children.Count; i++)
							{
								var pos = i * new Vector3(0, rowDistance, 0);
								pos.x = x - m_width / 2f;
								children[i].anchoredPosition = pos;
							}
							if (a.Key == m_lastGroupIndex && m_lastGroupIndex != m_firstGroupIndex)
							{
								for (int i = 0; i < children.Count; i++)
								{
									var pos = children[i].anchoredPosition;
									pos.y -= rowDistance * (maxRow - 1) / 2f;
									children[i].anchoredPosition = pos;
								}
							}
							else
								for (int i = 0; i < children.Count; i++)
								{
									children[i].anchoredPosition = new Vector3(
										children[i].anchoredPosition.x,
										children[i].anchoredPosition.y - children[children.Count - 1].anchoredPosition.y / 2);
								}
							break;
					}

					for (int i = 0; i < children.Count; i++)
					{
						var topRight = children[i].TopRight();
						if (topRight.x > m_childTopRight.x)
							m_childTopRight.x = topRight.x;
						if (topRight.y > m_childTopRight.y)
							m_childTopRight.y = topRight.y;

						var botLeft = children[i].BotLeft();
						if (botLeft.x < m_childBotLeft.x)
							m_childBotLeft.x = botLeft.x;
						if (botLeft.y < m_childBotLeft.y)
							m_childBotLeft.y = botLeft.y;
					}
				}
			}

			if (m_AutoResizeContentX || m_AutoResizeContentY)
				AutoResizeContent();
		}

#if ODIN_INSPECTOR
		[Button]
#else
        [InspectorButton]
#endif
		public void AutoResizeContent()
		{
			float height = m_childTopRight.y - m_childBotLeft.y + m_ContentSizeBonus.y;
			float width = m_childTopRight.x - m_childBotLeft.x + m_ContentSizeBonus.x;

			var size = ((RectTransform)transform).sizeDelta;
			if (m_AutoResizeContentX)
				size.x = width;
			if (m_AutoResizeContentY)
				size.y = height;
			((RectTransform)transform).sizeDelta = size;
		}

#if ODIN_INSPECTOR
		[Button]
#else
        [InspectorButton]
#endif
		public void AlignByTweener(Action onFinish, AnimationCurve pCurve = null)
		{
			Init();
			if (pCurve != null)
				animCurve = pCurve;

			var initialPositions = new Dictionary<int, Vector3[]>();
			var finalPositions = new Dictionary<int, Vector3[]>();

			if (tableLayoutType == TableLayoutType.Horizontal)
			{
				foreach (var a in m_childrenGroup)
				{
					var children = a.Value;
					float y = a.Key * rowDistance;
					if (reverseY) y = -y + m_height;

					var childrenNewPosition = new Vector3[children.Count];
					var childrenPrePosition = new Vector3[children.Count];
					switch (horizontalLayout)
					{
						case AlignmentHorizontal.Left:
							for (int i = 0; i < children.Count; i++)
							{
								childrenPrePosition[i] = children[i].anchoredPosition;
								var pos = i * new Vector3(columnDistance, 0, 0);
								pos.y = y - m_height / 2f;
								childrenNewPosition[i] = pos;
							}
							break;

						case AlignmentHorizontal.Right:
							for (int i = 0; i < children.Count; i++)
							{
								childrenPrePosition[i] = children[i].anchoredPosition;
								var pos = new Vector3(columnDistance, 0, 0) * ((children.Count - 1 - i) * -1);
								pos.y = y - m_height / 2f;
								childrenNewPosition[i] = pos;
							}
							break;

						case AlignmentHorizontal.Center:
							for (int i = 0; i < children.Count; i++)
							{
								childrenPrePosition[i] = children[i].anchoredPosition;
								var pos = i * new Vector3(columnDistance, 0, 0);
								pos.y = y - m_height / 2f;
								childrenNewPosition[i] = pos;
							}
							if (a.Key == m_lastGroupIndex)
								for (int i = 0; i < children.Count; i++)
								{
									var pos = childrenNewPosition[i];
									pos.x -= columnDistance * (children.Count - 1) / 2f;
									childrenNewPosition[i] = pos;
								}
							else
								for (int i = 0; i < childrenNewPosition.Length; i++)
								{
									childrenNewPosition[i] = new Vector3(
										childrenNewPosition[i].x - childrenNewPosition[children.Count - 1].x / 2,
										childrenNewPosition[i].y,
										childrenNewPosition[i].z);
								}
							break;

						case AlignmentHorizontal.CenterTop:
						{
							for (int i = 0; i < children.Count; i++)
							{
								childrenPrePosition[i] = children[i].anchoredPosition;
								var pos = i * new Vector3(columnDistance, 0, 0);
								pos.y = y - m_height;
								childrenNewPosition[i] = pos;
							}
							if (a.Key == m_lastGroupIndex)
								for (int i = 0; i < children.Count; i++)
								{
									var pos = childrenNewPosition[i];
									pos.x -= columnDistance * (children.Count - 1) / 2f;
									childrenNewPosition[i] = pos;
								}
							else
								for (int i = 0; i < childrenNewPosition.Length; i++)
								{
									childrenNewPosition[i] = new Vector3(
										childrenNewPosition[i].x - childrenNewPosition[children.Count - 1].x / 2,
										childrenNewPosition[i].y,
										childrenNewPosition[i].z);
								}
							break;
						}
						case AlignmentHorizontal.CenterBot:
						{
							for (int i = 0; i < children.Count; i++)
							{
								childrenPrePosition[i] = children[i].anchoredPosition;
								var pos = i * new Vector3(columnDistance, 0, 0);
								pos.y = y + m_height - rowDistance;
								childrenNewPosition[i] = pos;
							}
							if (a.Key == m_lastGroupIndex)
								for (int i = 0; i < children.Count; i++)
								{
									var pos = childrenNewPosition[i];
									pos.x -= columnDistance * (children.Count - 1) / 2f;
									childrenNewPosition[i] = pos;
								}
							else
								for (int i = 0; i < childrenNewPosition.Length; i++)
								{
									childrenNewPosition[i] = new Vector3(
										childrenNewPosition[i].x - childrenNewPosition[children.Count - 1].x / 2,
										childrenNewPosition[i].y,
										childrenNewPosition[i].z);
								}
							break;
						}
					}

					initialPositions.Add(a.Key, childrenPrePosition);
					finalPositions.Add(a.Key, childrenNewPosition);
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
					switch (verticalLayout)
					{
						case AlignmentVertical.Top:
							for (int i = 0; i < children.Count; i++)
							{
								childrenPrePosition[i] = children[i].anchoredPosition;
								var pos = i * new Vector3(0, rowDistance, 0);
								pos.x = x - m_width / 2f;
								childrenNewPosition[i] = pos;
							}
							break;

						case AlignmentVertical.Bottom:
							for (int i = 0; i < children.Count; i++)
							{
								childrenPrePosition[i] = children[i].anchoredPosition;
								var pos = new Vector3(0, rowDistance, 0) * ((childrenNewPosition.Length - 1 - i) * -1);
								pos.x = x - m_width / 2f;
								childrenNewPosition[i] = pos;
							}
							break;

						case AlignmentVertical.Center:
							for (int i = 0; i < children.Count; i++)
							{
								childrenPrePosition[i] = children[i].anchoredPosition;
								var pos = i * new Vector3(0, rowDistance, 0);
								pos.x = x - m_width / 2f;
								childrenNewPosition[i] = pos;
							}
							if (a.Key == m_lastGroupIndex && m_lastGroupIndex != m_firstGroupIndex)
							{
								for (int i = 0; i < children.Count; i++)
								{
									var pos = childrenNewPosition[i];
									pos.y -= rowDistance * (maxRow - 1) / 2f;
									childrenNewPosition[i] = pos;
								}
							}
							else
								for (int i = 0; i < childrenNewPosition.Length; i++)
								{
									childrenNewPosition[i] = new Vector3(
										childrenNewPosition[i].x,
										childrenNewPosition[i].y - childrenNewPosition[childrenNewPosition.Length - 1].y / 2,
										childrenNewPosition[i].z);
								}
							break;
					}

					initialPositions.Add(a.Key, childrenPrePosition);
					finalPositions.Add(a.Key, childrenNewPosition);
				}
			}

#if DOTWEEN
			float lerp = 0;
			DOTween.Kill(GetInstanceID());
			DOTween.To(val => lerp = val, 0f, 1f, tweenTime)
				.OnUpdate(() =>
				{
					float t = lerp;
					if (animCurve.length > 1)
						t = animCurve.Evaluate(lerp);
					foreach (var a in m_childrenGroup)
					{
						var children = a.Value;
						for (int j = 0; j < children.Count; j++)
						{
							var pos = Vector2.Lerp(initialPositions[a.Key][j], finalPositions[a.Key][j], t);
							children[j].anchoredPosition = pos;

							var topRight = children[j].TopRight();
							if (topRight.x > m_childTopRight.x)
								m_childTopRight.x = topRight.x;
							if (topRight.y > m_childTopRight.y)
								m_childTopRight.y = topRight.y;

							var botLeft = children[j].BotLeft();
							if (botLeft.x < m_childBotLeft.x)
								m_childBotLeft.x = botLeft.x;
							if (botLeft.y < m_childBotLeft.y)
								m_childBotLeft.y = botLeft.y;

							if (m_AutoResizeContentX || m_AutoResizeContentY)
								AutoResizeContent();
						}
					}
				})
				.OnComplete(() =>
				{
					onFinish?.Invoke();
				});

#else
			StartCoroutine(IEArrangeChildren(m_childrenGroup, initialPositions, finalPositions, tweenTime, onFinish));
#endif
		}

		private IEnumerator IEArrangeChildren(Dictionary<int, List<RectTransform>> childrenGroup, Dictionary<int, Vector3[]> initialPositions, Dictionary<int, Vector3[]> finalPositions,
			float pDuration, Action pOnCompleted)
		{
			float time = 0;
			while (true)
			{
				if (time >= pDuration)
					time = pDuration;
				float lerp = time / pDuration;
				float t = lerp;

				if (animCurve.length > 1)
					t = animCurve.Evaluate(lerp);

				foreach (var a in childrenGroup)
				{
					var children = a.Value;
					for (int j = 0; j < children.Count; j++)
					{
						var pos = Vector2.Lerp(initialPositions[a.Key][j], finalPositions[a.Key][j], t);
						children[j].anchoredPosition = pos;

						var topRight = children[j].TopRight();
						if (topRight.x > m_childTopRight.x)
							m_childTopRight.x = topRight.x;
						if (topRight.y > m_childTopRight.y)
							m_childTopRight.y = topRight.y;

						var botLeft = children[j].BotLeft();
						if (botLeft.x < m_childBotLeft.x)
							m_childBotLeft.x = botLeft.x;
						if (botLeft.y < m_childBotLeft.y)
							m_childBotLeft.y = botLeft.y;

						if (m_AutoResizeContentX || m_AutoResizeContentY)
							AutoResizeContent();
					}
				}

				if (lerp >= 1)
					break;
				yield return null;
				time += Time.deltaTime;
			}

			pOnCompleted?.Invoke();
		}
	}
}