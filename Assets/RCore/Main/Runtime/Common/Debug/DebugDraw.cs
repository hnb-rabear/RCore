/**
* Author RadBear - nbhung71711 @gmail.com - 2018
**/

using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System;

namespace RCore
{
	public static class DebugDraw
	{
		static DebugDraw()
		{
			m_Enabled = PlayerPrefs.GetInt("EnabledDebugDraw", 0) == 1;
		}

		public static Action OnEnabled;
		private static bool m_Enabled;
		public static bool Enabled
		{
			get => m_Enabled;
			set
			{
				if (m_Enabled == value)
					return;
				m_Enabled = value;
				PlayerPrefs.SetInt("EnabledDebugDraw", value ? 1 : 0);
				OnEnabled?.Invoke();
			}
		}

		//================= BASIC =================

		[Conditional("UNITY_EDITOR")]
		public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.5f)
		{
			if (!Enabled) return;
			UnityEngine.Debug.DrawLine(start, end, color, duration);
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawLines(Vector3[] points, Color color, float duration = 0.5f)
		{
			if (!Enabled || points.Length < 2) return;
			for (int i = 0; i < points.Length; i++)
			{
				UnityEngine.Debug.DrawLine(points[i], i < points.Length - 1 ? points[i + 1] : points[0], color, duration);
			}
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawLinesGizmos(Transform[] points, Color color)
		{
			if (!Enabled || points.Length < 2) return;
			var colorTemp = Gizmos.color;
			Gizmos.color = color;
			for (int i = 0; i < points.Length; i++)
			{
				Gizmos.DrawLine(points[i].position, i < points.Length - 1 ? points[i + 1].position : points[0].position);
			}
			Gizmos.color = colorTemp;
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawLinesGizmos(Vector3[] points, Color color, bool close = true)
		{
			if (!Enabled || points.Length < 2) return;
			var colorTemp = Gizmos.color;
			Gizmos.color = color;
			Gizmos.color = color;
			for (int i = 0; i < points.Length; i++)
			{
				if (close)
					Gizmos.DrawLine(points[i], i < points.Length - 1 ? points[i + 1] : points[0]);
				else
				{
					if (i < points.Length - 1)
						Gizmos.DrawLine(points[i], points[i + 1]);
				}
			}
			Gizmos.color = colorTemp;
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawLinesGizmos(List<Vector3> points, Color color)
		{
			if (!Enabled || points.Count < 2) return;
			var colorTemp = Gizmos.color;
			Gizmos.color = color;
			Gizmos.color = color;
			for (int i = 0; i < points.Count; i++)
			{
				Gizmos.DrawLine(points[i], i < points.Count - 1 ? points[i + 1] : points[0]);
			}
			Gizmos.color = colorTemp;
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawLinesGizmos(Color color, params Vector3[] points)
		{
			if (!Enabled || points.Length < 2) return;
			var colorTemp = Gizmos.color;
			Gizmos.color = color;
			Gizmos.color = color;
			for (int i = 0; i < points.Length; i++)
			{
				Gizmos.DrawLine(points[i], i < points.Length - 1 ? points[i + 1] : points[0]);
			}
			Gizmos.color = colorTemp;
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawLinesGizmos(Color color, params Vector2[] points)
		{
			if (!Enabled || points.Length < 2) return;
			var colorTemp = Gizmos.color;
			Gizmos.color = color;
			Gizmos.color = color;
			for (int i = 0; i < points.Length; i++)
			{
				Gizmos.DrawLine(points[i], i < points.Length - 1 ? points[i + 1] : points[0]);
			}
			Gizmos.color = colorTemp;
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawLine(Vector3 start, Vector3 dir, float length, Color color, float duration = 0.1f)
		{
			if (!Enabled) return;
			var pos = start + dir.normalized * length;
			DrawLine(start, pos, color, duration);
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawRectangle(Rect pRect, Color color, float duration = 10f)
		{
			if (!Enabled) return;
			float xMin = pRect.xMin;
			float xMax = pRect.xMax;
			float yMin = pRect.yMin;
			float yMax = pRect.yMax;
			var topLeft = new Vector3(xMin, yMax);
			var topRight = new Vector3(xMax, yMax);
			var bottomLeft = new Vector3(xMin, yMin);
			var bottomRight = new Vector3(xMax, yMin);
			DrawLine(topLeft, topRight, color, duration);
			DrawLine(topRight, bottomRight, color, duration);
			DrawLine(bottomRight, bottomLeft, color, duration);
			DrawLine(bottomLeft, topLeft, color, duration);
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawRectangleGizmos(Rect pRect, Color color = default)
		{
			if (!Enabled) return;
			float xMin = pRect.xMin;
			float xMax = pRect.xMax;
			float yMin = pRect.yMin;
			float yMax = pRect.yMax;
			var topLeft = new Vector3(xMin, yMax);
			var topRight = new Vector3(xMax, yMax);
			var bottomLeft = new Vector3(xMin, yMin);
			var bottomRight = new Vector3(xMax, yMin);
			if (color != default)
				Gizmos.color = color;
			Gizmos.DrawLine(topLeft, topRight);
			Gizmos.DrawLine(topRight, bottomRight);
			Gizmos.DrawLine(bottomRight, bottomLeft);
			Gizmos.DrawLine(bottomLeft, topLeft);
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawRectangle(Vector2 pCenter, Vector2 pSize, Color color, float duration = 1f)
		{
			if (!Enabled) return;
			float xMin = pCenter.x - pSize.x / 2;
			float xMax = pCenter.x + pSize.x / 2;
			float yMin = pCenter.y - pSize.y / 2;
			float yMax = pCenter.y + pSize.y / 2;
			var topLeft = new Vector3(xMin, yMax);
			var topRight = new Vector3(xMax, yMax);
			var bottomLeft = new Vector3(xMin, yMin);
			var bottomRight = new Vector3(xMax, yMin);
			DrawLine(topLeft, topRight, color, duration);
			DrawLine(topRight, bottomRight, color, duration);
			DrawLine(bottomRight, bottomLeft, color, duration);
			DrawLine(bottomLeft, topLeft, color, duration);
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawRectangleGizmos(Vector2 pCenter, Vector2 pSize, Color color = default)
		{
			if (!Enabled) return;
			float xMin = pCenter.x - pSize.x / 2;
			float xMax = pCenter.x + pSize.x / 2;
			float yMin = pCenter.y - pSize.y / 2;
			float yMax = pCenter.y + pSize.y / 2;
			var topLeft = new Vector3(xMin, yMax);
			var topRight = new Vector3(xMax, yMax);
			var bottomLeft = new Vector3(xMin, yMin);
			var bottomRight = new Vector3(xMax, yMin);
			if (color != default)
				Gizmos.color = color;
			Gizmos.DrawLine(topLeft, topRight);
			Gizmos.DrawLine(topRight, bottomRight);
			Gizmos.DrawLine(bottomRight, bottomLeft);
			Gizmos.DrawLine(bottomLeft, topLeft);
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawColoredRectangle(Bounds bounds, Color color, float duration)
		{
			if (!Enabled) return;
			float width = bounds.size.x;
			float height = bounds.size.y;

			DrawColoredRectangle(bounds.center, width, height, color, duration);
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawColoredRectangle(Rect rect, Color color, float duration)
		{
			float width = rect.size.x;
			float height = rect.size.y;

			DrawColoredRectangle(rect.center, width, height, color, duration);
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawColoredRectangle(Vector3 center, float width, float height, Color color, float time = 0.5f)
		{
			if (!Enabled) return;
			var a = new Vector2(center.x - width / 2, center.y + height / 2);
			var b = new Vector2(center.x - width / 2, center.y - height / 2);
			var c = new Vector2(center.x + width / 2, center.y - height / 2);
			var d = new Vector2(center.x + width / 2, center.y + height / 2);
			UnityEngine.Debug.DrawLine(a, b, color, time);
			UnityEngine.Debug.DrawLine(b, c, color, time);
			UnityEngine.Debug.DrawLine(c, d, color, time);
			UnityEngine.Debug.DrawLine(d, a, color, time);
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawColoredRectangle(BoxCollider2D boxCollider2d, Color color, float duration)
		{
			if (!Enabled) return;
			var bounds = boxCollider2d.bounds;
			float width = bounds.size.x;
			float height = bounds.size.y;

			DrawColoredRectangle(bounds.center, width, height, color, duration);
		}

		//================= ADVANCE =================

		[Conditional("UNITY_EDITOR")]
		public static void DrawEllipse(Vector3 center, Vector2 size, Color color, float duration = 0.5f)
		{
			if (!Enabled) return;
			DrawEllipse(center, size.x, size.y, color, duration);
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawEllipse(Vector3 center, float width, float height, Color color, float duration = 0.5f)
		{
			if (!Enabled) return;
			const int steps = 100;
			float interval = 2 * width / steps;
			var currentPoint = Vector3.zero;
			for (int i = 0; i <= steps; i++)
			{
				var previousPoint = currentPoint;
				float x = -width + interval * i;
				float y = Mathf.Sqrt(1 - x * x / (width * width)) * height;
				currentPoint = new Vector2(x, y);
				if (i > 0)
					DrawLine(center + previousPoint, center + currentPoint, color, duration);
				if (i > 0)
					DrawLine(center + new Vector3(previousPoint.x, -previousPoint.y), center + new Vector3(currentPoint.x, -currentPoint.y), color, duration);
			}
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawEllipseGizmos(Vector3 center, float width, float height, Color color, float duration = 0.5f)
		{
			if (!Enabled) return;
			const int steps = 100;
			float interval = 2 * width / steps;
			var currentPoint = Vector3.zero;
			for (int i = 0; i <= steps; i++)
			{
				var previousPoint = currentPoint;
				float x = -width + interval * i;
				float y = Mathf.Sqrt(1 - x * x / (width * width)) * height;
				currentPoint = new Vector2(x, y);
				if (i > 0)
					DrawLinesGizmos(new[] { center + previousPoint, center + currentPoint }, color);
				if (i > 0)
					DrawLinesGizmos(new[] { center + new Vector3(previousPoint.x, -previousPoint.y), center + new Vector3(currentPoint.x, -currentPoint.y) }, color);
			}
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawGridLinesGizmos(Vector3 rootPos, int width, int length, float pTileSize)
		{
			for (int i = 0; i < width; i++)
			{
				//Draw vertical line
				var from = rootPos;
				from.x += i * pTileSize;
				var to = rootPos;
				to.z += length * pTileSize;
				to.x += i * pTileSize;
				Gizmos.DrawLine(from, to);
			}
			for (int i = 0; i < length; i++)
			{
				//Draw horizontal line
				var from = rootPos;
				from.z += i * pTileSize;
				var to = rootPos;
				to.x += width * pTileSize;
				to.z += i * pTileSize;
				Gizmos.DrawLine(from, to);
			}
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawGridLinesGizmos(Vector2 rootPos, float width, float length, Vector2 cellSize, float rootCellSize = 0, float centerCellSize = 0)
		{
			var from = new Vector2();
			var to = new Vector2();
			for (int i = 0; i <= width; i++)
			{
				//Draw vertical line
				from = rootPos;
				from.x += i * cellSize.x;
				to = rootPos;
				to.y += length * cellSize.y;
				to.x += i * cellSize.x;
				Gizmos.DrawLine(from, to);
			}
			for (int i = 0; i <= length; i++)
			{
				//Draw horizontal line
				from = rootPos;
				from.y += i * cellSize.y;
				to = rootPos;
				to.x += width * cellSize.x;
				to.y += i * cellSize.y;
				Gizmos.DrawLine(from, to);
			}

			if (rootCellSize > 0)
			{
				for (int i = 0; i <= width; i++)
				{
					float x = rootPos.x;
					x += i * cellSize.x;
					for (int j = 0; j <= length; j++)
					{
						float y = rootPos.y;
						y += j * cellSize.y;
						Gizmos.DrawCube(new Vector2(x, y), Vector2.one * rootCellSize);
					}
				}
			}

			if (centerCellSize > 0)
			{
				for (int i = 0; i < width; i++)
				{
					float x = rootPos.x;
					x += i * cellSize.x;
					x += cellSize.x / 2f;
					for (int j = 0; j < length; j++)
					{
						float y = rootPos.y;
						y += j * cellSize.y;
						y += cellSize.y / 2f;
						Gizmos.DrawCube(new Vector2(x, y), Vector2.one * rootCellSize);
					}
				}
			}
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawGridIsometricGizmos(Vector2 rootPos, float sizeX, float sizeY, Vector2 cellSize, bool pShowCoordinate = false, Color color = default, int cellOffset = 0)
		{
			if (color == default)
				color = Color.white;

			var offsetX = cellSize.x / 2f;
			var offsetY = cellSize.y / 2f;

			Gizmos.color = color;
			for (int y = 0; y <= sizeY; y++)
			{
				int yy = y + cellOffset;
				var pos1 = rootPos + new Vector2(cellOffset * offsetX - yy * offsetX, yy * offsetY + cellOffset * offsetY);
				var pos2 = rootPos + new Vector2((sizeX + cellOffset) * offsetX - yy * offsetX, yy * offsetY + (sizeX + cellOffset) * offsetY);
				Gizmos.DrawLine(pos1, pos2);
			}

			for (int x = 0; x <= sizeX; x++)
			{
				int xx = x + cellOffset;
				var pos1 = rootPos + new Vector2(xx * offsetX - cellOffset * offsetX, cellOffset * offsetY + xx * offsetY);
				var pos2 = rootPos + new Vector2(xx * offsetX - (sizeY + cellOffset) * offsetX, (sizeY + cellOffset) * offsetY + xx * offsetY);
				Gizmos.DrawLine(pos1, pos2);
			}

			for (int y = 0; y < sizeY; y++)
			{
				int yy = y + cellOffset;
				for (int x = 0; x < sizeX; x++)
				{
					int xx = x + cellOffset;
					var pos = new Vector2(xx * offsetX - yy * offsetX, yy * offsetY + xx * offsetY);
					pos.x += rootPos.x;
					pos.y += rootPos.y;
					DrawGizmosPoint(pos, color.Invert(), 0.1f);
					if (pShowCoordinate)
						DrawText($"({xx},{yy})", pos, color.SetAlpha(1));
				}
			}
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawCellIsometricGizmos(Vector2 rootPos, Color color = default, int cellOffset = 0)
		{
			if (color == default)
				color = Color.white;

			Gizmos.color = color;
			for (int y = 0; y <= 1; y++)
			{
				int yy = y + cellOffset;
				var pos1 = rootPos + new Vector2(cellOffset - yy, yy * 0.5f + cellOffset * 0.5f);
				var pos2 = rootPos + new Vector2((1 + cellOffset) - yy, yy * 0.5f + (1 + cellOffset) * 0.5f);
				Gizmos.DrawLine(pos1, pos2);
			}

			for (int x = 0; x <= 1; x++)
			{
				int xx = x + cellOffset;
				var pos1 = rootPos + new Vector2(xx - cellOffset, cellOffset * 0.5f + xx * 0.5f);
				var pos2 = rootPos + new Vector2(xx - (1 + cellOffset), (1 + cellOffset) * 0.5f + xx * 0.5f);
				Gizmos.DrawLine(pos1, pos2);
			}
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawIsometricAreaGizmos(Vector2 rootPos, int areaWidth, int areaLength, Color color = default, int cellOffset = 0)
		{
			DrawGridIsometricGizmos(rootPos, 1, 1, new Vector2(areaWidth * 2, areaLength), false, color, cellOffset);
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawGridLines(Vector3 rootPos, int width, int length, float pTileSize, Color pColor = default, float pDuration = 0.1f)
		{
			for (int i = 0; i < width; i++)
			{
				//Draw vertical line
				var from = rootPos;
				from.x += i * pTileSize;
				var to = rootPos;
				to.z += length * pTileSize;
				to.x += i * pTileSize;
				UnityEngine.Debug.DrawLine(from, to, pColor, pDuration);
			}
			for (int i = 0; i < length; i++)
			{
				//Draw horizontal line
				var from = rootPos;
				from.z += i * pTileSize;
				var to = rootPos;
				to.x += width * pTileSize;
				to.z += i * pTileSize;
				UnityEngine.Debug.DrawLine(from, to, pColor, pDuration);
			}
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawGridNodesGizmos(Vector3 rootPos, int width, int length, float tileSize, float pNodeSize)
		{
			var nodes = BuildGridNodes(rootPos, width, length, tileSize);
			for (int i = 0; i < nodes.Count; i++)
				Gizmos.DrawCube(nodes[i], Vector3.one * pNodeSize);
		}

		public static void DrawPathGizmos(List<Vector3> pPath, Color? colour = null, bool pLoop = false)
		{
			if (colour != null)
				Gizmos.color = colour.Value;
			if (pPath.Count > 1)
			{
				for (int i = 0; i < pPath.Count - 1; i++)
				{
					Gizmos.DrawCube(pPath[i], Vector3.one * 0.1f);
					Gizmos.DrawLine(pPath[i], pPath[i + 1]);
				}
				if (pLoop && pPath.Count > 2)
				{
					Gizmos.DrawCube(pPath[pPath.Count - 1], Vector3.one * 0.1f);
					Gizmos.DrawLine(pPath[0], pPath[pPath.Count - 1]);
				}
			}
		}

		public static void DrawPathGizmos(Vector3[] pPath, Color? colour = null, bool pLoop = false)
		{
			if (colour != null)
				Gizmos.color = colour.Value;
			if (pPath.Length > 1)
			{
				for (int i = 0; i < pPath.Length - 1; i++)
				{
					Gizmos.DrawCube(pPath[i], Vector3.one * 0.1f);
					Gizmos.DrawLine(pPath[i], pPath[i + 1]);
				}
				if (pLoop && pPath.Length > 2)
				{
					Gizmos.DrawCube(pPath[pPath.Length - 1], Vector3.one * 0.1f);
					Gizmos.DrawLine(pPath[0], pPath[pPath.Length - 1]);
				}
			}
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawText(string text, Vector3 worldPos, Color? colour = null)
		{
#if UNITY_EDITOR
			if (Application.isPlaying)
				return;

			UnityEditor.Handles.BeginGUI();
			var restoreColor = GUI.color;
			if (colour.HasValue) GUI.color = colour.Value;
			var view = UnityEditor.SceneView.currentDrawingSceneView;
			var screenPos = view.camera.WorldToScreenPoint(worldPos);

			if (screenPos.y < 0 || screenPos.y > Screen.height || screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
			{
				GUI.color = restoreColor;
				UnityEditor.Handles.EndGUI();
				return;
			}

			var size = GUI.skin.label.CalcSize(new GUIContent(text));
			GUI.Label(new Rect(screenPos.x - size.x / 2, -screenPos.y + view.position.height + 4, size.x, size.y), text);
			GUI.color = restoreColor;
			UnityEditor.Handles.EndGUI();
#endif
		}

		[Conditional("UNITY_EDITOR")]
		public static void DrawFOV(FOVInfo pFovInfo)
		{
			if (!Enabled) return;

			if (pFovInfo.usingGizmos)
				Gizmos.color = pFovInfo.color;

			int steps = (int)pFovInfo.viewAngle / 5;
			float stepAngle = pFovInfo.viewAngle / steps;
			float rootAngle = pFovInfo.root.eulerAngles.y + pFovInfo.directionAngle;
			var rootPosition = pFovInfo.root.position;

			for (int i = 0; i <= steps; i++)
			{
				float angle = rootAngle - pFovInfo.viewAngle / 2 + stepAngle * i;
				var dir = MathHelper.DirOfYAngle(angle);
				var point = rootPosition + dir * pFovInfo.range;

				if (Physics.Raycast(rootPosition, dir, out var hit, pFovInfo.range, pFovInfo.obstacleLayer))
					point = hit.point;

				if (pFovInfo.usingGizmos)
					Gizmos.DrawLine(rootPosition, point);
				else
					UnityEngine.Debug.DrawLine(rootPosition, point, pFovInfo.color, pFovInfo.duration);
			}
		}
		
		//======================================================

		private static List<Vector3> BuildGridNodes(Vector3 rootPos, int width, int length, float tileSize)
		{
			var list = new List<Vector3>();
			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < length; j++)
				{
					var pos = rootPos;
					pos.x += tileSize * i + tileSize / 2f;
					pos.z += tileSize * j + tileSize / 2f;
					list.Add(pos);
				}
			}
			return list;
		}

		public class FOVInfo
		{
			//Main attributes
			public Transform root;
			public float viewAngle;
			public float range;
			public Color color;
			//Optional attributes
			public LayerMask obstacleLayer;
			public float directionAngle;
			public bool usingGizmos;
			public float duration;
		}

		//================= ARKHAM INTERACTIVE ===================

#region DebugDrawFunctions

		/// <summary>Debugs a point.</summary>
		/// <param name='position'>The point to debug.</param>
		/// <param name='color'>The color of the point.</param>
		/// <param name='scale'>The size of the point.</param>
		/// <param name='duration'>How long to draw the point.</param>
		/// <param name='depthTest'>Whether or not this point should be faded when behind other objects.</param>
		public static void DrawPoint(Vector3 position, Color color, float scale = 1.0f, float duration = 0, bool depthTest = true)
		{
			color = color == default ? Color.white : color;

			UnityEngine.Debug.DrawRay(position + Vector3.up * (scale * 0.5f), -Vector3.up * scale, color, duration, depthTest);
			UnityEngine.Debug.DrawRay(position + Vector3.right * (scale * 0.5f), -Vector3.right * scale, color, duration, depthTest);
			UnityEngine.Debug.DrawRay(position + Vector3.forward * (scale * 0.5f), -Vector3.forward * scale, color, duration, depthTest);
		}

		/// <summary>Debugs a point.</summary>
		/// <param name='position'>The point to debug.</param>
		/// <param name='scale'>The size of the point.</param>
		/// <param name='duration'>How long to draw the point.</param>
		/// <param name='depthTest'>Whether or not this point should be faded when behind other objects.</param>
		public static void DrawPoint(Vector3 position, float scale = 1.0f, float duration = 0, bool depthTest = true)
		{
			DrawPoint(position, Color.white, scale, duration, depthTest);
		}

		/// <summary>Debugs an axis-aligned bounding box.</summary>
		/// <param name='bounds'>The bounds to debug.</param>
		/// <param name='color'>The color of the bounds.</param>
		/// <param name='duration'>How long to draw the bounds.</param>
		/// <param name='depthTest'>Whether or not the bounds should be faded when behind other objects.</param>
		public static void DrawBounds(Bounds bounds, Color color, float duration = 0, bool depthTest = true)
		{
			var center = bounds.center;

			float x = bounds.extents.x;
			float y = bounds.extents.y;
			float z = bounds.extents.z;

			var ruf = center + new Vector3(x, y, z);
			var rub = center + new Vector3(x, y, -z);
			var luf = center + new Vector3(-x, y, z);
			var lub = center + new Vector3(-x, y, -z);

			var rdf = center + new Vector3(x, -y, z);
			var rdb = center + new Vector3(x, -y, -z);
			var lfd = center + new Vector3(-x, -y, z);
			var lbd = center + new Vector3(-x, -y, -z);

			UnityEngine.Debug.DrawLine(ruf, luf, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(ruf, rub, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(luf, lub, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(rub, lub, color, duration, depthTest);

			UnityEngine.Debug.DrawLine(ruf, rdf, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(rub, rdb, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(luf, lfd, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(lub, lbd, color, duration, depthTest);

			UnityEngine.Debug.DrawLine(rdf, lfd, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(rdf, rdb, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(lfd, lbd, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(lbd, rdb, color, duration, depthTest);
		}

		/// <summary>Debugs an axis-aligned bounding box.</summary>
		/// <param name='bounds'>The bounds to debug.</param>
		/// <param name='duration'>How long to draw the bounds.</param>
		/// <param name='depthTest'>Whether or not the bounds should be faded when behind other objects.</param>
		public static void DrawBounds(Bounds bounds, float duration = 0, bool depthTest = true)
		{
			DrawBounds(bounds, Color.white, duration, depthTest);
		}

		/// <summary>Debugs a local cube.</summary>
		/// <param name='transform'>The transform that the cube will be local to.</param>
		/// <param name='size'>The size of the cube.</param>
		/// <param name='color'>Color of the cube.</param>
		/// <param name='center'>The position (relative to transform) where the cube will be debugged.</param>
		/// <param name='duration'>How long to draw the cube.</param>
		/// <param name='depthTest'>Whether or not the cube should be faded when behind other objects.</param>
		public static void DrawLocalCube(Transform transform, Vector3 size, Color color, Vector3 center = default, float duration = 0, bool depthTest = true)
		{
			var lbb = transform.TransformPoint(center + -size * 0.5f);
			var rbb = transform.TransformPoint(center + new Vector3(size.x, -size.y, -size.z) * 0.5f);

			var lbf = transform.TransformPoint(center + new Vector3(size.x, -size.y, size.z) * 0.5f);
			var rbf = transform.TransformPoint(center + new Vector3(-size.x, -size.y, size.z) * 0.5f);

			var lub = transform.TransformPoint(center + new Vector3(-size.x, size.y, -size.z) * 0.5f);
			var rub = transform.TransformPoint(center + new Vector3(size.x, size.y, -size.z) * 0.5f);

			var luf = transform.TransformPoint(center + size * 0.5f);
			var ruf = transform.TransformPoint(center + new Vector3(-size.x, size.y, size.z) * 0.5f);

			UnityEngine.Debug.DrawLine(lbb, rbb, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(rbb, lbf, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(lbf, rbf, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(rbf, lbb, color, duration, depthTest);

			UnityEngine.Debug.DrawLine(lub, rub, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(rub, luf, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(luf, ruf, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(ruf, lub, color, duration, depthTest);

			UnityEngine.Debug.DrawLine(lbb, lub, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(rbb, rub, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(lbf, luf, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(rbf, ruf, color, duration, depthTest);
		}

		/// <summary>Debugs a local cube.</summary>
		/// <param name='transform'>The transform that the cube will be local to.</param>
		/// <param name='size'>The size of the cube.</param>
		/// <param name='center'>The position (relative to transform) where the cube will be debugged.</param>
		/// <param name='duration'>How long to draw the cube.</param>
		/// <param name='depthTest'>Whether or not the cube should be faded when behind other objects.</param>
		public static void DrawLocalCube(Transform transform, Vector3 size, Vector3 center = default, float duration = 0, bool depthTest = true)
		{
			DrawLocalCube(transform, size, Color.white, center, duration, depthTest);
		}

		/// <summary>Debugs a local cube.</summary>
		/// <param name='space'>The space the cube will be local to.</param>
		/// <param name='size'>The size of the cube.</param>
		/// <param name='color'>Color of the cube.</param>
		/// <param name='center'>The position (relative to transform) where the cube will be debugged.</param>
		/// <param name='duration'>How long to draw the cube.</param>
		/// <param name='depthTest'>Whether or not the cube should be faded when behind other objects.</param>
		public static void DrawLocalCube(Matrix4x4 space, Vector3 size, Color color, Vector3 center = default, float duration = 0, bool depthTest = true)
		{
			color = color == default ? Color.white : color;

			var lbb = space.MultiplyPoint3x4(center + -size * 0.5f);
			var rbb = space.MultiplyPoint3x4(center + new Vector3(size.x, -size.y, -size.z) * 0.5f);

			var lbf = space.MultiplyPoint3x4(center + new Vector3(size.x, -size.y, size.z) * 0.5f);
			var rbf = space.MultiplyPoint3x4(center + new Vector3(-size.x, -size.y, size.z) * 0.5f);

			var lub = space.MultiplyPoint3x4(center + new Vector3(-size.x, size.y, -size.z) * 0.5f);
			var rub = space.MultiplyPoint3x4(center + new Vector3(size.x, size.y, -size.z) * 0.5f);

			var luf = space.MultiplyPoint3x4(center + size * 0.5f);
			var ruf = space.MultiplyPoint3x4(center + new Vector3(-size.x, size.y, size.z) * 0.5f);

			UnityEngine.Debug.DrawLine(lbb, rbb, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(rbb, lbf, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(lbf, rbf, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(rbf, lbb, color, duration, depthTest);

			UnityEngine.Debug.DrawLine(lub, rub, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(rub, luf, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(luf, ruf, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(ruf, lub, color, duration, depthTest);

			UnityEngine.Debug.DrawLine(lbb, lub, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(rbb, rub, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(lbf, luf, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(rbf, ruf, color, duration, depthTest);
		}

		/// <summary>Debugs a local cube.</summary>
		/// <param name='space'>The space the cube will be local to.</param>
		/// <param name='size'>The size of the cube.</param>
		/// <param name='center'>The position (relative to transform) where the cube will be debugged.</param>
		/// <param name='duration'>How long to draw the cube.</param>
		/// <param name='depthTest'>Whether or not the cube should be faded when behind other objects.</param>
		public static void DrawLocalCube(Matrix4x4 space, Vector3 size, Vector3 center = default, float duration = 0, bool depthTest = true)
		{
			DrawLocalCube(space, size, Color.white, center, duration, depthTest);
		}

		/// <summary>Debugs a circle.</summary>
		/// <param name='position'>Where the center of the circle will be positioned.</param>
		/// <param name='up'>The direction perpendicular to the surface of the circle.</param>
		/// <param name='color'>The color of the circle.</param>
		/// <param name='radius'>The radius of the circle.</param>
		/// <param name='duration'>How long to draw the circle.</param>
		/// <param name='depthTest'>Whether or not the circle should be faded when behind other objects.</param>
		public static void DrawCircle(Vector3 position, Vector3 up, Color color, float radius = 1.0f, float duration = 0, bool depthTest = true)
		{
			var _up = up.normalized * radius;
			var _forward = Vector3.Slerp(_up, -_up, 0.5f);
			var _right = Vector3.Cross(_up, _forward).normalized * radius;

			var matrix = new Matrix4x4
			{
				[0] = _right.x,
				[1] = _right.y,
				[2] = _right.z,
				[4] = _up.x,
				[5] = _up.y,
				[6] = _up.z,
				[8] = _forward.x,
				[9] = _forward.y,
				[10] = _forward.z
			};

			var _lastPoint = position + matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)));
			var _nextPoint = Vector3.zero;

			color = color == default ? Color.white : color;

			for (var i = 0; i < 91; i++)
			{
				_nextPoint.x = Mathf.Cos(i * 4 * Mathf.Deg2Rad);
				_nextPoint.z = Mathf.Sin(i * 4 * Mathf.Deg2Rad);
				_nextPoint.y = 0;

				_nextPoint = position + matrix.MultiplyPoint3x4(_nextPoint);

				UnityEngine.Debug.DrawLine(_lastPoint, _nextPoint, color, duration, depthTest);
				_lastPoint = _nextPoint;
			}
		}

		/// <summary>Debugs a circle.</summary>
		/// <param name='position'>Where the center of the circle will be positioned.</param>
		/// <param name='color'>The color of the circle.</param>
		/// <param name='radius'>The radius of the circle.</param>
		/// <param name='duration'>How long to draw the circle.</param>
		/// <param name='depthTest'>Whether or not the circle should be faded when behind other objects.</param>
		public static void DrawCircle(Vector3 position, Color color, float radius = 1.0f, float duration = 0, bool depthTest = true)
		{
			DrawCircle(position, Vector3.up, color, radius, duration, depthTest);
		}

		/// <summary>Debugs a circle.</summary>
		/// <param name='position'>Where the center of the circle will be positioned.</param>
		/// <param name='up'>The direction perpendicular to the surface of the circle.</param>
		/// <param name='radius'>The radius of the circle.</param>
		/// <param name='duration'>How long to draw the circle.</param>
		/// <param name='depthTest'>Whether or not the circle should be faded when behind other objects.</param>
		public static void DrawCircle(Vector3 position, Vector3 up, float radius = 1.0f, float duration = 0, bool depthTest = true)
		{
			DrawCircle(position, up, Color.white, radius, duration, depthTest);
		}

		/// <summary>Debugs a circle.</summary>
		/// <param name='position'>Where the center of the circle will be positioned.</param>
		/// <param name='radius'>The radius of the circle.</param>
		/// <param name='duration'>How long to draw the circle.</param>
		/// <param name='depthTest'>Whether or not the circle should be faded when behind other objects.</param>
		public static void DrawCircle(Vector3 position, float radius = 1.0f, float duration = 0, bool depthTest = true)
		{
			DrawCircle(position, Vector3.up, Color.white, radius, duration, depthTest);
		}

		/// <summary>Debugs a wire sphere.</summary>
		/// <param name='position'>The position of the center of the sphere.</param>
		/// <param name='color'>The color of the sphere.</param>
		/// <param name='radius'>The radius of the sphere.</param>
		/// <param name='duration'>How long to draw the sphere.</param>
		/// <param name='depthTest'>Whether or not the sphere should be faded when behind other objects.</param>
		public static void DrawWireSphere(Vector3 position, Color color, float radius = 1.0f, float duration = 0, bool depthTest = true)
		{
			float angle = 10.0f;

			var x = new Vector3(position.x, position.y + radius * Mathf.Sin(0), position.z + radius * Mathf.Cos(0));
			var y = new Vector3(position.x + radius * Mathf.Cos(0), position.y, position.z + radius * Mathf.Sin(0));
			var z = new Vector3(position.x + radius * Mathf.Cos(0), position.y + radius * Mathf.Sin(0), position.z);

			for (int i = 1; i < 37; i++)
			{
				var new_x = new Vector3(position.x, position.y + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad), position.z + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad));
				var new_y = new Vector3(position.x + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad), position.y, position.z + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad));
				var new_z = new Vector3(position.x + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad), position.y + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad), position.z);

				UnityEngine.Debug.DrawLine(x, new_x, color, duration, depthTest);
				UnityEngine.Debug.DrawLine(y, new_y, color, duration, depthTest);
				UnityEngine.Debug.DrawLine(z, new_z, color, duration, depthTest);

				x = new_x;
				y = new_y;
				z = new_z;
			}
		}

		/// <summary>Debugs a wire sphere.</summary>
		/// <param name='position'>The position of the center of the sphere.</param>
		/// <param name='radius'>The radius of the sphere.</param>
		/// <param name='duration'>How long to draw the sphere.</param>
		/// <param name='depthTest'>Whether or not the sphere should be faded when behind other objects.</param>
		public static void DrawWireSphere(Vector3 position, float radius = 1.0f, float duration = 0, bool depthTest = true)
		{
			DrawWireSphere(position, Color.white, radius, duration, depthTest);
		}

		/// <summary>Debugs a cylinder.</summary>
		/// <param name='start'>The position of one end of the cylinder.</param>
		/// <param name='end'>The position of the other end of the cylinder.</param>
		/// <param name='color'>The color of the cylinder.</param>
		/// <param name='radius'>The radius of the cylinder.</param>
		/// <param name='duration'>How long to draw the cylinder.</param>
		/// <param name='depthTest'>Whether or not the cylinder should be faded when behind other objects.</param>
		public static void DrawCylinder(Vector3 start, Vector3 end, Color color, float radius = 1, float duration = 0, bool depthTest = true)
		{
			var up = (end - start).normalized * radius;
			var forward = Vector3.Slerp(up, -up, 0.5f);
			var right = Vector3.Cross(up, forward).normalized * radius;

			//Radial circles
			DrawCircle(start, up, color, radius, duration, depthTest);
			DrawCircle(end, -up, color, radius, duration, depthTest);
			DrawCircle((start + end) * 0.5f, up, color, radius, duration, depthTest);

			//Side lines
			UnityEngine.Debug.DrawLine(start + right, end + right, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(start - right, end - right, color, duration, depthTest);

			UnityEngine.Debug.DrawLine(start + forward, end + forward, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(start - forward, end - forward, color, duration, depthTest);

			//Start endcap
			UnityEngine.Debug.DrawLine(start - right, start + right, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(start - forward, start + forward, color, duration, depthTest);

			//End endcap
			UnityEngine.Debug.DrawLine(end - right, end + right, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(end - forward, end + forward, color, duration, depthTest);
		}

		/// <summary>Debugs a cylinder.</summary>
		/// <param name='start'>The position of one end of the cylinder.</param>
		/// <param name='end'>The position of the other end of the cylinder.</param>
		/// <param name='radius'>The radius of the cylinder.</param>
		/// <param name='duration'>How long to draw the cylinder.</param>
		/// <param name='depthTest'>Whether or not the cylinder should be faded when behind other objects.</param>
		public static void DrawCylinder(Vector3 start, Vector3 end, float radius = 1, float duration = 0, bool depthTest = true)
		{
			DrawCylinder(start, end, Color.white, radius, duration, depthTest);
		}

		/// <summary>Debugs a cone.</summary>
		/// <param name='position'>The position for the tip of the cone.</param>
		/// <param name='direction'>The direction for the cone gets wider in.</param>
		/// <param name='angle'>The angle of the cone.</param>
		/// <param name='color'>The color of the cone.</param>
		/// <param name='duration'>How long to draw the cone.</param>
		/// <param name='depthTest'>Whether or not the cone should be faded when behind other objects.</param>
		public static void DrawCone(Vector3 position, Vector3 direction, Color color, float angle = 45, float duration = 0, bool depthTest = true)
		{
			float length = direction.magnitude;

			var _forward = direction;
			var _up = Vector3.Slerp(_forward, -_forward, 0.5f);
			var _right = Vector3.Cross(_forward, _up).normalized * length;

			direction = direction.normalized;

			var slerpVector = Vector3.Slerp(_forward, _up, angle / 90.0f);

			var farPlane = new Plane(-direction, position + _forward);
			var distRay = new Ray(position, slerpVector);

			farPlane.Raycast(distRay, out float dist);

			UnityEngine.Debug.DrawRay(position, slerpVector.normalized * dist, color);
			UnityEngine.Debug.DrawRay(position, Vector3.Slerp(_forward, -_up, angle / 90.0f).normalized * dist, color, duration, depthTest);
			UnityEngine.Debug.DrawRay(position, Vector3.Slerp(_forward, _right, angle / 90.0f).normalized * dist, color, duration, depthTest);
			UnityEngine.Debug.DrawRay(position, Vector3.Slerp(_forward, -_right, angle / 90.0f).normalized * dist, color, duration, depthTest);

			DrawCircle(position + _forward, direction, color, (_forward - slerpVector.normalized * dist).magnitude, duration, depthTest);
			DrawCircle(position + _forward * 0.5f, direction, color, (_forward * 0.5f - slerpVector.normalized * (dist * 0.5f)).magnitude, duration, depthTest);
		}

		/// <summary>Debugs a cone.</summary>
		/// <param name='position'>The position for the tip of the cone.</param>
		/// <param name='direction'>The direction for the cone gets wider in.</param>
		/// <param name='angle'>The angle of the cone.</param>
		/// <param name='duration'>How long to draw the cone.</param>
		/// <param name='depthTest'>Whether or not the cone should be faded when behind other objects.</param>
		public static void DrawCone(Vector3 position, Vector3 direction, float angle = 45, float duration = 0, bool depthTest = true)
		{
			DrawCone(position, direction, Color.white, angle, duration, depthTest);
		}

		/// <summary>Debugs a cone.</summary>
		/// <param name='position'>The position for the tip of the cone.</param>
		/// <param name='angle'>The angle of the cone.</param>
		/// <param name='color'>The color of the cone.</param>
		/// <param name='duration'>How long to draw the cone.</param>
		/// <param name='depthTest'>Whether or not the cone should be faded when behind other objects.</param>
		public static void DrawCone(Vector3 position, Color color, float angle = 45, float duration = 0, bool depthTest = true)
		{
			DrawCone(position, Vector3.up, color, angle, duration, depthTest);
		}

		/// <summary>Debugs a cone.</summary>
		/// <param name='position'>The position for the tip of the cone.</param>
		/// <param name='angle'>The angle of the cone.</param>
		/// <param name='duration'>How long to draw the cone.</param>
		/// <param name='depthTest'>Whether or not the cone should be faded when behind other objects.</param>
		public static void DrawCone(Vector3 position, float angle = 45, float duration = 0, bool depthTest = true)
		{
			DrawCone(position, Vector3.up, Color.white, angle, duration, depthTest);
		}

		/// <summary>Debugs an arrow.</summary>
		/// <param name='position'>The start position of the arrow.</param>
		/// <param name='direction'>The direction the arrow will point in.</param>
		/// <param name='color'>The color of the arrow.</param>
		/// <param name='duration'>How long to draw the arrow.</param>
		/// <param name='depthTest'>Whether or not the arrow should be faded when behind other objects. </param>
		public static void DrawArrow(Vector3 position, Vector3 direction, Color color, float duration = 0, bool depthTest = true)
		{
			UnityEngine.Debug.DrawRay(position, direction, color, duration, depthTest);
			DrawCone(position + direction, -direction * 0.333f, color, 15, duration, depthTest);
		}

		/// <summary>Debugs an arrow.</summary>
		/// <param name='position'>The start position of the arrow.</param>
		/// <param name='direction'>The direction the arrow will point in.</param>
		/// <param name='duration'>How long to draw the arrow.</param>
		/// <param name='depthTest'>Whether or not the arrow should be faded when behind other objects. </param>
		public static void DrawArrow(Vector3 position, Vector3 direction, float duration = 0, bool depthTest = true)
		{
			DrawArrow(position, direction, Color.white, duration, depthTest);
		}

		/// <summary>Debugs a capsule.</summary>
		/// <param name='start'>The position of one end of the capsule.</param>
		/// <param name='end'>The position of the other end of the capsule.</param>
		/// <param name='color'>The color of the capsule.</param>
		/// <param name='radius'>The radius of the capsule.</param>
		/// <param name='duration'>How long to draw the capsule.</param>
		/// <param name='depthTest'>Whether or not the capsule should be faded when behind other objects.</param>
		public static void DrawCapsule(Vector3 start, Vector3 end, Color color, float radius = 1, float duration = 0, bool depthTest = true)
		{
			var up = (end - start).normalized * radius;
			var forward = Vector3.Slerp(up, -up, 0.5f);
			var right = Vector3.Cross(up, forward).normalized * radius;

			float height = (start - end).magnitude;
			float sideLength = Mathf.Max(0, height * 0.5f - radius);
			var middle = (end + start) * 0.5f;

			start = middle + (start - middle).normalized * sideLength;
			end = middle + (end - middle).normalized * sideLength;

			//Radial circles
			DrawCircle(start, up, color, radius, duration, depthTest);
			DrawCircle(end, -up, color, radius, duration, depthTest);

			//Side lines
			UnityEngine.Debug.DrawLine(start + right, end + right, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(start - right, end - right, color, duration, depthTest);

			UnityEngine.Debug.DrawLine(start + forward, end + forward, color, duration, depthTest);
			UnityEngine.Debug.DrawLine(start - forward, end - forward, color, duration, depthTest);

			for (int i = 1; i < 26; i++)
			{
				//Start endcap
				UnityEngine.Debug.DrawLine(Vector3.Slerp(right, -up, i / 25.0f) + start, Vector3.Slerp(right, -up, (i - 1) / 25.0f) + start, color, duration, depthTest);
				UnityEngine.Debug.DrawLine(Vector3.Slerp(-right, -up, i / 25.0f) + start, Vector3.Slerp(-right, -up, (i - 1) / 25.0f) + start, color, duration, depthTest);
				UnityEngine.Debug.DrawLine(Vector3.Slerp(forward, -up, i / 25.0f) + start, Vector3.Slerp(forward, -up, (i - 1) / 25.0f) + start, color, duration, depthTest);
				UnityEngine.Debug.DrawLine(Vector3.Slerp(-forward, -up, i / 25.0f) + start, Vector3.Slerp(-forward, -up, (i - 1) / 25.0f) + start, color, duration, depthTest);

				//End endcap
				UnityEngine.Debug.DrawLine(Vector3.Slerp(right, up, i / 25.0f) + end, Vector3.Slerp(right, up, (i - 1) / 25.0f) + end, color, duration, depthTest);
				UnityEngine.Debug.DrawLine(Vector3.Slerp(-right, up, i / 25.0f) + end, Vector3.Slerp(-right, up, (i - 1) / 25.0f) + end, color, duration, depthTest);
				UnityEngine.Debug.DrawLine(Vector3.Slerp(forward, up, i / 25.0f) + end, Vector3.Slerp(forward, up, (i - 1) / 25.0f) + end, color, duration, depthTest);
				UnityEngine.Debug.DrawLine(Vector3.Slerp(-forward, up, i / 25.0f) + end, Vector3.Slerp(-forward, up, (i - 1) / 25.0f) + end, color, duration, depthTest);
			}
		}

		/// <summary>Debugs a capsule.</summary>
		/// <param name='start'>The position of one end of the capsule.</param>
		/// <param name='end'>The position of the other end of the capsule.</param>
		/// <param name='radius'>The radius of the capsule.</param>
		/// <param name='duration'>How long to draw the capsule.</param>
		/// <param name='depthTest'>Whether or not the capsule should be faded when behind other objects.</param>
		public static void DrawCapsule(Vector3 start, Vector3 end, float radius = 1, float duration = 0, bool depthTest = true)
		{
			DrawCapsule(start, end, Color.white, radius, duration, depthTest);
		}

#endregion

#region GizmoDrawFunctions

		/// <summary>Draws a point.</summary>
		/// <param name='position'>The point to draw.</param>
		///  <param name='color'>The color of the drawn point.</param>
		/// <param name='scale'>The size of the drawn point.</param>
		public static void DrawGizmosPoint(Vector3 position, Color color, float scale = 1.0f)
		{
			var oldColor = Gizmos.color;

			Gizmos.color = color;
			Gizmos.DrawRay(position + Vector3.up * (scale * 0.5f), -Vector3.up * scale);
			Gizmos.DrawRay(position + Vector3.right * (scale * 0.5f), -Vector3.right * scale);
			Gizmos.DrawRay(position + Vector3.forward * (scale * 0.5f), -Vector3.forward * scale);

			Gizmos.color = oldColor;
		}

		/// <summary>Draws a point.</summary>
		/// <param name='position'>The point to draw.</param>
		/// <param name='scale'>The size of the drawn point.</param>
		public static void DrawGizmosPoint(Vector3 position, float scale = 1.0f)
		{
			DrawGizmosPoint(position, Color.white, scale);
		}

		/// <summary>Draws an axis-aligned bounding box.</summary>
		/// <param name='bounds'>The bounds to draw.</param>
		/// <param name='color'>The color of the bounds.</param>
		public static void DrawGizmosBounds(Vector3 pRoot, Bounds bounds, Color color)
		{
			var center = bounds.center + pRoot;

			float x = bounds.extents.x;
			float y = bounds.extents.y;
			float z = bounds.extents.z;

			var ruf = center + new Vector3(x, y, z);
			var rub = center + new Vector3(x, y, -z);
			var luf = center + new Vector3(-x, y, z);
			var lub = center + new Vector3(-x, y, -z);

			var rdf = center + new Vector3(x, -y, z);
			var rdb = center + new Vector3(x, -y, -z);
			var lfd = center + new Vector3(-x, -y, z);
			var lbd = center + new Vector3(-x, -y, -z);

			var oldColor = Gizmos.color;
			Gizmos.color = color;

			Gizmos.DrawLine(ruf, luf);
			Gizmos.DrawLine(ruf, rub);
			Gizmos.DrawLine(luf, lub);
			Gizmos.DrawLine(rub, lub);

			Gizmos.DrawLine(ruf, rdf);
			Gizmos.DrawLine(rub, rdb);
			Gizmos.DrawLine(luf, lfd);
			Gizmos.DrawLine(lub, lbd);

			Gizmos.DrawLine(rdf, lfd);
			Gizmos.DrawLine(rdf, rdb);
			Gizmos.DrawLine(lfd, lbd);
			Gizmos.DrawLine(lbd, rdb);

			Gizmos.color = oldColor;
		}

		/// <summary>Draws an axis-aligned bounding box.</summary>
		/// <param name='bounds'>The bounds to draw.</param>
		public static void DrawGizmosBounds(Vector3 pRoot, Bounds bounds)
		{
			DrawGizmosBounds(pRoot, bounds, Color.white);
		}

		/// <summary>Draws a local cube.</summary>
		/// <param name='transform'>The transform the cube will be local to.</param>
		/// <param name='size'>The local size of the cube.</param>
		/// <param name='center'>The local position of the cube.</param>
		/// <param name='color'>The color of the cube.</param>
		public static void DrawGizmosLocalCube(Transform transform, Vector3 size, Color color, Vector3 center = default)
		{
			var oldColor = Gizmos.color;
			Gizmos.color = color;

			var lbb = transform.TransformPoint(center + -size * 0.5f);
			var rbb = transform.TransformPoint(center + new Vector3(size.x, -size.y, -size.z) * 0.5f);

			var lbf = transform.TransformPoint(center + new Vector3(size.x, -size.y, size.z) * 0.5f);
			var rbf = transform.TransformPoint(center + new Vector3(-size.x, -size.y, size.z) * 0.5f);

			var lub = transform.TransformPoint(center + new Vector3(-size.x, size.y, -size.z) * 0.5f);
			var rub = transform.TransformPoint(center + new Vector3(size.x, size.y, -size.z) * 0.5f);

			var luf = transform.TransformPoint(center + size * 0.5f);
			var ruf = transform.TransformPoint(center + new Vector3(-size.x, size.y, size.z) * 0.5f);

			Gizmos.DrawLine(lbb, rbb);
			Gizmos.DrawLine(rbb, lbf);
			Gizmos.DrawLine(lbf, rbf);
			Gizmos.DrawLine(rbf, lbb);

			Gizmos.DrawLine(lub, rub);
			Gizmos.DrawLine(rub, luf);
			Gizmos.DrawLine(luf, ruf);
			Gizmos.DrawLine(ruf, lub);

			Gizmos.DrawLine(lbb, lub);
			Gizmos.DrawLine(rbb, rub);
			Gizmos.DrawLine(lbf, luf);
			Gizmos.DrawLine(rbf, ruf);

			Gizmos.color = oldColor;
		}

		/// <summary>Draws a local cube.</summary>
		/// <param name='transform'>The transform the cube will be local to.</param>
		/// <param name='size'>The local size of the cube.</param>
		/// <param name='center'>The local position of the cube.</param>	
		public static void DrawGizmosLocalCube(Transform transform, Vector3 size, Vector3 center = default)
		{
			DrawGizmosLocalCube(transform, size, Color.white, center);
		}

		/// <summary>Draws a local cube.</summary>
		/// <param name='space'>The space the cube will be local to.</param>
		/// <param name='size'>The local size of the cube.</param>
		/// <param name='center'>The local position of the cube.</param>
		/// <param name='color'>The color of the cube.</param>
		public static void DrawGizmosLocalCube(Matrix4x4 space, Vector3 size, Color color, Vector3 center = default)
		{
			var oldColor = Gizmos.color;
			Gizmos.color = color;

			var lbb = space.MultiplyPoint3x4(center + -size * 0.5f);
			var rbb = space.MultiplyPoint3x4(center + new Vector3(size.x, -size.y, -size.z) * 0.5f);

			var lbf = space.MultiplyPoint3x4(center + new Vector3(size.x, -size.y, size.z) * 0.5f);
			var rbf = space.MultiplyPoint3x4(center + new Vector3(-size.x, -size.y, size.z) * 0.5f);

			var lub = space.MultiplyPoint3x4(center + new Vector3(-size.x, size.y, -size.z) * 0.5f);
			var rub = space.MultiplyPoint3x4(center + new Vector3(size.x, size.y, -size.z) * 0.5f);

			var luf = space.MultiplyPoint3x4(center + size * 0.5f);
			var ruf = space.MultiplyPoint3x4(center + new Vector3(-size.x, size.y, size.z) * 0.5f);

			Gizmos.DrawLine(lbb, rbb);
			Gizmos.DrawLine(rbb, lbf);
			Gizmos.DrawLine(lbf, rbf);
			Gizmos.DrawLine(rbf, lbb);

			Gizmos.DrawLine(lub, rub);
			Gizmos.DrawLine(rub, luf);
			Gizmos.DrawLine(luf, ruf);
			Gizmos.DrawLine(ruf, lub);

			Gizmos.DrawLine(lbb, lub);
			Gizmos.DrawLine(rbb, rub);
			Gizmos.DrawLine(lbf, luf);
			Gizmos.DrawLine(rbf, ruf);

			Gizmos.color = oldColor;
		}

		/// <summary>Draws a local cube.</summary>
		/// <param name='space'>The space the cube will be local to.</param>
		/// <param name='size'>The local size of the cube.</param>
		/// <param name='center'>The local position of the cube.</param>
		public static void DrawGizmosLocalCube(Matrix4x4 space, Vector3 size, Vector3 center = default)
		{
			DrawGizmosLocalCube(space, size, Color.white, center);
		}

		/// <summary>Draws a circle.</summary>
		/// <param name='position'>Where the center of the circle will be positioned.</param>
		/// <param name='up'>The direction perpendicular to the surface of the circle.</param>
		/// <param name='color'>The color of the circle.</param>
		/// <param name='radius'>The radius of the circle.</param>
		public static void DrawGizmosCircle(Vector3 position, Vector3 up, Color color, float radius = 1.0f)
		{
			up = (up == Vector3.zero ? Vector3.up : up).normalized * radius;
			var forward = Vector3.Slerp(up, -up, 0.5f);
			var right = Vector3.Cross(up, forward).normalized * radius;

			var matrix = new Matrix4x4
			{
				[0] = right.x,
				[1] = right.y,
				[2] = right.z,
				[4] = up.x,
				[5] = up.y,
				[6] = up.z,
				[8] = forward.x,
				[9] = forward.y,
				[10] = forward.z
			};

			var lastPoint = position + matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)));
			var nextPoint = Vector3.zero;

			var oldColor = Gizmos.color;
			Gizmos.color = color == default ? Color.white : color;

			for (var i = 0; i < 91; i++)
			{
				nextPoint.x = Mathf.Cos(i * 4 * Mathf.Deg2Rad);
				nextPoint.z = Mathf.Sin(i * 4 * Mathf.Deg2Rad);
				nextPoint.y = 0;

				nextPoint = position + matrix.MultiplyPoint3x4(nextPoint);

				Gizmos.DrawLine(lastPoint, nextPoint);
				lastPoint = nextPoint;
			}

			Gizmos.color = oldColor;
		}

		/// <summary>Draws a circle.</summary>
		/// <param name='position'>Where the center of the circle will be positioned.</param>
		/// <param name='color'>The color of the circle.</param>
		/// <param name='radius'>The radius of the circle.</param>
		public static void DrawGizmosCircle(Vector3 position, Color color, float radius = 1.0f)
		{
			DrawGizmosCircle(position, Vector3.up, color, radius);
		}

		/// <summary>Draws a circle.</summary>
		/// <param name='position'>Where the center of the circle will be positioned.</param>
		/// <param name='up'>The direction perpendicular to the surface of the circle.</param>
		/// <param name='radius'>The radius of the circle.</param>
		public static void DrawGizmosCircle(Vector3 position, Vector3 up, float radius = 1.0f)
		{
			DrawGizmosCircle(position, position, Color.white, radius);
		}

		/// <summary>Draws a circle.</summary>
		/// <param name='position'>Where the center of the circle will be positioned.</param>
		/// <param name='radius'>The radius of the circle.</param>
		public static void DrawGizmosCircle(Vector3 position, float radius = 1.0f)
		{
			DrawGizmosCircle(position, Vector3.up, Color.white, radius);
		}

		//Wire sphere already exists

		/// <summary>Draws a cylinder.</summary>
		/// <param name='start'>The position of one end of the cylinder.</param>
		/// <param name='end'>The position of the other end of the cylinder.</param>
		/// <param name='color'>The color of the cylinder.</param>
		/// <param name='radius'>The radius of the cylinder.</param>
		public static void DrawGizmosCylinder(Vector3 start, Vector3 end, Color color, float radius = 1.0f)
		{
			var up = (end - start).normalized * radius;
			var forward = Vector3.Slerp(up, -up, 0.5f);
			var right = Vector3.Cross(up, forward).normalized * radius;

			//Radial circles
			DrawGizmosCircle(start, up, color, radius);
			DrawGizmosCircle(end, -up, color, radius);
			DrawGizmosCircle((start + end) * 0.5f, up, color, radius);

			var oldColor = Gizmos.color;
			Gizmos.color = color;

			//Side lines
			Gizmos.DrawLine(start + right, end + right);
			Gizmos.DrawLine(start - right, end - right);

			Gizmos.DrawLine(start + forward, end + forward);
			Gizmos.DrawLine(start - forward, end - forward);

			//Start endcap
			Gizmos.DrawLine(start - right, start + right);
			Gizmos.DrawLine(start - forward, start + forward);

			//End endcap
			Gizmos.DrawLine(end - right, end + right);
			Gizmos.DrawLine(end - forward, end + forward);

			Gizmos.color = oldColor;
		}

		/// <summary>Draws a cylinder.</summary>
		/// <param name='start'>The position of one end of the cylinder.</param>
		/// <param name='end'>The position of the other end of the cylinder.</param>
		/// <param name='radius'>The radius of the cylinder.</param>
		public static void DrawGizmosCylinder(Vector3 start, Vector3 end, float radius = 1.0f)
		{
			DrawGizmosCylinder(start, end, Color.white, radius);
		}

		/// <summary>Draws a cone.</summary>
		/// <param name='position'>The position for the tip of the cone.</param>
		/// <param name='direction'>The direction for the cone to get wider in.</param>
		/// <param name='color'>The color of the cone.</param>
		/// <param name='angle'>The angle of the cone.</param>
		public static void DrawGizmosCone(Vector3 position, Vector3 direction, Color color, float angle = 45)
		{
			float length = direction.magnitude;

			var _forward = direction;
			var _up = Vector3.Slerp(_forward, -_forward, 0.5f);
			var _right = Vector3.Cross(_forward, _up).normalized * length;

			direction = direction.normalized;

			var slerpVector = Vector3.Slerp(_forward, _up, angle / 90.0f);

			var farPlane = new Plane(-direction, position + _forward);
			var distRay = new Ray(position, slerpVector);

			farPlane.Raycast(distRay, out float dist);

			var oldColor = Gizmos.color;
			Gizmos.color = color;

			Gizmos.DrawRay(position, slerpVector.normalized * dist);
			Gizmos.DrawRay(position, Vector3.Slerp(_forward, -_up, angle / 90.0f).normalized * dist);
			Gizmos.DrawRay(position, Vector3.Slerp(_forward, _right, angle / 90.0f).normalized * dist);
			Gizmos.DrawRay(position, Vector3.Slerp(_forward, -_right, angle / 90.0f).normalized * dist);

			DrawGizmosCircle(position + _forward, direction, color, (_forward - slerpVector.normalized * dist).magnitude);
			DrawGizmosCircle(position + _forward * 0.5f, direction, color, (_forward * 0.5f - slerpVector.normalized * (dist * 0.5f)).magnitude);

			Gizmos.color = oldColor;
		}

		/// <summary>Draws a cone.</summary>
		/// <param name='position'>The position for the tip of the cone.</param>
		/// <param name='direction'>The direction for the cone to get wider in.</param>
		/// <param name='angle'>The angle of the cone.</param>
		public static void DrawGizmosCone(Vector3 position, Vector3 direction, float angle = 45)
		{
			DrawGizmosCone(position, direction, Color.white, angle);
		}

		/// <summary>Draws a cone.</summary>
		/// <param name='position'>The position for the tip of the cone.</param>
		/// <param name='color'>The color of the cone.</param>
		/// <param name='angle'>The angle of the cone.</param>
		public static void DrawGizmosCone(Vector3 position, Color color, float angle = 45)
		{
			DrawGizmosCone(position, Vector3.up, color, angle);
		}

		/// <summary>Draws a cone.</summary>
		/// <param name='position'>The position for the tip of the cone.</param>
		/// <param name='angle'>The angle of the cone.</param>
		public static void DrawGizmosCone(Vector3 position, float angle = 45)
		{
			DrawGizmosCone(position, Vector3.up, Color.white, angle);
		}

		/// <summary>Draws an arrow.</summary>
		/// <param name='position'>The start position of the arrow.</param>
		/// <param name='direction'>The direction the arrow will point in.</param>
		/// <param name='color'>The color of the arrow.</param>
		public static void DrawGizmosArrow(Vector3 position, Vector3 direction, Color color)
		{
			var oldColor = Gizmos.color;
			Gizmos.color = color;

			Gizmos.DrawRay(position, direction);
			DrawGizmosCone(position + direction, -direction * 0.333f, color, 15);

			Gizmos.color = oldColor;
		}

		/// <summary>Draws an arrow.</summary>
		/// <param name='position'>The start position of the arrow.</param>
		/// <param name='direction'>The direction the arrow will point in.</param>
		public static void DrawGizmosArrow(Vector3 position, Vector3 direction)
		{
			DrawGizmosArrow(position, direction, Color.white);
		}

		/// <summary>Draws a capsule.</summary>
		/// <param name='start'>The position of one end of the capsule.</param>
		/// <param name='end'>The position of the other end of the capsule.</param>
		/// <param name='color'>The color of the capsule.</param>
		/// <param name='radius'>The radius of the capsule.</param>
		public static void DrawGizmosCapsule(Vector3 start, Vector3 end, Color color, float radius = 1)
		{
			var up = (end - start).normalized * radius;
			var forward = Vector3.Slerp(up, -up, 0.5f);
			var right = Vector3.Cross(up, forward).normalized * radius;

			var oldColor = Gizmos.color;
			Gizmos.color = color;

			float height = (start - end).magnitude;
			float sideLength = Mathf.Max(0, height * 0.5f - radius);
			var middle = (end + start) * 0.5f;

			start = middle + (start - middle).normalized * sideLength;
			end = middle + (end - middle).normalized * sideLength;

			//Radial circles
			DrawGizmosCircle(start, up, color, radius);
			DrawGizmosCircle(end, -up, color, radius);

			//Side lines
			Gizmos.DrawLine(start + right, end + right);
			Gizmos.DrawLine(start - right, end - right);

			Gizmos.DrawLine(start + forward, end + forward);
			Gizmos.DrawLine(start - forward, end - forward);

			for (int i = 1; i < 26; i++)
			{
				//Start endcap
				Gizmos.DrawLine(Vector3.Slerp(right, -up, i / 25.0f) + start, Vector3.Slerp(right, -up, (i - 1) / 25.0f) + start);
				Gizmos.DrawLine(Vector3.Slerp(-right, -up, i / 25.0f) + start, Vector3.Slerp(-right, -up, (i - 1) / 25.0f) + start);
				Gizmos.DrawLine(Vector3.Slerp(forward, -up, i / 25.0f) + start, Vector3.Slerp(forward, -up, (i - 1) / 25.0f) + start);
				Gizmos.DrawLine(Vector3.Slerp(-forward, -up, i / 25.0f) + start, Vector3.Slerp(-forward, -up, (i - 1) / 25.0f) + start);

				//End endcap
				Gizmos.DrawLine(Vector3.Slerp(right, up, i / 25.0f) + end, Vector3.Slerp(right, up, (i - 1) / 25.0f) + end);
				Gizmos.DrawLine(Vector3.Slerp(-right, up, i / 25.0f) + end, Vector3.Slerp(-right, up, (i - 1) / 25.0f) + end);
				Gizmos.DrawLine(Vector3.Slerp(forward, up, i / 25.0f) + end, Vector3.Slerp(forward, up, (i - 1) / 25.0f) + end);
				Gizmos.DrawLine(Vector3.Slerp(-forward, up, i / 25.0f) + end, Vector3.Slerp(-forward, up, (i - 1) / 25.0f) + end);
			}

			Gizmos.color = oldColor;
		}

		/// <summary>Draws a capsule.</summary>
		/// <param name='start'>The position of one end of the capsule.</param>
		/// <param name='end'>The position of the other end of the capsule.</param>
		/// <param name='radius'>The radius of the capsule.</param>
		public static void DrawGizmosCapsule(Vector3 start, Vector3 end, float radius = 1)
		{
			DrawGizmosCapsule(start, end, Color.white, radius);
		}

#endregion

#region DebugFunctions

		/// <summary>Gets the methods of an object.</summary>
		/// <returns>A list of methods accessible from this object.</returns>
		/// <param name='obj'>The object to get the methods of.</param>
		/// <param name='includeInfo'>Whether or not to include each method's method info in the list.</param>
		public static string MethodsOfObject(System.Object obj, bool includeInfo = false)
		{
			string methods = "";
			var methodInfos = obj.GetType().GetMethods();
			for (int i = 0; i < methodInfos.Length; i++)
			{
				if (includeInfo)
				{
					methods += methodInfos[i] + "\n";
				}

				else
				{
					methods += methodInfos[i].Name + "\n";
				}
			}

			return methods;
		}

		/// <summary>Gets the methods of a type.</summary>
		/// <returns>A list of methods accessible from this type.</returns>
		/// <param name='type'>The type to get the methods of.</param>
		/// <param name='includeInfo'>Whether or not to include each method's method info in the list.</param>
		public static string MethodsOfType(System.Type type, bool includeInfo = false)
		{
			string methods = "";
			var methodInfos = type.GetMethods();
			for (var i = 0; i < methodInfos.Length; i++)
			{
				if (includeInfo)
				{
					methods += methodInfos[i] + "\n";
				}

				else
				{
					methods += methodInfos[i].Name + "\n";
				}
			}

			return methods;
		}

#endregion
	}
}