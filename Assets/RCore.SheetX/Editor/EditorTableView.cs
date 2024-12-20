/***
 * This is modification of https://github.com/redclock/SimpleEditorTableView
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	public class EditorTableView<TData>
	{
		private MultiColumnHeaderState m_multiColumnHeaderState;
		private MultiColumnHeader m_multiColumnHeader;
		private MultiColumnHeaderState.Column[] m_columns;
		private readonly Color m_lighterColor = Color.white * 0.3f;
		private readonly Color m_darkerColor = Color.white * 0.1f;

		private Vector2 m_scrollPosition;
		private bool m_columnResized;
		private bool m_sortingDirty;
		private EditorWindow m_editorWindow;
		private string m_header;

		public float viewWidth;
		public float viewHeight;
		public float viewWidthFillRatio;
		public float viewHeightFillRatio;

		public delegate void DrawItem(Rect rect, TData item);

		public class ColumnDef
		{
			internal MultiColumnHeaderState.Column column;
			internal DrawItem onDraw;
			internal Comparison<TData> onSort;

			public ColumnDef SetMaxWidth(float maxWidth)
			{
				column.maxWidth = maxWidth;
				return this;
			}

			public ColumnDef SetTooltip(string tooltip)
			{
				column.headerContent.tooltip = tooltip;
				return this;
			}

			public ColumnDef SetAutoResize(bool autoResize)
			{
				column.autoResize = autoResize;
				return this;
			}

			public ColumnDef SetAllowToggleVisibility(bool allow)
			{
				column.allowToggleVisibility = allow;
				return this;
			}

			public ColumnDef SetSorting(Comparison<TData> onSort)
			{
				this.onSort = onSort;
				column.canSort = true;
				return this;
			}
		}

		private readonly List<ColumnDef> m_columnDefs = new List<ColumnDef>();

		public EditorTableView(EditorWindow pWindow, string header = null)
		{
			m_editorWindow = pWindow;
			m_header = header;
		}

		public ColumnDef AddColumn(string title, int minWidth, int maxWidth, DrawItem onDrawItem)
		{
			var columnDef = new ColumnDef()
			{
				column = new MultiColumnHeaderState.Column()
				{
					allowToggleVisibility = false,
					autoResize = true,
					minWidth = minWidth,
					maxWidth = maxWidth > 0 ? maxWidth : 1000f,
					width = minWidth, // Initialize width with minWidth
					canSort = false,
					sortingArrowAlignment = TextAlignment.Right,
					headerContent = new GUIContent(title),
					headerTextAlignment = TextAlignment.Left,
				},
				onDraw = onDrawItem
			};

			m_columnDefs.Add(columnDef);
			m_columnResized = true;
			return columnDef;
		}

		private void ReBuild()
		{
			m_columns = m_columnDefs.Select(def => def.column).ToArray();
			m_multiColumnHeaderState = new MultiColumnHeaderState(m_columns);
			m_multiColumnHeader = new MultiColumnHeader(m_multiColumnHeaderState);
			m_multiColumnHeader.visibleColumnsChanged += multiColumnHeader => multiColumnHeader.ResizeToFit();
			m_multiColumnHeader.sortingChanged += multiColumnHeader => m_sortingDirty = true;
			AdjustColumnWidths();
			m_multiColumnHeader.ResizeToFit();
			m_columnResized = false;
		}

		private void AdjustColumnWidths()
		{
			// Calculate total minimum width required
			float totalMinWidth = m_columns.Sum(c => c.minWidth);
			float availableWidth = m_editorWindow.position.width;

			// Ensure total width doesn't exceed window width
			if (totalMinWidth > availableWidth)
			{
				// If total minimum width exceeds available width, set widths to minimum values
				foreach (var column in m_columns)
				{
					column.width = column.minWidth;
				}
			}
			else
			{
				// Distribute the available width proportionally
				float extraWidth = availableWidth - totalMinWidth;
				foreach (var column in m_columns)
				{
					if (column.maxWidth == 1000f)
					{
						column.width = column.minWidth + (extraWidth / m_columns.Count(c => c.maxWidth == 1000f));
					}
					else
					{
						column.width = column.minWidth;
					}
				}
			}
		}

		private GUIStyle m_headerStyle;
		public void DrawOnGUI(List<TData> data, float maxHeight = float.MaxValue, float rowHeight = -1)
		{
			if (m_multiColumnHeader == null || m_columnResized)
				ReBuild();

			var style = new GUIStyle("box");

			float _viewWidth = viewWidth;
			float _viewHeight = viewHeight;

			if (viewWidthFillRatio > 0 && viewWidthFillRatio < 1 && m_editorWindow.position.width > 0)
				_viewWidth = viewWidthFillRatio * m_editorWindow.position.width - 7;
			if (viewHeightFillRatio > 0 && viewHeightFillRatio < 1 && m_editorWindow.position.height > 0)
				_viewHeight = viewHeightFillRatio * m_editorWindow.position.height;

			if (_viewWidth > 0) style.fixedWidth = _viewWidth;
			if (_viewHeight > 0) style.fixedHeight = _viewHeight;
			if (_viewWidth > 0 || _viewHeight > 0)
				EditorGUILayout.BeginVertical(style);

			if (!string.IsNullOrEmpty(m_header))
			{
				m_headerStyle ??= new GUIStyle(EditorStyles.boldLabel)
				{
					alignment = TextAnchor.MiddleCenter,
					fontSize = 16,
					fontStyle = FontStyle.Bold,
					padding = new RectOffset(10, 10, 5, 5),
					normal = new GUIStyleState
					{
						textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black,
						background = CreateTexture(1, 1, EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.9f, 0.9f, 0.9f))
					}
				};
				GUILayout.Label(m_header, m_headerStyle);
			}

			Texture2D CreateTexture(int width, int height, Color color)
			{
				var pixels = new Color[width * height];
				for (int i = 0; i < pixels.Length; i++)
				{
					pixels[i] = color;
				}
				var texture = new Texture2D(width, height);
				texture.SetPixels(pixels);
				texture.Apply();
				return texture;
			}

			float rowWidth = m_multiColumnHeaderState.widthOfAllVisibleColumns;
			if (rowHeight < 0)
				rowHeight = EditorGUIUtility.singleLineHeight;

			var headerRect = GUILayoutUtility.GetRect(rowWidth, rowHeight);
			m_multiColumnHeader!.OnGUI(headerRect, xScroll: 0.0f);

			float sumWidth = rowWidth;
			float sumHeight = rowHeight * (data?.Count ?? 1) + GUI.skin.horizontalScrollbar.fixedHeight;

			UpdateSorting(data);

			var scrollViewPos = GUILayoutUtility.GetRect(0, sumWidth, 0, maxHeight);
			var viewRect = new Rect(0, 0, sumWidth, sumHeight);

			m_scrollPosition = GUI.BeginScrollView(
				position: scrollViewPos,
				scrollPosition: m_scrollPosition,
				viewRect: viewRect,
				alwaysShowHorizontal: false,
				alwaysShowVertical: false
			);

			if (data != null)
				for (int row = 0; row < data.Count; row++)
				{
					var rowRect = new Rect(0, rowHeight * row, rowWidth, rowHeight);

					EditorGUI.DrawRect(rect: rowRect, color: row % 2 == 0 ? m_darkerColor : m_lighterColor);

					for (int col = 0; col < m_columns.Length; col++)
					{
						if (m_multiColumnHeader.IsColumnVisible(col))
						{
							int visibleColumnIndex = m_multiColumnHeader.GetVisibleColumnIndex(col);
							var cellRect = m_multiColumnHeader.GetCellRect(visibleColumnIndex, rowRect);
							m_columnDefs[col].onDraw(cellRect, data[row]);
						}
					}
				}

			GUI.EndScrollView(handleScrollWheel: true);

			if (_viewWidth > 0 || _viewHeight > 0)
				EditorGUILayout.EndVertical();
		}

		private void UpdateSorting(List<TData> data)
		{
			if (m_sortingDirty && data != null)
			{
				int sortIndex = m_multiColumnHeader.sortedColumnIndex;
				if (sortIndex >= 0)
				{
					var sortCompare = m_columnDefs[sortIndex].onSort;
					bool ascending = m_multiColumnHeader.IsSortedAscending(sortIndex);

					data.Sort((a, b) =>
					{
						int result = sortCompare(a, b);
						return ascending ? result : -result;
					});
				}

				m_sortingDirty = false;
			}
		}
	}
}