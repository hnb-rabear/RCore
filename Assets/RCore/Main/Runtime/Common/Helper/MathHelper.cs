/**
 * Author HNB-RaBear - 2018
 **/

using System.Collections.Generic;
using UnityEngine;

namespace RCore
{
	public static class MathHelper
	{
		/// <summary>
		/// Return the angle in degree of two vector
		/// </summary>
		/// <param name="dir1"></param>
		/// <param name="dir2"></param>
		/// <returns></returns>
		public static float CalcAngle(Vector3 dir1, Vector3 dir2)
		{
			//Unity way
			//return Vector3.Angle(Vector3 from, Vector3 to);

			//Tradition way
			float dot = Vector3.Dot(dir1, dir2);
			dot = dot / (dir1.magnitude * dir2.magnitude);
			var angleRadian = Mathf.Acos(dot);
			var angleDegree = angleRadian * 180 / Mathf.PI;
			return angleDegree;
		}
		public static float CalcAngle(Vector2 direction)
		{
			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			return angle;
		}
		public static Vector3 CalcAngle360(Vector3 fromDir, Vector3 toDir)
		{
			return Quaternion.FromToRotation(Vector3.forward, toDir - fromDir).eulerAngles;
		}
		public static float CalcAngle360X(Vector3 fromDir, Vector3 toDir)
		{
			return Quaternion.FromToRotation(Vector3.right, toDir - fromDir).eulerAngles.z;
		}
		public static float CalcAngle360Z(Vector3 fromDir, Vector3 toDir)
		{
			return Quaternion.FromToRotation(Vector3.up, toDir - fromDir).eulerAngles.z;
		}
		public static float CalcAngle360Y(Vector3 fromDir, Vector3 toDir)
		{
			return Quaternion.FromToRotation(Vector3.forward, toDir - fromDir).eulerAngles.y;
		}

		/// <summary>
		/// Returns a point randomly selected the on a sphere.
		/// </summary>
		/// <param name="radius">The radius of the sphere.</param>
		/// <returns></returns>
		// http://mathworld.wolfram.com/SpherePointPicking.html
		public static Vector3 RandomPointOnSphere(float radius)
		{
			var random = new System.Random();

			float u = (float)random.NextDouble();
			float v = (float)random.NextDouble();

			float theta = 2 * Mathf.PI * u;
			float phi = Mathf.Acos(2 * v - 1);

			float x = radius * Mathf.Cos(theta) * Mathf.Sin(phi);
			float y = radius * Mathf.Sin(theta) * Mathf.Sin(phi);
			float z = radius * Mathf.Cos(phi);

			return new Vector3(x, y, z);
		}

		public static Vector3 RandomPointOnCircleEdge_XZ(Vector3 pCenter, float pRadius)
		{
			var pos = UnityEngine.Random.insideUnitCircle.normalized * pRadius;
			return new Vector3(pCenter.x + pos.x, pCenter.y, pCenter.z + pos.y);
		}

		public static Vector3 RandomPointOnCircle_XZ(Vector3 pCenter, float pRadius)
		{
			var pos = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0, pRadius);
			return new Vector3(pCenter.x + pos.x, pCenter.y, pCenter.z + pos.y);
		}

		public static Vector3 RandomPointOnCircleEdge_XY(Vector2 pCenter, float pRadius)
		{
			var pos = UnityEngine.Random.insideUnitCircle.normalized * pRadius;
			return new Vector3(pCenter.x + pos.x, pCenter.y + pos.y);
		}

		/// <summary>
		/// To check if target is front of object
		/// true > 0
		/// false < 0
		/// </summary>
		public static float Dot(Transform pRoot, Transform pTarget)
		{
			var dir = pTarget.position - pRoot.position;
			float dot = Vector3.Dot(pRoot.forward, dir);
			return dot;
		}

		public static bool OnRightOrLeft(Transform pRoot, Vector3 pPosition)
		{
			var dir = pPosition - pRoot.position;
			float dot = Vector3.Dot(pRoot.right, dir);
			return dot > 0;
		}

		/// <summary>
		/// A utility function to calculate area of triangle formed by(x1, y1) (x2, y2) and(x3, y3)
		/// </summary>
		private static float Area(Vector2 a, Vector2 b, Vector2 c)
		{
			return Mathf.Abs((a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y)) / 2.0f);
		}

		/// <summary>
		/// A function to check whether point P(x, y) lies inside the triangle formed by A(x1, y1), B(x2, y2) and C(x3, y3)
		/// </summary>
		public static bool IsInside(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
		{
			/* Calculate area of triangle ABC */
			float A = Area(a, b, c);

			/* Calculate area of triangle PBC */
			float A1 = Area(p, b, c);

			/* Calculate area of triangle PAC */
			float A2 = Area(a, p, c);

			/* Calculate area of triangle PAB */
			float A3 = Area(a, b, p);

			/* Check if sum of A1, A2 and A3 is same as A */
			return A == A1 + A2 + A3;
		}

		public static Vector3 DirOfYAngle(float pAngleInDegrees)
		{
			//Tradition way
			return new Vector3(Mathf.Sin(pAngleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(pAngleInDegrees * Mathf.Deg2Rad));
			//Unity way
			//var rot = Quaternion.AngleAxis(pAngleInDegrees, Vector3.up);
			//var direction = rot * Vector3.forward;
			//return direction;
		}

		public static Vector3 DirOfXAngle(float pAngleInDegrees)
		{
			var rot = Quaternion.AngleAxis(pAngleInDegrees, Vector3.right);
			var direction = rot * Vector3.forward;
			return direction;
		}

		public static Vector3 DirOfZAngle(float pAngleInDegrees)
		{
			var rot = Quaternion.AngleAxis(pAngleInDegrees, Vector3.forward);
			var direction = rot * Vector3.forward;
			return direction;
		}

		public static Vector3 DirOfAngle(Vector3 pAngle)
		{
			var rot = Quaternion.Euler(pAngle);
			var direction = rot * Vector3.forward;
			return direction;
		}

		public static bool IsBetween(Vector3 from, Vector3 to, Vector3 mid)
		{
			var a = Vector2.zero;
			var b = Vector2.zero;
			var c = Vector2.zero;

			if (from.x == to.x && to.x == mid.x)
			{
				a.x = from.y;
				a.y = from.z;
				b.x = to.y;
				b.y = to.z;
				c.x = mid.y;
				c.y = mid.z;
			}
			else if (from.y == to.y && to.y == mid.y)
			{
				a.x = from.x;
				a.y = from.z;
				b.x = to.x;
				b.y = to.z;
				c.x = mid.x;
				c.y = mid.z;
			}
			else if (from.z == to.z && to.z == mid.z)
			{
				a.x = from.x;
				a.y = from.y;
				b.x = to.x;
				b.y = to.y;
				c.x = mid.x;
				c.y = mid.y;
			}
			else
				return false;
			return IsBetween(a, b, c);
		}

		/// <summary>
		/// Check if vector Mid is between vector From and vector To
		/// </summary>
		public static bool IsBetweenXZ(Vector3 from, Vector3 to, Vector3 mid)
		{
			return (from.z * mid.x - from.x * mid.z) * (to.z * mid.x - to.x * mid.z) < 0;
		}

		/// <summary>
		/// Check if vector Mid is between vector From and vector To
		/// </summary>
		public static bool IsBetween(Vector2 from, Vector2 to, Vector2 mid)
		{
			return (from.y * mid.x - from.x * mid.y) * (to.y * mid.x - to.x * mid.y) < 0;
		}

		/// <summary>
		/// Calculate position from distance and direction
		/// </summary>
		public static Vector3 CalcPosition(Vector3 pRootPos, float pDistance, Vector3 pDir)
		{
			return pRootPos + pDir.normalized * pDistance;
		}

		public static float Round(float pValue, int pDecimal)
		{
			float pow = Mathf.Pow(10, pDecimal);
			return Mathf.Round(pValue * pow) / pow;
		}

		public static Vector2 Round(Vector2 pValue, int pDecimal)
		{
			float pow = Mathf.Pow(10, pDecimal);
			pValue.x = Mathf.Round(pValue.x * pow) / pow;
			pValue.y = Mathf.Round(pValue.y * pow) / pow;
			return pValue;
		}

		public static Vector3 Round(Vector3 pValue, int pDecimal)
		{
			float pow = Mathf.Pow(10, pDecimal);
			pValue.x = Mathf.Round(pValue.x * pow) / pow;
			pValue.y = Mathf.Round(pValue.y * pow) / pow;
			pValue.z = Mathf.Round(pValue.z * pow) / pow;
			return pValue;
		}

		public static Vector3 LeftDirection(Vector3 pForward, Vector3 pUp)
		{
			return RightDirection(pForward, pUp) * -1;
		}

		public static Vector3 RightDirection(Vector3 pForward, Vector3 pUp)
		{
			return Vector3.Cross(pForward, pUp);
		}

		public static Vector3 LeftDirectionXZ(Vector3 pForward)
		{
			return RightDirectionXZ(pForward) * -1;
		}

		public static Vector3 RightDirectionXZ(Vector3 pForward)
		{
			return new Vector3(pForward.z, pForward.y, -pForward.x);
		}

		public static string IntToBinary(int val, int bits)
		{
			string final = "";

			for (int i = bits; i > 0;)
			{
				if (i == 8 || i == 16 || i == 24) final += " ";
				final += (val & 1 << --i) != 0 ? '1' : '0';
			}
			return final;
		}

		/// <summary>
		/// Lerp function that doesn't clamp the 'factor' in 0-1 range.
		/// </summary>
		public static float Lerp(float from, float to, float factor)
		{
			return from * (1f - factor) + to * factor;
		}

		/// <summary>
		/// Clamp the specified integer to be between 0 and below 'max'.
		/// </summary>
		public static int ClampIndex(int val, int max)
		{
			return val < 0 ? 0 : val < max ? val : max - 1;
		}

		/// <summary>
		/// Wrap the index using repeating logic, so that for example +1 past the end means index of '1'.
		/// </summary>
		public static int RepeatIndex(int val, int max)
		{
			if (max < 1) return 0;
			while (val < 0) val += max;
			while (val >= max) val -= max;
			return val;
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
		/// Convert a hexadecimal character to its decimal value.
		/// </summary>
		public static int HexToDecimal(char ch)
		{
			switch (ch)
			{
				case '0': return 0x0;
				case '1': return 0x1;
				case '2': return 0x2;
				case '3': return 0x3;
				case '4': return 0x4;
				case '5': return 0x5;
				case '6': return 0x6;
				case '7': return 0x7;
				case '8': return 0x8;
				case '9': return 0x9;
				case 'a':
				case 'A': return 0xA;
				case 'b':
				case 'B': return 0xB;
				case 'c':
				case 'C': return 0xC;
				case 'd':
				case 'D': return 0xD;
				case 'e':
				case 'E': return 0xE;
				case 'f':
				case 'F': return 0xF;
			}
			return 0xF;
		}

		/// <summary>
		/// Convert a single 0-15 value into its hex representation.
		/// It's coded because int.ToString(format) syntax doesn't seem to be supported by Unity's Flash. It just silently crashes.
		/// </summary>
		public static char DecimalToHexChar(int num)
		{
			if (num > 15) return 'F';
			if (num < 10) return (char)('0' + num);
			return (char)('A' + num - 10);
		}

		/// <summary>
		/// Convert a decimal value to its hex representation.
		/// </summary>
		public static string DecimalToHex8(int num)
		{
			num &= 0xFF;
			return num.ToString("X2");
		}

		/// <summary>
		/// Convert a decimal value to its hex representation.
		/// It's coded because num.ToString("X6") syntax doesn't seem to be supported by Unity's Flash. It just silently crashes.
		/// string.Format("{0,6:X}", num).Replace(' ', '0') doesn't work either. It returns the format string, not the formatted value.
		/// </summary>
		public static string DecimalToHex24(int num)
		{
			num &= 0xFFFFFF;
			return num.ToString("X6");
		}

		/// <summary>
		/// Convert a decimal value to its hex representation.
		/// It's coded because num.ToString("X6") syntax doesn't seem to be supported by Unity's Flash. It just silently crashes.
		/// string.Format("{0,6:X}", num).Replace(' ', '0') doesn't work either. It returns the format string, not the formatted value.
		/// </summary>
		public static string DecimalToHex32(int num)
		{
			return num.ToString("X8");
		}

		/// <summary>
		/// Determine the distance from the specified point to the line segment.
		/// </summary>
		private static float DistancePointToLineSegment(Vector2 point, Vector2 a, Vector2 b)
		{
			float l2 = (b - a).sqrMagnitude;
			if (l2 == 0f) return (point - a).magnitude;
			float t = Vector2.Dot(point - a, b - a) / l2;
			if (t < 0f) return (point - a).magnitude;
			else if (t > 1f) return (point - b).magnitude;
			var projection = a + t * (b - a);
			return (point - projection).magnitude;
		}

		/// <summary>
		/// Determine the distance from the mouse position to the screen space rectangle specified by the 4 points.
		/// </summary>
		public static float DistanceToRectangle(Vector2[] screenPoints, Vector2 mousePos)
		{
			bool oddNodes = false;
			int j = 4;

			for (int i = 0; i < 5; i++)
			{
				Vector3 v0 = screenPoints[RepeatIndex(i, 4)];
				Vector3 v1 = screenPoints[RepeatIndex(j, 4)];

				if (v0.y > mousePos.y != v1.y > mousePos.y)
				{
					if (mousePos.x < (v1.x - v0.x) * (mousePos.y - v0.y) / (v1.y - v0.y) + v0.x)
					{
						oddNodes = !oddNodes;
					}
				}
				j = i;
			}

			if (!oddNodes)
			{
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
			else return 0f;
		}

		/// <summary>
		/// Determine the distance from the mouse position to the world rectangle specified by the 4 points.
		/// </summary>
		public static float DistanceToRectangle(Vector3[] worldPoints, Vector2 mousePos, Camera cam)
		{
			var screenPoints = new Vector2[4];
			for (int i = 0; i < 4; ++i)
				screenPoints[i] = cam.WorldToScreenPoint(worldPoints[i]);
			return DistanceToRectangle(screenPoints, mousePos);
		}

		public static int VectorCompareXYZ(Vector3 value1, Vector3 value2)
		{
			if (value1.x < value2.x)
				return -1;
			else if (value1.x == value2.x)
			{
				if (value1.y < value2.y)
					return -1;
				else if (value1.y == value2.y)
				{
					if (value1.z < value2.z)
						return -1;
					else if (value1.z == value2.z)
						return 0;
					else
						return 1;
				}
				else
					return 1;
			}
			else
				return 1;
		}

		public static bool InsideAngle(float angle, float minAngle, float maxAngle)
		{
			if (Mathf.Cos(angle / 180f) < Mathf.Cos(minAngle / 180f)
			    || Mathf.Cos(angle / 180f) < Mathf.Cos(maxAngle / 180f))
				return false;
			return true;
		}

		public static Vector2 GetPosOnCircle(float pAngleDeg, float pRadius)
		{
			float x = CosDeg(pAngleDeg) * pRadius;
			float y = SinDeg(pAngleDeg) * pRadius;
			return new Vector2(x, y);
		}

		public static float SinRad(float pRadiant)
		{
			return Mathf.Sin(pRadiant);
		}
		public static float CosRad(float pRadiant)
		{
			return Mathf.Cos(pRadiant);
		}
		public static float SinDeg(float pDegree)
		{
			return Mathf.Sin(Ded2Rad(pDegree));
		}
		public static float CosDeg(float pDegree)
		{
			return Mathf.Cos(Ded2Rad(pDegree));
		}
		public static float TanDeg(float pDegree)
		{
			return Mathf.Tan(Ded2Rad(pDegree));
		}
		public static float Ded2Rad(float pDegree)
		{
			//Tradition way
			//pDegree *= Mathf.PI / 180;
			return pDegree * Mathf.Deg2Rad;
		}
		public static float Tad2Deg(float pRadiant)
		{
			return pRadiant * Mathf.Rad2Deg;
		}
		public static float AngleDeg(Vector2 pFrom, Vector2 pTo)
		{
			return AtanDeg(pTo.y - pFrom.y, pTo.x - pFrom.x);
		}
		public static float AtanDeg(float dy, float dx)
		{
			return Tad2Deg(AtanRad(dy, dx));
		}
		public static float AtanRad(float dy, float dx)
		{
			return Mathf.Atan2(dy, dx);
		}

		public static Vector3 GetPosOnCircle(Vector3 pRoot, float pAngleDeg, float pRadius)
		{
			var dir = DirOfYAngle(pAngleDeg);
			var pos = pRoot + dir * pRadius;
			return pos;
		}

		public static int GetRandomIndexFromChances(List<int> chances)
		{
			int index = -1;
			float totalRatios = 0;
			for (int i = 0; i < chances.Count; i++)
				totalRatios += chances[i];

			float random = UnityEngine.Random.Range(0, totalRatios);
			float temp2 = 0;
			for (int i = 0; i < chances.Count; i++)
			{
				temp2 += chances[i];
				if (temp2 > random)
				{
					index = i;
					break;
				}
			}
			return index;
		}

		public static int GetRandomIndexFromChances(params float[] chances)
		{
			int index = -1;
			float totalRatios = 0;
			for (int i = 0; i < chances.Length; i++)
				totalRatios += chances[i];

			float random = UnityEngine.Random.Range(0, totalRatios);
			float temp2 = 0;
			for (int i = 0; i < chances.Length; i++)
			{
				temp2 += chances[i];
				if (temp2 > random)
				{
					index = i;
					break;
				}
			}
			return index;
		}

		public static int GetRandomIndexFromChances(params int[] chances)
		{
			int index = -1;
			float totalRatios = 0;
			for (int i = 0; i < chances.Length; i++)
				totalRatios += chances[i];

			float random = UnityEngine.Random.Range(0, totalRatios);
			float temp2 = 0;
			for (int i = 0; i < chances.Length; i++)
			{
				temp2 += chances[i];
				if (temp2 > random)
				{
					index = i;
					break;
				}
			}
			return index;
		}

		public static int Sum(params int[] pNumbers)
		{
			int sum = 0;
			for (int i = 0; i < pNumbers.Length; i++)
				sum += pNumbers[i];
			return sum;
		}

		public static int GCD(int a, int b)
		{
			while (b > 0)
			{
				int temp = b;
				b = a % b;
				a = temp;
			}
			return a;
		}
		/// <summary>
		/// Find greatest common divisor
		/// </summary>
		public static int GCD(List<int> arr)
		{
			if (arr.Count == 1)
				return 1;
			int gcd = arr[0];
			for (int i = 1; i < arr.Count; i++)
				gcd = GCD(gcd, arr[i]);
			return gcd;
		}

		public static bool IsPointInsideEllipse(Vector2 pointToCheck, Vector2 ellipsePos, Vector2 ellipseSize)
		{
			return IsPointInsideEllipse(pointToCheck, ellipsePos, ellipseSize.x, ellipseSize.y);
		}
		public static bool IsPointInsideEllipse(Vector2 pointToCheck, Vector2 ellipsePos, float ellipseWidth, float ellipseHeight)
		{
			if (ellipseWidth <= 0 || ellipseHeight <= 0)
			{
				return false;
			}
			float dx = pointToCheck.x - ellipsePos.x;
			float dy = pointToCheck.y - ellipsePos.y;
			float xComponent = dx * dx / (ellipseWidth * ellipseWidth);
			float yComponent = dy * dy / (ellipseHeight * ellipseHeight);
			float value = xComponent + yComponent;

			if (value < 1)
			{
				return true;
			}
			return false;
		}
		public static bool CheckLineIntersect(Vector2 pLineStart, Vector2 pLineEnd, Rect pRect)
		{
			// check if the line has hit any of the rectangle's sides
			float x1 = pLineStart.x;
			float y1 = pLineStart.y;
			float x2 = pLineEnd.x;
			float y2 = pLineEnd.y;
			float rx = pRect.x;
			float ry = pRect.y;
			float rw = pRect.width;
			float rh = pRect.height;
			// uses the Line/Line function below
			bool left = LineLine(x1, y1, x2, y2, rx, ry, rx, ry + rh);
			bool right = LineLine(x1, y1, x2, y2, rx + rw, ry, rx + rw, ry + rh);
			bool top = LineLine(x1, y1, x2, y2, rx, ry, rx + rw, ry);
			bool bottom = LineLine(x1, y1, x2, y2, rx, ry + rh, rx + rw, ry + rh);

			// if ANY of the above are true, the line
			// has hit the rectangle
			if (left || right || top || bottom)
			{
				return true;
			}
			return false;
		}
		public static bool LineLine(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
		{
			// calculate the direction of the lines
			float uA = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));
			float uB = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));

			// if uA and uB are between 0-1, lines are colliding
			if (uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1)
			{
				return true;
			}
			return false;
		}
		private const int Intersection_OUT_SIDE = 0;
		private const int Intersection_IN_SIDE = 1;
		private const int Intersection_CUT = 2;
		public static int IntersectEllipseLine(Vector2 center, float rx, float ry, Vector2 a1, Vector2 a2)
		{
			int result;
			var origin = a1;
			var dir = a2 - a1;
			var diff = origin - center;
			var mDir = new Vector2(dir.x / (rx * rx), dir.y / (ry * ry));
			var mDiff = new Vector2(diff.x / (rx * rx), diff.y / (ry * ry));

			var a = Vector2.Dot(dir, mDir);
			var b = Vector2.Dot(dir, mDiff);
			var c = Vector2.Dot(diff, mDiff) - 1.0f;
			var d = b * b - a * c;

			if (d < 0)
			{
				result = Intersection_OUT_SIDE;
			}
			else if (d > 0)
			{
				var root = Mathf.Sqrt(d);
				var t_a = (-b - root) / a;
				var t_b = (-b + root) / a;

				if ((t_a < 0 || 1 < t_a) && (t_b < 0 || 1 < t_b))
				{
					if (t_a < 0 && t_b < 0 || t_a > 1 && t_b > 1)
						result = Intersection_OUT_SIDE;
					else
						result = Intersection_IN_SIDE;
				}
				else
				{
					result = Intersection_CUT;
				}
			}
			else
			{
				var t = -b / a;
				if (0 <= t && t <= 1)
				{
					result = Intersection_CUT;
				}
				else
				{
					result = Intersection_OUT_SIDE;
				}
			}

			return result;
		}
		public static bool IntersectEllipseRectangle(Vector2 c, float rx, float ry, Rect pRect)
		{
			var tL = new Vector2(pRect.x, pRect.y + pRect.height);
			var tR = new Vector2(pRect.x + pRect.width, pRect.y + pRect.height);
			var bL = new Vector2(pRect.x, pRect.y);
			var bR = new Vector2(pRect.x + pRect.width, pRect.y);
			return IntersectEllipseLine(c, rx, ry, tL, tR) == Intersection_CUT
				|| IntersectEllipseLine(c, rx, ry, tL, bL) == Intersection_CUT
				|| IntersectEllipseLine(c, rx, ry, bL, bR) == Intersection_CUT
				|| IntersectEllipseLine(c, rx, ry, bR, tR) == Intersection_CUT;
		}
		public static bool RectInsideEllipse(Vector2 c, float rx, float ry, Rect pRect)
		{
			var tL = new Vector2(pRect.x, pRect.y + pRect.height);
			var tR = new Vector2(pRect.x + pRect.width, pRect.y + pRect.height);
			var bL = new Vector2(pRect.x, pRect.y);
			var bR = new Vector2(pRect.x + pRect.width, pRect.y);
			return IntersectEllipseLine(c, rx, ry, tL, tR) == Intersection_IN_SIDE
				&& IntersectEllipseLine(c, rx, ry, tL, bL) == Intersection_IN_SIDE
				&& IntersectEllipseLine(c, rx, ry, bL, bR) == Intersection_IN_SIDE
				&& IntersectEllipseLine(c, rx, ry, bR, tR) == Intersection_IN_SIDE;
		}
		private static bool InAngle(Vector2 v1, Vector2 v2, float pFromDeg, float pToDeg)
		{
			float deg = AngleDeg(v1, v2);
			return deg >= pFromDeg && deg <= pToDeg || deg + 360 >= pFromDeg && deg + 360 <= pToDeg;
		}
		public static bool IntersectOrOverlapEllipseRectangle(Vector2 c, float rx, float ry, Rect pRect, float pFromAngle = 0, float pToAngle = 360, bool pRightSide = true, bool drawBox = false)
		{
			var tL = new Vector2(pRect.x, pRect.y + pRect.height);
			var tR = new Vector2(pRect.x + pRect.width, pRect.y + pRect.height);
			var bL = new Vector2(pRect.x, pRect.y);
			var bR = new Vector2(pRect.x + pRect.width, pRect.y);

			float a1 = pRightSide ? pFromAngle : pFromAngle + 180;
			float a2 = pRightSide ? pToAngle : pToAngle + 180;

			pFromAngle = a1;
			pToAngle = a2;

			var result = true;
			bool inAngleRange = true;
			if (pToAngle - pFromAngle != 360)
			{
				// check if any angle from center to rect point in angle range
				inAngleRange = (pRightSide ? pRect.xMax > c.x : pRect.xMin < c.x)
					&& (InAngle(c, tL, pFromAngle, pToAngle)
						|| InAngle(c, tR, pFromAngle, pToAngle)
						|| InAngle(c, bL, pFromAngle, pToAngle)
						|| InAngle(c, bR, pFromAngle, pToAngle));
				if (inAngleRange == false)
				{
					result = inAngleRange;
				}
			}
			if (inAngleRange)
			{
				var r1 = IntersectEllipseLine(c, rx, ry, tL, tR);
				if (r1 != Intersection_CUT)
				{
					var r2 = IntersectEllipseLine(c, rx, ry, tL, bL);
					if (r2 != Intersection_CUT)
					{
						var r3 = IntersectEllipseLine(c, rx, ry, bL, bR);
						if (r3 != Intersection_CUT)
						{
							var r4 = IntersectEllipseLine(c, rx, ry, bR, tR);
							if (r4 != Intersection_CUT)
							{
								result = r1 == Intersection_IN_SIDE
									|| r2 == Intersection_IN_SIDE
									|| r3 == Intersection_IN_SIDE
									|| r4 == Intersection_IN_SIDE;
							}
						}
					}
				}
			}
#if UNITY_EDITOR
			if (drawBox)
			{
				var resultColor = result ? Color.green : Color.red;
				if (inAngleRange == false)
					resultColor = Color.yellow;

				DebugDraw.DrawLine(tL, tR, resultColor, 0);
				DebugDraw.DrawLine(tR, bR, resultColor, 0);
				DebugDraw.DrawLine(bR, bL, resultColor, 0);
				DebugDraw.DrawLine(bL, tL, resultColor, 0);
				
				float x = c.x + rx * CosDeg(a1);
				float y = c.y + ry * SinDeg(a1);
				DebugDraw.DrawLine(c, new Vector3(x, y), Color.red, 0);
				x = c.x + rx * CosDeg(a2);
				y = c.y + ry * SinDeg(a2);
				DebugDraw.DrawLine(c, new Vector3(x, y), Color.blue, 0);
				DebugDraw.DrawEllipse(c, rx, ry, Color.red, 0);
			}
#endif
			return result;
		}

		public static int Factorial(int pVal)
		{
			int result = 1;
			for (int i = 1; i <= pVal; i++)
				result *= i;
			return result;
		}

		//Value[1] = BaseValue
		//Value[N] = Value[N-1] + (1 * ValueGrow / 100)
		public static float CalcBaseValue(int pMaxLevel, float pTotalValue, float pValueGrow, float pSurplus = 0)
		{
			float total = pTotalValue * (1f - pSurplus / 100f);
			float pow = 0;
			for (int i = 1; i < pMaxLevel; i++)
				pow += Mathf.Pow(1 + pValueGrow / 100f, i);
			float baseUpgradeCost = total / (1 + pow);
			return baseUpgradeCost;
		}

		public static float CalcCompoundingValue(float pBase, float pGrow, int pLevel)
		{
			float cost = 0;
			for (int i = 1; i <= pLevel; i++)
			{
				if (i == 1)
					cost = pBase;
				else
					cost *= 1 + pGrow / 100f;
			}
			return cost;
		}

		public static Vector3Int[] CalcIsometricCells(Vector2 pPosition, Vector2Int pTileSize, int pFlip = 1)
		{
			return CalcIsometricCells(new Vector3(2, 1), pPosition, pTileSize, pFlip);
		}

		public static Vector3Int[] CalcIsometricCells(Vector2 pCellSize, Vector2 pPosition, Vector2Int pTileSize, int pFlip)
		{
			var tileSize = pTileSize;
			if (pFlip == -1)
			{
				var temp = tileSize.x;
				tileSize.x = tileSize.y;
				tileSize.y = temp;
			}
			var yOffset = Mathf.Min(tileSize.x, tileSize.y) / 2f;
			var rootPos = pPosition.AddY(-yOffset);

			var cells = new Vector3Int[tileSize.x * tileSize.y];
			var offsetX = pCellSize.x / 2f;
			var offsetY = pCellSize.y / 2f;
			int index = 0;
			for (int y = 0; y < tileSize.y; y++)
			{
				for (int x = 0; x < tileSize.x; x++)
				{
					var pos = new Vector2(x * offsetX - y * offsetX, y * offsetY + x * offsetY);
					pos.x += rootPos.x;
					pos.y += rootPos.y;
					cells[index] = WorldToIsometricCell(pos, pCellSize.x, pCellSize.y);
					index++;
				}
			}
			return cells;
		}

		public static Vector3Int WorldToIsometricCell(Vector3 worldPosition, float cellWidth = 2, float cellHeight = 1)
		{
			return WorldToIsometricCell(worldPosition.x, worldPosition.y, cellWidth, cellHeight);
		}

		public static Vector3Int WorldToIsometricCell(float worldPosX, float worldPosY, float cellWidth = 2, float cellHeight = 1)
		{
			float isoX = worldPosX / cellWidth + worldPosY / cellHeight;
			float isoY = (worldPosX / cellWidth - worldPosY / cellHeight) * -1f;
			return new Vector3Int(Mathf.FloorToInt(isoX), Mathf.FloorToInt(isoY), 0);
		}

		public static Vector3 IsometricCellToWorld(Vector3Int cellPosition, float cellWidth = 2, float cellHeight = 1)
		{
			return IsometricCellToWorld(cellPosition.x, cellPosition.y, cellWidth, cellHeight);
		}

		public static Vector3 IsometricCellToWorld(int cellX, int cellY, float cellWidth = 2, float cellHeight = 1)
		{
			float worldX = (cellX - cellY) * cellWidth / 2;
			float worldY = (cellX + cellY) * cellHeight / 2;
			return new Vector3(worldX, worldY, 0);
		}

		public static Vector3 GetIsometricWorldPosition(Vector3 worldPosition, float cellWidth = 2, float cellHeight = 1)
		{
			var cell = WorldToIsometricCell(worldPosition, cellWidth, cellHeight);
			var pos = IsometricCellToWorld(cell);
			return pos;
		}

		public static Vector3 GetCenterVector(Vector3[] vectors)
		{
			var sum = Vector3.zero;
			if (vectors == null || vectors.Length == 0)
				return sum;
			foreach (var vec in vectors)
				sum += vec;
			return sum / vectors.Length;
		}
		
		public static Vector3 GetCenterVector(List<Vector3> vectors)
		{
			var sum = Vector3.zero;
			if (vectors == null || vectors.Count == 0)
				return sum;
			foreach (var vec in vectors)
				sum += vec;
			return sum / vectors.Count;
		}

		public static Vector3 GetCenterVector(List<Vector3Int> vectors)
		{
			var sum = Vector3.zero;
			if (vectors == null || vectors.Count == 0)
				return sum;
			foreach (Vector3 vec in vectors)
				sum += vec;
			return sum / vectors.Count;
		}
        
        public static List<Vector3> CalcGridNodes(Vector3 rootPos, int width, int length, float tileSize, bool pRootIsCenter = false)
        {
            var list = new List<Vector3>();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    var pos = rootPos;
                    pos.x += tileSize * i + tileSize / 2f;
                    pos.z += tileSize * j + tileSize / 2f;
                    if (pRootIsCenter)
                    {
                        pos.x -= width * tileSize / 2f;
                        pos.z -= length * tileSize / 2f;
                    }
                    list.Add(pos);
                }
            }
            return list;
        }
    }

	public static class MathExtension
	{
		public static int Sign(this int x)
		{
			if (x > 0) return 1;
			if (x < 0) return -1;

			return 0;
		}

		public static float Sign(this float x)
		{
			if (x > 0) return 1;
			if (x < 0) return -1;

			return 0;
		}

		public static Vector3 Round(this Vector3 pVector, int pDecimal)
		{
			float pow = Mathf.Pow(10, pDecimal);
			var round = new Vector3(Mathf.Round(pVector.x * pow),
				Mathf.Round(pVector.y * pow),
				Mathf.Round(pVector.z * pow));
			return round / pow;
		}

		public static string ToBinary(this int val, int bits)
		{
			string final = "";

			for (int i = bits; i > 0;)
			{
				if (i == 8 || i == 16 || i == 24) final += " ";
				final += (val & 1 << --i) != 0 ? '1' : '0';
			}
			return final;
		}

		/// <summary>
		/// Convert negative angle to positive
		/// </summary>
		public static Vector3 ToPositiveAngle(this Vector3 pAngle)
		{
			while (pAngle.x < 0)
				pAngle.x += 360f;
			while (pAngle.y < 0)
				pAngle.y += 360f;
			while (pAngle.z < 0)
				pAngle.z += 360f;
			return pAngle;
		}

		/// <summary>
		/// Convert negative angle to positive
		/// </summary>
		public static float ToPositiveAngle(this float pAngle)
		{
			while (pAngle < 0)
				pAngle += 360f;
			return pAngle;
		}

		public static float DistanceTo<T>(this Transform pRoot, T pTarget) where T : Component
		{
			return Vector3.Distance(pRoot.position, pTarget.transform.position);
			//Because of floating point when doing Mathf.Sqrt, these below method is heavier than Unity one
			//var distanceSqrd = (pRoot.position - pTarget.transform.position).sqrMagnitude;
			//return Mathf.Sqrt(distanceSqrd);
		}

		public static float DistanceTo(this Transform pRoot, Vector3 pTarget)
		{
			return Vector3.Distance(pRoot.position, pTarget);
		}

		public static float DistanceTo(this Vector3 pRoot, Vector3 pTarget)
		{
			return Vector3.Distance(pRoot, pTarget);
		}

		public static bool Approximately(this Vector3 pValue, Vector3 pRefValue)
		{
			bool ax = Mathf.Approximately(pValue.x, pRefValue.x);
			bool ay = Mathf.Approximately(pValue.y, pRefValue.y);
			bool az = Mathf.Approximately(pValue.z, pRefValue.z);
			return ax && ay && az;
		}

		private static Vector3 RoundVector(this Vector3 pVector, int pDecimal)
		{
			pVector.Set(MathHelper.Round(pVector.x, pDecimal),
				MathHelper.Round(pVector.y, pDecimal),
				MathHelper.Round(pVector.z, pDecimal));
			return pVector;
		}

		public static Vector3 Sign(this Vector3 pVal)
		{
			return new Vector3(Mathf.Sign(pVal.x), Mathf.Sign(pVal.y), Mathf.Sign(pVal.z));
		}

		public static Vector3Int SetValue(this Vector3Int pVector, int pX, int pY, int pZ)
		{
			pVector.x = pX;
			pVector.y = pY;
			pVector.z = pZ;
			return pVector;
		}
	}
}