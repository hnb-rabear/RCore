
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif

namespace RevCore.UI
{
	/// <summary>
	/// Arranges children in a grid (rows x columns), filling either row-major or column-major based
	/// on <see cref="tableLayoutType"/>. Supports per-axis alignment, animated reflow, and optional
	/// auto-resize of the container to fit the laid-out cells.
	/// </summary>
	public class TableAlignmentUI : MonoBehaviour, IAligned
	{
		/// <summary>Fill direction for the grid.</summary>
		public enum TableLayoutType
		{
			/// <summary>Fill rows first, then wrap to next column.</summary>
			Horizontal,
			/// <summary>Fill columns first, then wrap to next row.</summary>
			Vertical
		}

		/// <summary>Vertical alignment used when <see cref="tableLayoutType"/> is Vertical.</summary>
		public enum AlignmentVertical
		{
			/// <summary>Anchor to top.</summary>
			Top,
			/// <summary>Anchor to bottom.</summary>
			Bottom,
			/// <summary>Center vertically.</summary>
			Center,
		}

		/// <summary>Horizontal alignment used when <see cref="tableLayoutType"/> is Horizontal.</summary>
		public enum AlignmentHorizontal
		{
			/// <summary>Anchor to left.</summary>
			Left,
			/// <summary>Anchor to right.</summary>
			Right,
			/// <summary>Center horizontally with default row offset.</summary>
			Center,
			/// <summary>Center horizontally, last row anchored to top.</summary>
			CenterTop,
			/// <summary>Center horizontally, last row anchored to bottom.</summary>
			CenterBot,
		}

		/// <summary>Fill direction.</summary>
		public TableLayoutType tableLayoutType;
		/// <summary>Tween duration when calling <see cref="AlignByTweener"/>.</summary>
		public float tweenTime = 0.25f;
		/// <summary>Animation easing curve.</summary>
		public AnimationCurve animCurve;
		/// <summary>Horizontal spacing between columns.</summary>
		public float columnDistance;
		/// <summary>Vertical spacing between rows.</summary>
		public float rowDistance;

		/// <summary>Vertical alignment (Vertical layout mode).</summary>
		[Header("Vertical Layout")]
		public AlignmentVertical verticalLayout;
		/// <summary>Max rows per column before wrapping (Vertical layout mode).</summary>
		public int maxRow;

		/// <summary>Horizontal alignment (Horizontal layout mode).</summary>
		[Header("Horizontal Layout")]
		public AlignmentHorizontal horizontalLayout;
		/// <summary>Max columns per row before wrapping (Horizontal layout mode).</summary>
		public int maxColumn;
		/// <summary>When true, row index 0 is at the top and indices grow downward.</summary>
		public bool reverseY = true;

		[Header("Optional Config")]
		[SerializeField] private bool m_AutoResizeContentX;
		[SerializeField] private bool m_AutoResizeContentY;
		[SerializeField] private Vector2 m_ContentSizeBonus;
		[SerializeField] private List<Transform> m_IgnoredObjects;
		[SerializeField, Range(0, 1f)] private float m_lerp;

		private float m_width;
		private float m_height;
		private int m_firstGroupIndex;
		private int m_lastGroupIndex;
		private Coroutine m_coroutine;

		private Dictionary<int, List<RectTransform>> m_childrenGroup = new();
		private Dictionary<int, Vector3[]> m_initialPositions = new();
		private Dictionary<int, Vector3[]> m_finalPositions = new();

		private void OnValidate()
		{
			if (Application.isPlaying)
				return;

			Init();
			RefreshPositions();

			float t = m_lerp;
			if (animCurve.length > 1)
				t = animCurve.Evaluate(m_lerp);

			foreach (var a in m_childrenGroup)
			{
				var children = a.Value;
				for (int j = 0; j < children.Count; j++)
				{
					var pos = Vector2.LerpUnclamped(Vector3.zero, m_finalPositions[a.Key][j], t);
					children[j].anchoredPosition = pos;
				}
			}

			if (m_AutoResizeContentX || m_AutoResizeContentY)
				AutoResizeContent();
		}

		/// <summary>(Re)builds the internal child group cache. Call after changing the active children at runtime.</summary>
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

		private void RefreshPositions()
		{
			m_initialPositions = new Dictionary<int, Vector3[]>();
			m_finalPositions = new Dictionary<int, Vector3[]>();

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

					m_initialPositions.Add(a.Key, childrenPrePosition);
					m_finalPositions.Add(a.Key, childrenNewPosition);
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

					m_initialPositions.Add(a.Key, childrenPrePosition);
					m_finalPositions.Add(a.Key, childrenNewPosition);
				}
			}
		}

		/// <inheritdoc />
		public void Align()
		{
			Init();
			RefreshPositions();

			foreach (var a in m_childrenGroup)
			{
				var children = a.Value;
				for (int j = 0; j < children.Count; j++)
					children[j].anchoredPosition = m_finalPositions[a.Key][j];
			}

			if (m_AutoResizeContentX || m_AutoResizeContentY)
				AutoResizeContent();
		}

		/// <summary>Resizes the host rect to fit the laid-out cells, honoring <c>m_AutoResizeContentX/Y</c> and <c>m_ContentSizeBonus</c>.</summary>
		public void AutoResizeContent()
		{
			Vector2 childTopRight = default;
			Vector2 childBotLeft = default;
			foreach (var group in m_childrenGroup)
			{
				var children = group.Value;
				for (int i = 0; i < children.Count; i++)
				{
					var topRight = children[i].TopRight();
					if (topRight.x > childTopRight.x)
						childTopRight.x = topRight.x;
					if (topRight.y > childTopRight.y)
						childTopRight.y = topRight.y;

					var botLeft = children[i].BotLeft();
					if (botLeft.x < childBotLeft.x)
						childBotLeft.x = botLeft.x;
					if (botLeft.y < childBotLeft.y)
						childBotLeft.y = botLeft.y;
				}
			}

			float height = childTopRight.y - childBotLeft.y + m_ContentSizeBonus.y;
			float width = childTopRight.x - childBotLeft.x + m_ContentSizeBonus.x;

			var size = ((RectTransform)transform).sizeDelta;
			if (m_AutoResizeContentX)
				size.x = width;
			if (m_AutoResizeContentY)
				size.y = height;
			((RectTransform)transform).sizeDelta = size;
		}

		/// <inheritdoc />
		public void AlignByTweener(Action onFinish)
		{
			Init();
			RefreshPositions();

#if DOTWEEN
			m_lerp = 0;
			DOTween.Kill(GetInstanceID());
			DOTween.To(val => m_lerp = val, 0f, 1f, tweenTime)
				.OnStart(() =>
				{
					foreach (var a in m_childrenGroup)
					{
						var children = a.Value;
						for (int j = 0; j < children.Count; j++)
							if (children[j].TryGetComponent(out ITweenItem item))
								item.OnStart();
					}
				})
				.OnUpdate(() =>
				{
					float t = m_lerp;
					if (animCurve.length > 1)
						t = animCurve.Evaluate(m_lerp);
					foreach (var a in m_childrenGroup)
					{
						var children = a.Value;
						for (int j = 0; j < children.Count; j++)
						{
							var pos = Vector2.LerpUnclamped(m_initialPositions[a.Key][j], m_finalPositions[a.Key][j], t);
							children[j].anchoredPosition = pos;
						}
					}

					if (m_AutoResizeContentX || m_AutoResizeContentY)
						AutoResizeContent();
				})
				.OnComplete(() =>
				{
					foreach (var a in m_childrenGroup)
					{
						var children = a.Value;
						for (int j = 0; j < children.Count; j++)
							if (children[j].TryGetComponent(out ITweenItem item))
								item.OnFinish();
					}

					if (m_AutoResizeContentX || m_AutoResizeContentY)
						AutoResizeContent();

					onFinish?.Invoke();
				});

#else
			StartCoroutine(IEArrangeChildren(m_childrenGroup, m_initialPositions, m_finalPositions, tweenTime, onFinish));
#endif
		}

		private IEnumerator IEArrangeChildren(Dictionary<int, List<RectTransform>> childrenGroup, Dictionary<int, Vector3[]> initialPositions, Dictionary<int, Vector3[]> finalPositions,
			float pDuration, Action pOnCompleted)
		{
			foreach (var a in m_childrenGroup)
			{
				var children = a.Value;
				for (int j = 0; j < children.Count; j++)
					if (children[j].TryGetComponent(out ITweenItem item))
						item.OnStart();
			}

			float time = 0;
			while (true)
			{
				if (time >= pDuration)
					time = pDuration;
				m_lerp = time / pDuration;
				float t = m_lerp;

				if (animCurve.length > 1)
					t = animCurve.Evaluate(m_lerp);

				foreach (var a in childrenGroup)
				{
					var children = a.Value;
					for (int j = 0; j < children.Count; j++)
					{
						var pos = Vector2.LerpUnclamped(initialPositions[a.Key][j], finalPositions[a.Key][j], t);
						children[j].anchoredPosition = pos;
					}
				}

				if (m_AutoResizeContentX || m_AutoResizeContentY)
					AutoResizeContent();

				if (m_lerp >= 1)
					break;
				yield return null;
				time += Time.deltaTime;
			}

			foreach (var a in m_childrenGroup)
			{
				var children = a.Value;
				for (int j = 0; j < children.Count; j++)
					if (children[j].TryGetComponent(out ITweenItem item))
						item.OnFinish();
			}

			pOnCompleted?.Invoke();
		}
	}
}
