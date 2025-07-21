/**
 * Author HNB-RaBear - 2018
 **/

using System.Collections.Generic;
using UnityEngine;

namespace RCore
{
	/// <summary>
	/// A comprehensive utility class for various mathematical and geometrical calculations.
	/// </summary>
	public static class MathHelper
	{
		#region Angle Calculation & Trigonometry

		/// <summary>
		/// Calculates the angle in degrees between two vectors.
		/// </summary>
		public static float CalcAngle(Vector3 dir1, Vector3 dir2)
		{
			float dot = Vector3.Dot(dir1.normalized, dir2.normalized);
			dot = Mathf.Clamp(dot, -1.0f, 1.0f);
			return Mathf.Acos(dot) * Mathf.Rad2Deg;
		}

		/// <summary>
		/// Calculates the angle in degrees of a 2D direction vector.
		/// </summary>
		public static float CalcAngle(Vector2 direction)
		{
			return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		}

		/// <summary>
		/// Calculates the full 360-degree Euler angle required to rotate from Vector3.forward to the target direction.
		/// </summary>
		public static Vector3 CalcAngle360(Vector3 fromDir, Vector3 toDir)
		{
			return Quaternion.FromToRotation(Vector3.forward, toDir - fromDir).eulerAngles;
		}

		/// <summary>
		/// Calculates the Z-axis angle (in Euler angles) to rotate from Vector3.right to the target direction.
		/// </summary>
		public static float CalcAngle360X(Vector3 fromDir, Vector3 toDir)
		{
			return Quaternion.FromToRotation(Vector3.right, toDir - fromDir).eulerAngles.z;
		}

		/// <summary>
		/// Calculates the Z-axis angle (in Euler angles) to rotate from Vector3.up to the target direction.
		/// </summary>
		public static float CalcAngle360Z(Vector3 fromDir, Vector3 toDir)
		{
			return Quaternion.FromToRotation(Vector3.up, toDir - fromDir).eulerAngles.z;
		}

		/// <summary>
		/// Calculates the Y-axis angle (in Euler angles) to rotate from Vector3.forward to the target direction.
		/// </summary>
		public static float CalcAngle360Y(Vector3 fromDir, Vector3 toDir)
		{
			return Quaternion.FromToRotation(Vector3.forward, toDir - fromDir).eulerAngles.y;
		}

		/// <summary>
		/// Ensure that the angle is within -180 to 180 range.
		/// </summary>
		public static float WrapAngle(float angle)
		{
			while (angle > 180f) angle -= 360f;
			while (angle < -180f) angle += 360f;
			return angle;
		}
		
		/// <summary>
		/// Checks if an angle is between a min and max angle.
		/// </summary>
		public static bool InsideAngle(float angle, float minAngle, float maxAngle)
		{
			float normalizedAngle = (angle % 360 + 360) % 360;
			float normalizedMin = (minAngle % 360 + 360) % 360;
			float normalizedMax = (maxAngle % 360 + 360) % 360;

			if (normalizedMin <= normalizedMax)
				return normalizedAngle >= normalizedMin && normalizedAngle <= normalizedMax;
			
			return normalizedAngle >= normalizedMin || normalizedAngle <= normalizedMax;
		}

		/// <summary>
		/// Gets a direction vector from an angle in degrees around the Y-axis.
		/// </summary>
		public static Vector3 DirOfYAngle(float pAngleInDegrees)
		{
			float rad = pAngleInDegrees * Mathf.Deg2Rad;
			return new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
		}

		/// <summary>
		/// Gets a direction vector from an angle in degrees around the X-axis.
		/// </summary>
		public static Vector3 DirOfXAngle(float pAngleInDegrees)
		{
			return Quaternion.AngleAxis(pAngleInDegrees, Vector3.right) * Vector3.forward;
		}

		/// <summary>
		/// Gets a direction vector from an angle in degrees around the Z-axis.
		/// </summary>
		public static Vector3 DirOfZAngle(float pAngleInDegrees)
		{
			return Quaternion.AngleAxis(pAngleInDegrees, Vector3.forward) * Vector3.up;
		}

		/// <summary>
		/// Gets a direction vector from a full Euler angle.
		/// </summary>
		public static Vector3 DirOfAngle(Vector3 pAngle)
		{
			return Quaternion.Euler(pAngle) * Vector3.forward;
		}
		
		public static float SinRad(float pRadiant) => Mathf.Sin(pRadiant);
		public static float CosRad(float pRadiant) => Mathf.Cos(pRadiant);
		public static float SinDeg(float pDegree) => Mathf.Sin(pDegree * Mathf.Deg2Rad);
		public static float CosDeg(float pDegree) => Mathf.Cos(pDegree * Mathf.Deg2Rad);
		public static float TanDeg(float pDegree) => Mathf.Tan(pDegree * Mathf.Deg2Rad);
		public static float Ded2Rad(float pDegree) => pDegree * Mathf.Deg2Rad;
		public static float Tad2Deg(float pRadiant) => pRadiant * Mathf.Rad2Deg;
		public static float AngleDeg(Vector2 pFrom, Vector2 pTo) => AtanDeg(pTo.y - pFrom.y, pTo.x - pFrom.x);
		public static float AtanDeg(float dy, float dx) => Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
		public static float AtanRad(float dy, float dx) => Mathf.Atan2(dy, dx);
		
		#endregion

		#region Geometric Checks & Calculations

		/// <summary>
		/// Checks if a point is in front of a transform.
		/// </summary>
		/// <returns>Positive if in front, negative if behind.</returns>
		public static float Dot(Transform pRoot, Transform pTarget)
		{
			Vector3 dir = pTarget.position - pRoot.position;
			return Vector3.Dot(pRoot.forward, dir.normalized);
		}

		/// <summary>
		/// Checks if a position is to the right or left of a transform.
		/// </summary>
		/// <returns>True if to the right, false if to the left.</returns>
		public static bool OnRightOrLeft(Transform pRoot, Vector3 pPosition)
		{
			Vector3 dir = pPosition - pRoot.position;
			return Vector3.Dot(pRoot.right, dir) > 0;
		}

		/// <summary>
		/// Calculates the area of a triangle formed by three points.
		/// </summary>
		private static float Area(Vector2 a, Vector2 b, Vector2 c)
		{
			return Mathf.Abs((a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y)) / 2.0f);
		}

		/// <summary>
		/// Checks whether a point P lies inside the triangle formed by A, B, and C.
		/// </summary>
		public static bool IsInside(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
		{
			float totalArea = Area(a, b, c);
			float area1 = Area(p, b, c);
			float area2 = Area(a, p, c);
			float area3 = Area(a, b, p);
			return Mathf.Approximately(totalArea, area1 + area2 + area3);
		}

		/// <summary>
		/// Checks if a point 'mid' is between two other points 'from' and 'to' on a line.
		/// </summary>
		public static bool IsBetween(Vector3 from, Vector3 to, Vector3 mid)
		{
			return Vector3.Dot(mid - from, to - from) > 0 && Vector3.Dot(mid - to, from - to) > 0;
		}
		
		/// <summary>
		/// Checks if a vector 'mid' is angularly between vectors 'from' and 'to' in the XZ plane.
		/// </summary>
		public static bool IsBetweenXZ(Vector3 from, Vector3 to, Vector3 mid)
		{
			return (from.z * mid.x - from.x * mid.z) * (to.z * mid.x - to.x * mid.z) < 0;
		}
		
		/// <summary>
		/// Checks if a vector 'mid' is angularly between vectors 'from' and 'to'.
		/// </summary>
		public static bool IsBetween(Vector2 from, Vector2 to, Vector2 mid)
		{
			return (from.y * mid.x - from.x * mid.y) * (to.y * mid.x - to.x * mid.y) < 0;
		}

		/// <summary>
		/// Calculates a position based on a root position, distance, and direction.
		/// </summary>
		public static Vector3 CalcPosition(Vector3 pRootPos, float pDistance, Vector3 pDir)
		{
			return pRootPos + pDir.normalized * pDistance;
		}

		public static Vector3 LeftDirection(Vector3 pForward, Vector3 pUp) => Vector3.Cross(pUp, pForward);
		public static Vector3 RightDirection(Vector3 pForward, Vector3 pUp) => Vector3.Cross(pForward, pUp);
		public static Vector3 LeftDirectionXZ(Vector3 pForward) => new Vector3(-pForward.z, pForward.y, pForward.x);
		public static Vector3 RightDirectionXZ(Vector3 pForward) => new Vector3(pForward.z, pForward.y, -pForward.x);

		/// <summary>
		/// Compares two Vector3 values component by component (X, then Y, then Z).
		/// </summary>
		public static int VectorCompareXYZ(Vector3 value1, Vector3 value2)
		{
			if (value1.x < value2.x) return -1;
			if (value1.x > value2.x) return 1;
			if (value1.y < value2.y) return -1;
			if (value1.y > value2.y) return 1;
			if (value1.z < value2.z) return -1;
			if (value1.z > value2.z) return 1;
			return 0;
		}
		
		/// <summary>
		/// Calculates the center point of a collection of vectors.
		/// </summary>
		public static Vector3 GetCenterVector(IEnumerable<Vector3> vectors)
		{
			Vector3 sum = Vector3.zero;
			int count = 0;
			if (vectors == null) return sum;
			foreach (var vec in vectors)
			{
				sum += vec;
				count++;
			}
			return (count == 0) ? Vector3.zero : sum / count;
		}

		#endregion

		#region Random Geometry & Chances

		private static System.Random _random = new System.Random();

		/// <summary>
		/// Returns a random point on the surface of a sphere.
		/// </summary>
		public static Vector3 RandomPointOnSphere(float radius)
		{
			float u = (float)_random.NextDouble();
			float v = (float)_random.NextDouble();
			float theta = 2 * Mathf.PI * u;
			float phi = Mathf.Acos(2 * v - 1);
			float x = radius * Mathf.Cos(theta) * Mathf.Sin(phi);
			float y = radius * Mathf.Sin(theta) * Mathf.Sin(phi);
			float z = radius * Mathf.Cos(phi);
			return new Vector3(x, y, z);
		}

		/// <summary>
		/// Returns a random point on the edge of a circle in the XZ plane.
		/// </summary>
		public static Vector3 RandomPointOnCircleEdge_XZ(Vector3 pCenter, float pRadius)
		{
			Vector2 pos = Random.insideUnitCircle.normalized * pRadius;
			return new Vector3(pCenter.x + pos.x, pCenter.y, pCenter.z + pos.y);
		}

		/// <summary>
		/// Returns a random point inside a circle in the XZ plane.
		/// </summary>
		public static Vector3 RandomPointOnCircle_XZ(Vector3 pCenter, float pRadius)
		{
			Vector2 pos = Random.insideUnitCircle * pRadius;
			return new Vector3(pCenter.x + pos.x, pCenter.y, pCenter.z + pos.y);
		}

		/// <summary>
		/// Returns a random point on the edge of a circle in the XY plane.
		/// </summary>
		public static Vector3 RandomPointOnCircleEdge_XY(Vector2 pCenter, float pRadius)
		{
			Vector2 pos = Random.insideUnitCircle.normalized * pRadius;
			return new Vector3(pCenter.x + pos.x, pCenter.y + pos.y, 0);
		}
		
		public static Vector2 GetPosOnCircle(float pAngleDeg, float pRadius)
		{
			return new Vector2(CosDeg(pAngleDeg) * pRadius, SinDeg(pAngleDeg) * pRadius);
		}
		
		public static Vector3 GetPosOnCircle(Vector3 pRoot, float pAngleDeg, float pRadius)
		{
			return pRoot + DirOfYAngle(pAngleDeg) * pRadius;
		}
		
		public static int GetRandomIndexFromChances(List<int> chances)
		{
			int totalRatios = 0;
			for (int i = 0; i < chances.Count; i++) totalRatios += chances[i];
			
			float random = Random.Range(0, totalRatios);
			float temp = 0;
			for (int i = 0; i < chances.Count; i++)
			{
				temp += chances[i];
				if (temp > random) return i;
			}
			return chances.Count - 1;
		}

		public static int GetRandomIndexFromChances(params float[] chances)
		{
			float totalRatios = 0;
			for (int i = 0; i < chances.Length; i++) totalRatios += chances[i];
			
			float random = Random.Range(0, totalRatios);
			float temp = 0;
			for (int i = 0; i < chances.Length; i++)
			{
				temp += chances[i];
				if (temp > random) return i;
			}
			return chances.Length - 1;
		}

		public static int GetRandomIndexFromChances(params int[] chances)
		{
			int totalRatios = 0;
			for (int i = 0; i < chances.Length; i++) totalRatios += chances[i];
			
			float random = Random.Range(0, totalRatios);
			float temp = 0;
			for (int i = 0; i < chances.Length; i++)
			{
				temp += chances[i];
				if (temp > random) return i;
			}
			return chances.Length - 1;
		}

		#endregion

		#region Intersection, Overlap & Distance

		private const int Intersection_OUT_SIDE = 0;
		private const int Intersection_IN_SIDE = 1;
		private const int Intersection_CUT = 2;
		
		/// <summary>
		/// Determines the distance from a point to a line segment.
		/// </summary>
		private static float DistancePointToLineSegment(Vector2 point, Vector2 a, Vector2 b)
		{
			float l2 = (b - a).sqrMagnitude;
			if (l2 == 0f) return (point - a).magnitude;
			float t = Mathf.Clamp01(Vector2.Dot(point - a, b - a) / l2);
			var projection = a + t * (b - a);
			return (point - projection).magnitude;
		}
		
		/// <summary>
		/// Determines the distance from a mouse position to a screen space rectangle.
		/// </summary>
		/// <returns>0 if inside, otherwise the shortest distance to an edge.</returns>
		public static float DistanceToRectangle(Vector2[] screenPoints, Vector2 mousePos)
		{
			if (IsPointInPolygon(screenPoints, mousePos)) return 0f;

			float closestDist = -1f;
			for (int i = 0; i < 4; i++)
			{
				Vector3 v0 = screenPoints[i];
				Vector3 v1 = screenPoints[RepeatIndex(i + 1, 4)];
				var dist = DistancePointToLineSegment(mousePos, v0, v1);
				if (dist < closestDist || closestDist < 0f) closestDist = dist;
			}
			return closestDist;
		}
		
		/// <summary>
		/// Determines the distance from the mouse position to a world rectangle.
		/// </summary>
		public static float DistanceToRectangle(Vector3[] worldPoints, Vector2 mousePos, Camera cam)
		{
			var screenPoints = new Vector2[4];
			for (int i = 0; i < 4; ++i)
				screenPoints[i] = cam.WorldToScreenPoint(worldPoints[i]);
			return DistanceToRectangle(screenPoints, mousePos);
		}
		
		/// <summary>
		/// Determines if a point is inside a polygon using the ray-casting algorithm.
		/// </summary>
		private static bool IsPointInPolygon(Vector2[] polygon, Vector2 point)
		{
			bool isInside = false;
			for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
			{
				if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
				    (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
				{
					isInside = !isInside;
				}
			}
			return isInside;
		}

		/// <summary>
		/// Checks if a line segment intersects with a rectangle.
		/// </summary>
		public static bool CheckLineIntersect(Vector2 pLineStart, Vector2 pLineEnd, Rect pRect)
		{
			return LineLine(pLineStart.x, pLineStart.y, pLineEnd.x, pLineEnd.y, pRect.xMin, pRect.yMin, pRect.xMin, pRect.yMax) ||
			       LineLine(pLineStart.x, pLineStart.y, pLineEnd.x, pLineEnd.y, pRect.xMin, pRect.yMax, pRect.xMax, pRect.yMax) ||
			       LineLine(pLineStart.x, pLineStart.y, pLineEnd.x, pLineEnd.y, pRect.xMax, pRect.yMax, pRect.xMax, pRect.yMin) ||
			       LineLine(pLineStart.x, pLineStart.y, pLineEnd.x, pLineEnd.y, pRect.xMax, pRect.yMin, pRect.xMin, pRect.yMin);
		}
		
		/// <summary>
		/// Checks if two line segments intersect.
		/// </summary>
		public static bool LineLine(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
		{
			float den = ((x4 - x3) * (y2 - y1) - (x2 - x1) * (y4 - y3));
			if (Mathf.Approximately(den, 0)) return false;

			float t = ((x1 - x3) * (y4 - y3) - (y1 - y3) * (x4 - x3)) / den;
			float u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / den;

			return t >= 0 && t <= 1 && u >= 0 && u <= 1;
		}
		
		/// <summary>
		/// Checks if a point is inside an ellipse defined by its position and size.
		/// </summary>
		public static bool IsPointInsideEllipse(Vector2 point, Vector2 ellipsePos, Vector2 ellipseSize)
		{
			return IsPointInsideEllipse(point, ellipsePos, ellipseSize.x, ellipseSize.y);
		}
		
		public static bool IsPointInsideEllipse(Vector2 pointToCheck, Vector2 ellipsePos, float ellipseWidth, float ellipseHeight)
		{
			if (ellipseWidth <= 0 || ellipseHeight <= 0) return false;
			float dx = pointToCheck.x - ellipsePos.x;
			float dy = pointToCheck.y - ellipsePos.y;
			float value = (dx * dx) / (ellipseWidth * ellipseWidth) + (dy * dy) / (ellipseHeight * ellipseHeight);
			return value < 1;
		}
		
		private static int IntersectEllipseLine(Vector2 center, float rx, float ry, Vector2 p1, Vector2 p2)
		{
			var dir = p2 - p1;
			var diff = p1 - center;
			var mDir = new Vector2(dir.x / (rx * rx), dir.y / (ry * ry));
			var mDiff = new Vector2(diff.x / (rx * rx), diff.y / (ry * ry));
			
			float a = Vector2.Dot(dir, mDir);
			float b = 2 * Vector2.Dot(dir, mDiff);
			float c = Vector2.Dot(diff, mDiff) - 1.0f;
			float d = b * b - 4 * a * c;

			if (d < 0) return Intersection_OUT_SIDE;

			if (d > 0)
			{
				float root = Mathf.Sqrt(d);
				float t_a = (-b - root) / (2 * a);
				float t_b = (-b + root) / (2 * a);

				if ((t_a < 0 || t_a > 1) && (t_b < 0 || t_b > 1))
				{
					return (t_a < 0 && t_b < 0) || (t_a > 1 && t_b > 1) ? Intersection_OUT_SIDE : Intersection_IN_SIDE;
				}
				return Intersection_CUT;
			}
			
			float t = -b / (2 * a);
			return (t >= 0 && t <= 1) ? Intersection_CUT : Intersection_OUT_SIDE;
		}
		
		public static bool IntersectEllipseRectangle(Vector2 c, float rx, float ry, Rect pRect)
		{
			var tL = new Vector2(pRect.xMin, pRect.yMax);
			var tR = new Vector2(pRect.xMax, pRect.yMax);
			var bL = new Vector2(pRect.xMin, pRect.yMin);
			var bR = new Vector2(pRect.xMax, pRect.yMin);

			return IntersectEllipseLine(c, rx, ry, tL, tR) == Intersection_CUT ||
			       IntersectEllipseLine(c, rx, ry, tL, bL) == Intersection_CUT ||
			       IntersectEllipseLine(c, rx, ry, bL, bR) == Intersection_CUT ||
			       IntersectEllipseLine(c, rx, ry, bR, tR) == Intersection_CUT;
		}
		
		#endregion
		
		#region Grid Calculation
		
		/// <summary>
		/// Creates a list of node positions for a simple grid.
		/// </summary>
		public static List<Vector3> CalcGridNodes(Vector3 rootPos, int width, int length, float tileSize, bool pRootIsCenter = false)
		{
			var list = new List<Vector3>();
			Vector3 startOffset = pRootIsCenter ? new Vector3(-width * tileSize / 2f, 0, -length * tileSize / 2f) : Vector3.zero;

			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < length; j++)
				{
					var pos = rootPos + startOffset;
					pos.x += tileSize * i + tileSize / 2f;
					pos.z += tileSize * j + tileSize / 2f;
					list.Add(pos);
				}
			}
			return list;
		}

		#endregion

		#region Isometric Grid

		/// <summary>
		/// Calculates all isometric cell coordinates covered by a tile of a given size at a world position.
		/// </summary>
		public static Vector3Int[] CalcIsometricCells(Vector2 pPosition, Vector2Int pTileSize, int pFlip = 1)
		{
			return CalcIsometricCells(new Vector2(2, 1), pPosition, pTileSize, pFlip);
		}

		/// <summary>
		/// Calculates all isometric cell coordinates covered by a tile of a given size at a world position, with custom cell size.
		/// </summary>
		public static Vector3Int[] CalcIsometricCells(Vector2 pCellSize, Vector2 pPosition, Vector2Int pTileSize, int pFlip)
		{
			var tileSize = pTileSize;
			if (pFlip == -1) (tileSize.x, tileSize.y) = (tileSize.y, tileSize.x);

			float yOffset = Mathf.Min(tileSize.x, tileSize.y) / 2f;
			var rootPos = new Vector2(pPosition.x, pPosition.y - yOffset);

			var cells = new Vector3Int[tileSize.x * tileSize.y];
			float offsetX = pCellSize.x / 2f;
			float offsetY = pCellSize.y / 2f;
			int index = 0;
			for (int y = 0; y < tileSize.y; y++)
			{
				for (int x = 0; x < tileSize.x; x++)
				{
					var pos = new Vector2(x * offsetX - y * offsetX + rootPos.x, y * offsetY + x * offsetY + rootPos.y);
					cells[index++] = WorldToIsometricCell(pos, pCellSize.x, pCellSize.y);
				}
			}
			return cells;
		}
		
		/// <summary>
		/// Converts a world position to its corresponding isometric cell coordinate.
		/// </summary>
		public static Vector3Int WorldToIsometricCell(Vector3 worldPosition, float cellWidth = 2, float cellHeight = 1)
		{
			float isoX = worldPosition.x / cellWidth + worldPosition.y / cellHeight;
			float isoY = (worldPosition.x / cellWidth - worldPosition.y / cellHeight) * -1f;
			return new Vector3Int(Mathf.FloorToInt(isoX), Mathf.FloorToInt(isoY), 0);
		}

		/// <summary>
		/// Converts an isometric cell coordinate to its corresponding world position (center of the cell).
		/// </summary>
		public static Vector3 IsometricCellToWorld(Vector3Int cellPosition, float cellWidth = 2, float cellHeight = 1)
		{
			float worldX = (cellPosition.x - cellPosition.y) * cellWidth / 2;
			float worldY = (cellPosition.x + cellPosition.y) * cellHeight / 2;
			return new Vector3(worldX, worldY, 0);
		}
		
		/// <summary>
		/// Snaps a world position to the center of its containing isometric cell.
		/// </summary>
		public static Vector3 GetIsometricWorldPosition(Vector3 worldPosition, float cellWidth = 2, float cellHeight = 1)
		{
			var cell = WorldToIsometricCell(worldPosition, cellWidth, cellHeight);
			return IsometricCellToWorld(cell, cellWidth, cellHeight);
		}

		#endregion
		
		#region Numeric & General Math

		/// <summary>
		/// Rounds a float to a specified number of decimal places.
		/// </summary>
		public static float Round(float pValue, int pDecimal)
		{
			float pow = Mathf.Pow(10, pDecimal);
			return Mathf.Round(pValue * pow) / pow;
		}

		/// <summary>
		/// Rounds each component of a Vector2 to a specified number of decimal places.
		/// </summary>
		public static Vector2 Round(Vector2 pValue, int pDecimal)
		{
			float pow = Mathf.Pow(10, pDecimal);
			pValue.x = Mathf.Round(pValue.x * pow) / pow;
			pValue.y = Mathf.Round(pValue.y * pow) / pow;
			return pValue;
		}

		/// <summary>
		/// Rounds each component of a Vector3 to a specified number of decimal places.
		/// </summary>
		public static Vector3 Round(Vector3 pValue, int pDecimal)
		{
			float pow = Mathf.Pow(10, pDecimal);
			pValue.x = Mathf.Round(pValue.x * pow) / pow;
			pValue.y = Mathf.Round(pValue.y * pow) / pow;
			pValue.z = Mathf.Round(pValue.z * pow) / pow;
			return pValue;
		}
		
		/// <summary>
		/// Converts an integer to its binary string representation.
		/// </summary>
		public static string IntToBinary(int val, int bits)
		{
			return System.Convert.ToString(val, 2).PadLeft(bits, '0');
		}

		/// <summary>
		/// A Lerp function that is not clamped between 0 and 1.
		/// </summary>
		public static float Lerp(float from, float to, float factor) => from * (1f - factor) + to * factor;

		/// <summary>
		/// Clamps an index to be between 0 and max-1.
		/// </summary>
		public static int ClampIndex(int val, int max) => (val < 0) ? 0 : (val < max ? val : max - 1);

		/// <summary>
		/// Repeats an index within the range [0, max-1].
		/// </summary>
		public static int RepeatIndex(int val, int max)
		{
			if (max < 1) return 0;
			return (val % max + max) % max;
		}

		/// <summary>
		/// Converts a hexadecimal character to its decimal value.
		/// </summary>
		public static int HexToDecimal(char ch)
		{
			if (ch >= '0' && ch <= '9') return ch - '0';
			if (ch >= 'A' && ch <= 'F') return ch - 'A' + 10;
			if (ch >= 'a' && ch <= 'f') return ch - 'a' + 10;
			return -1; // Invalid character
		}

		/// <summary>
		/// Converts a single 0-15 value into its hex character representation.
		/// </summary>
		public static char DecimalToHexChar(int num)
		{
			if (num < 0 || num > 15) return '?';
			return num < 10 ? (char)('0' + num) : (char)('A' + num - 10);
		}
		
		public static string DecimalToHex8(int num) => (num & 0xFF).ToString("X2");
		public static string DecimalToHex24(int num) => (num & 0xFFFFFF).ToString("X6");
		public static string DecimalToHex32(int num) => num.ToString("X8");
		
		public static int Sum(params int[] pNumbers)
		{
			int sum = 0;
			for (int i = 0; i < pNumbers.Length; i++) sum += pNumbers[i];
			return sum;
		}
		
		public static int GCD(int a, int b)
		{
			while (b > 0) (a, b) = (b, a % b);
			return a;
		}
		
		public static int GCD(List<int> arr)
		{
			if (arr == null || arr.Count == 0) return 0;
			int result = arr[0];
			for (int i = 1; i < arr.Count; i++) result = GCD(result, arr[i]);
			return result;
		}

		public static int Factorial(int pVal)
		{
			if (pVal < 0) return 0; // Factorial is not defined for negative numbers
			int result = 1;
			for (int i = 2; i <= pVal; i++) result *= i;
			return result;
		}
		
		public static float CalcBaseValue(int pMaxLevel, float pTotalValue, float pValueGrow, float pSurplus = 0)
		{
			float total = pTotalValue * (1f - pSurplus / 100f);
			if (pMaxLevel <= 1) return total;
			
			float denominator = 1;
			for (int i = 1; i < pMaxLevel; i++)
				denominator += Mathf.Pow(1 + pValueGrow / 100f, i);
			
			return total / denominator;
		}

		public static float CalcCompoundingValue(float pBase, float pGrow, int pLevel)
		{
			if (pLevel <= 1) return pBase;
			return pBase * Mathf.Pow(1 + pGrow / 100f, pLevel - 1);
		}

		#endregion
	}

	/// <summary>
	/// A collection of extension methods for mathematical operations.
	/// </summary>
	public static class MathExtension
	{
		#region Numeric Extensions
		
		public static int Sign(this int x) => (x > 0) ? 1 : ((x < 0) ? -1 : 0);
		public static float Sign(this float x) => (x > 0f) ? 1f : ((x < 0f) ? -1f : 0f);
		public static string ToBinary(this int val, int bits) => MathHelper.IntToBinary(val, bits);

		#endregion

		#region Vector Extensions
		
		public static Vector3 Round(this Vector3 pVector, int pDecimal) => MathHelper.Round(pVector, pDecimal);

		public static Vector3 ToPositiveAngle(this Vector3 pAngle)
		{
			if (pAngle.x < 0) pAngle.x += 360f;
			if (pAngle.y < 0) pAngle.y += 360f;
			if (pAngle.z < 0) pAngle.z += 360f;
			return pAngle;
		}

		public static float ToPositiveAngle(this float pAngle)
		{
			return (pAngle % 360f + 360f) % 360f;
		}

		public static bool Approximately(this Vector3 pValue, Vector3 pRefValue)
		{
			return Mathf.Approximately(pValue.x, pRefValue.x) &&
			       Mathf.Approximately(pValue.y, pRefValue.y) &&
			       Mathf.Approximately(pValue.z, pRefValue.z);
		}
		
		public static Vector3 Sign(this Vector3 pVal)
		{
			return new Vector3(Mathf.Sign(pVal.x), Mathf.Sign(pVal.y), Mathf.Sign(pVal.z));
		}
		
		public static Vector3Int SetValue(this Vector3Int pVector, int pX, int pY, int pZ)
		{
			pVector.Set(pX, pY, pZ);
			return pVector;
		}

		#endregion

		#region Transform & Distance Extensions
		
		public static float DistanceTo<T>(this Transform pRoot, T pTarget) where T : Component
		{
			return Vector3.Distance(pRoot.position, pTarget.transform.position);
		}

		public static float DistanceTo(this Transform pRoot, Vector3 pTarget)
		{
			return Vector3.Distance(pRoot.position, pTarget);
		}
		
		public static float DistanceTo(this Vector3 pRoot, Vector3 pTarget)
		{
			return Vector3.Distance(pRoot, pTarget);
		}

		#endregion
	}
}