using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RevCore
{
	/// <summary>
	/// Static math helpers — angles, directions, weighted random, grid placement, rounding,
	/// hex conversion, simple game-economy formulas. All members are pure functions unless
	/// noted; no shared mutable state.
	/// </summary>
	public static class MathHelper
	{
		/// <summary>Returns the unsigned angle between two directions in degrees (0..180). Robust against direction vectors of any magnitude.</summary>
		public static float CalcAngle(Vector3 dir1, Vector3 dir2)
		{
			float dot = Vector3.Dot(dir1.normalized, dir2.normalized);
			dot = Mathf.Clamp(dot, -1f, 1f);
			return Mathf.Acos(dot) * Mathf.Rad2Deg;
		}

		/// <summary>Returns the angle of a 2D direction vector from the +X axis in degrees (–180..180).</summary>
		public static float CalcAngle(Vector2 direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

		/// <summary>Returns the Euler angles representing the rotation from forward to <c>(toDir - fromDir)</c>.</summary>
		public static Vector3 CalcAngle360(Vector3 fromDir, Vector3 toDir) => Quaternion.FromToRotation(Vector3.forward, toDir - fromDir).eulerAngles;

		/// <summary>The Z-axis component of the rotation from +X (Vector3.right) to <c>(toDir - fromDir)</c>.</summary>
		public static float CalcAngle360X(Vector3 fromDir, Vector3 toDir) => Quaternion.FromToRotation(Vector3.right, toDir - fromDir).eulerAngles.z;

		/// <summary>The Z-axis component of the rotation from +Y (Vector3.up) to <c>(toDir - fromDir)</c>.</summary>
		public static float CalcAngle360Z(Vector3 fromDir, Vector3 toDir) => Quaternion.FromToRotation(Vector3.up, toDir - fromDir).eulerAngles.z;

		/// <summary>The Y-axis component of the rotation from +Z (Vector3.forward) to <c>(toDir - fromDir)</c>.</summary>
		public static float CalcAngle360Y(Vector3 fromDir, Vector3 toDir) => Quaternion.FromToRotation(Vector3.forward, toDir - fromDir).eulerAngles.y;

		/// <summary>Wraps an angle in degrees into the range (–180, 180].</summary>
		public static float WrapAngle(float angle)
		{
			angle %= 360;
			if (angle > 180) return angle - 360;
			if (angle < -180) return angle + 360;
			return angle;
		}

		/// <summary>
		/// Returns <c>true</c> if <paramref name="angle"/> falls inside the inclusive arc from
		/// <paramref name="minAngle"/> to <paramref name="maxAngle"/>. Handles arcs that wrap past 360.
		/// </summary>
		public static bool InsideAngle(float angle, float minAngle, float maxAngle)
		{
			float normalizedAngle = (angle % 360 + 360) % 360;
			float normalizedMin = (minAngle % 360 + 360) % 360;
			float normalizedMax = (maxAngle % 360 + 360) % 360;
			return normalizedMin <= normalizedMax
				? normalizedAngle >= normalizedMin && normalizedAngle <= normalizedMax
				: normalizedAngle >= normalizedMin || normalizedAngle <= normalizedMax;
		}

		/// <summary>Returns the unit XZ direction corresponding to a Y-axis rotation of <paramref name="angleDegrees"/>.</summary>
		public static Vector3 DirOfYAngle(float angleDegrees)
		{
			float rad = angleDegrees * Mathf.Deg2Rad;
			return new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
		}

		/// <summary>Returns a direction obtained by rotating <see cref="Vector3.forward"/> around +X by <paramref name="angleDegrees"/>.</summary>
		public static Vector3 DirOfXAngle(float angleDegrees) => Quaternion.AngleAxis(angleDegrees, Vector3.right) * Vector3.forward;

		/// <summary>Returns a direction obtained by rotating <see cref="Vector3.up"/> around +Z by <paramref name="angleDegrees"/>.</summary>
		public static Vector3 DirOfZAngle(float angleDegrees) => Quaternion.AngleAxis(angleDegrees, Vector3.forward) * Vector3.up;

		/// <summary>Forward direction after applying the Euler rotation <paramref name="angle"/>.</summary>
		public static Vector3 DirOfAngle(Vector3 angle) => Quaternion.Euler(angle) * Vector3.forward;

		/// <summary>Alias for <see cref="Mathf.Sin"/>. Argument is in radians.</summary>
		public static float SinRad(float radian) => Mathf.Sin(radian);
		/// <summary>Alias for <see cref="Mathf.Cos"/>. Argument is in radians.</summary>
		public static float CosRad(float radian) => Mathf.Cos(radian);
		/// <summary>Sine of <paramref name="degree"/> (degrees).</summary>
		public static float SinDeg(float degree) => Mathf.Sin(degree * Mathf.Deg2Rad);
		/// <summary>Cosine of <paramref name="degree"/> (degrees).</summary>
		public static float CosDeg(float degree) => Mathf.Cos(degree * Mathf.Deg2Rad);
		/// <summary>Tangent of <paramref name="degree"/> (degrees).</summary>
		public static float TanDeg(float degree) => Mathf.Tan(degree * Mathf.Deg2Rad);
		/// <summary>Converts degrees to radians.</summary>
		public static float Deg2Rad(float degree) => degree * Mathf.Deg2Rad;
		/// <summary>Converts radians to degrees.</summary>
		public static float Rad2Deg(float radian) => radian * Mathf.Rad2Deg;
		/// <summary>Angle in degrees from point <paramref name="from"/> to point <paramref name="to"/>, measured from +X axis.</summary>
		public static float AngleDeg(Vector2 from, Vector2 to) => AtanDeg(to.y - from.y, to.x - from.x);
		/// <summary><see cref="Mathf.Atan2"/> in degrees.</summary>
		public static float AtanDeg(float dy, float dx) => Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
		/// <summary>Alias for <see cref="Mathf.Atan2"/>.</summary>
		public static float AtanRad(float dy, float dx) => Mathf.Atan2(dy, dx);

		/// <summary>Returns the dot product of <paramref name="root"/>'s forward direction and the unit vector toward <paramref name="target"/>. 1 = facing exactly toward, -1 = exactly away.</summary>
		public static float Dot(Transform root, Transform target) => Vector3.Dot(root.forward, (target.position - root.position).normalized);

		/// <summary>Returns <c>true</c> when <paramref name="position"/> is on the +X (right) side of <paramref name="root"/>, <c>false</c> on the –X side.</summary>
		public static bool OnRightOrLeft(Transform root, Vector3 position) => Vector3.Dot(root.right, position - root.position) > 0;

		/// <summary>
		/// Returns <c>true</c> if point <paramref name="p"/> is inside (or on the edge of) triangle
		/// <paramref name="a"/>-<paramref name="b"/>-<paramref name="c"/>. Uses barycentric coordinates.
		/// Returns <c>false</c> for degenerate (collinear) triangles.
		/// </summary>
		public static bool IsInside(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
		{
			float denom = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
			if (Mathf.Abs(denom) < Mathf.Epsilon) return false;
			float w1 = ((b.y - c.y) * (p.x - c.x) + (c.x - b.x) * (p.y - c.y)) / denom;
			float w2 = ((c.y - a.y) * (p.x - c.x) + (a.x - c.x) * (p.y - c.y)) / denom;
			float w3 = 1f - w1 - w2;
			return w1 >= 0 && w2 >= 0 && w3 >= 0;
		}

		/// <summary>Returns <c>true</c> when <paramref name="mid"/> lies on the line segment between <paramref name="from"/> and <paramref name="to"/> (not on the extension).</summary>
		public static bool IsBetween(Vector3 from, Vector3 to, Vector3 mid) => Vector3.Dot(mid - from, to - from) > 0 && Vector3.Dot(mid - to, from - to) > 0;
		/// <summary>2D-in-XZ-plane variant of <see cref="IsBetween(Vector3,Vector3,Vector3)"/>; ignores Y.</summary>
		public static bool IsBetweenXZ(Vector3 from, Vector3 to, Vector3 mid) => (from.z * mid.x - from.x * mid.z) * (to.z * mid.x - to.x * mid.z) < 0;
		/// <summary>2D variant: returns <c>true</c> when <paramref name="mid"/> is on the segment from <paramref name="from"/> to <paramref name="to"/>.</summary>
		public static bool IsBetween(Vector2 from, Vector2 to, Vector2 mid) => (from.y * mid.x - from.x * mid.y) * (to.y * mid.x - to.x * mid.y) < 0;
		/// <summary>Returns <c>rootPos + dir.normalized * distance</c>.</summary>
		public static Vector3 CalcPosition(Vector3 rootPos, float distance, Vector3 dir) => rootPos + dir.normalized * distance;
		/// <summary>Returns the left-hand cross product (Cross(up, forward)).</summary>
		public static Vector3 LeftDirection(Vector3 forward, Vector3 up) => Vector3.Cross(up, forward);
		/// <summary>Returns the right-hand cross product (Cross(forward, up)).</summary>
		public static Vector3 RightDirection(Vector3 forward, Vector3 up) => Vector3.Cross(forward, up);
		/// <summary>XZ-plane left direction: 90° counter-clockwise from forward.</summary>
		public static Vector3 LeftDirectionXZ(Vector3 forward) => new(-forward.z, forward.y, forward.x);
		/// <summary>XZ-plane right direction: 90° clockwise from forward.</summary>
		public static Vector3 RightDirectionXZ(Vector3 forward) => new(forward.z, forward.y, -forward.x);

		/// <summary>Returns the arithmetic mean of all points in <paramref name="vectors"/>, or <see cref="Vector3.zero"/> for null/empty input.</summary>
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
			return count == 0 ? Vector3.zero : sum / count;
		}

		/// <summary>Random point on the circumference (edge) of a circle in the XZ plane.</summary>
		public static Vector3 RandomPointOnCircleEdge_XZ(Vector3 center, float radius)
		{
			Vector2 pos = Random.insideUnitCircle.normalized * radius;
			return new Vector3(center.x + pos.x, center.y, center.z + pos.y);
		}

		/// <summary>Random point inside a disc in the XZ plane.</summary>
		public static Vector3 RandomPointOnCircle_XZ(Vector3 center, float radius)
		{
			Vector2 pos = Random.insideUnitCircle * radius;
			return new Vector3(center.x + pos.x, center.y, center.z + pos.y);
		}

		/// <summary>Random point on the circumference of a circle in the XY plane (Z = 0).</summary>
		public static Vector3 RandomPointOnCircleEdge_XY(Vector2 center, float radius)
		{
			Vector2 pos = Random.insideUnitCircle.normalized * radius;
			return new Vector3(center.x + pos.x, center.y + pos.y, 0);
		}

		/// <summary>2D point on a unit-radius circle at the given polar angle.</summary>
		public static Vector2 GetPosOnCircle(float angleDeg, float radius) => new(CosDeg(angleDeg) * radius, SinDeg(angleDeg) * radius);
		/// <summary>3D point on an XZ-plane circle centered at <paramref name="root"/>.</summary>
		public static Vector3 GetPosOnCircle(Vector3 root, float angleDeg, float radius) => root + DirOfYAngle(angleDeg) * radius;

		/// <summary>List-overload of <see cref="GetRandomIndexFromChances(float[])"/>.</summary>
		public static int GetRandomIndexFromChances(List<int> chances) => GetRandomIndexFromChances(chances.ToArray());

		/// <summary>
		/// Returns an index sampled from the weighted distribution <paramref name="chances"/>.
		/// Chance values do not need to sum to 1 — they are normalized internally.
		/// </summary>
		public static int GetRandomIndexFromChances(params float[] chances)
		{
			float total = 0;
			for (int i = 0; i < chances.Length; i++) total += chances[i];
			float random = Random.Range(0, total);
			float cursor = 0;
			for (int i = 0; i < chances.Length; i++)
			{
				cursor += chances[i];
				if (cursor > random) return i;
			}
			return chances.Length - 1;
		}

		/// <summary>Integer overload of <see cref="GetRandomIndexFromChances(float[])"/>.</summary>
		public static int GetRandomIndexFromChances(params int[] chances)
		{
			int total = 0;
			for (int i = 0; i < chances.Length; i++) total += chances[i];
			float random = Random.Range(0, total);
			float cursor = 0;
			for (int i = 0; i < chances.Length; i++)
			{
				cursor += chances[i];
				if (cursor > random) return i;
			}
			return chances.Length - 1;
		}

		/// <summary>
		/// Returns world positions for the center of each cell in a <paramref name="width"/>×<paramref name="length"/>
		/// grid of <paramref name="tileSize"/>-sized cells, anchored either at <paramref name="rootPos"/> (corner)
		/// or centered on it.
		/// </summary>
		public static List<Vector3> CalcGridNodes(Vector3 rootPos, int width, int length, float tileSize, bool rootIsCenter = false)
		{
			var list = new List<Vector3>();
			Vector3 startOffset = rootIsCenter ? new Vector3(-width * tileSize / 2f, 0, -length * tileSize / 2f) : Vector3.zero;
			for (int i = 0; i < width; i++)
				for (int j = 0; j < length; j++)
					list.Add(rootPos + startOffset + new Vector3(tileSize * i + tileSize / 2f, 0, tileSize * j + tileSize / 2f));
			return list;
		}

		/// <summary>Rounds to <paramref name="decimalPlaces"/> decimal places using banker's rounding via <see cref="Mathf.Round"/>.</summary>
		public static float Round(float value, int decimalPlaces)
		{
			float pow = Mathf.Pow(10, decimalPlaces);
			return Mathf.Round(value * pow) / pow;
		}

		/// <summary>Component-wise round to <paramref name="decimalPlaces"/> decimal places.</summary>
		public static Vector2 Round(Vector2 value, int decimalPlaces)
		{
			float pow = Mathf.Pow(10, decimalPlaces);
			value.x = Mathf.Round(value.x * pow) / pow;
			value.y = Mathf.Round(value.y * pow) / pow;
			return value;
		}

		/// <summary>Component-wise round to <paramref name="decimalPlaces"/> decimal places.</summary>
		public static Vector3 Round(Vector3 value, int decimalPlaces)
		{
			float pow = Mathf.Pow(10, decimalPlaces);
			value.x = Mathf.Round(value.x * pow) / pow;
			value.y = Mathf.Round(value.y * pow) / pow;
			value.z = Mathf.Round(value.z * pow) / pow;
			return value;
		}

		/// <summary>Formats <paramref name="val"/> as a binary string padded to <paramref name="bits"/> characters.</summary>
		public static string IntToBinary(int val, int bits) => System.Convert.ToString(val, 2).PadLeft(bits, '0');
		/// <summary>Linear interpolation between <paramref name="from"/> and <paramref name="to"/> by <paramref name="factor"/> (unclamped).</summary>
		public static float Lerp(float from, float to, float factor) => from * (1f - factor) + to * factor;
		/// <summary>Clamps <paramref name="val"/> to the index range <c>[0, max-1]</c>.</summary>
		public static int ClampIndex(int val, int max) => val < 0 ? 0 : val < max ? val : max - 1;
		/// <summary>Maps <paramref name="val"/> onto <c>[0, max-1]</c> with wrap-around. Negative inputs wrap from the top.</summary>
		public static int RepeatIndex(int val, int max) => max < 1 ? 0 : (val % max + max) % max;
		/// <summary>Returns the numeric value of a hex digit (0–15) for the given character, or -1 if not a hex digit.</summary>
		public static int HexToDecimal(char ch) => ch >= '0' && ch <= '9' ? ch - '0' : ch >= 'A' && ch <= 'F' ? ch - 'A' + 10 : ch >= 'a' && ch <= 'f' ? ch - 'a' + 10 : -1;
		/// <summary>Returns the hex character (0–9, A–F) for a value 0–15, or <c>?</c> when out of range.</summary>
		public static char DecimalToHexChar(int num) => num < 0 || num > 15 ? '?' : num < 10 ? (char)('0' + num) : (char)('A' + num - 10);
		/// <summary>Renders the low 8 bits of <paramref name="num"/> as a two-character hex string.</summary>
		public static string DecimalToHex8(int num) => (num & 0xFF).ToString("X2");
		/// <summary>Renders the low 24 bits of <paramref name="num"/> as a six-character hex string.</summary>
		public static string DecimalToHex24(int num) => (num & 0xFFFFFF).ToString("X6");
		/// <summary>Renders the full 32 bits of <paramref name="num"/> as an eight-character hex string.</summary>
		public static string DecimalToHex32(int num) => num.ToString("X8");
		/// <summary>Sums the elements of <paramref name="numbers"/>.</summary>
		public static int Sum(params int[] numbers) { int sum = 0; for (int i = 0; i < numbers.Length; i++) sum += numbers[i]; return sum; }
		/// <summary>Greatest common divisor of <paramref name="a"/> and <paramref name="b"/> (Euclid's algorithm).</summary>
		public static int GCD(int a, int b) { while (b > 0) (a, b) = (b, a % b); return a; }
		/// <summary>Returns <paramref name="val"/>! Negative input returns 0. Overflows silently for large inputs.</summary>
		public static int Factorial(int val) { if (val < 0) return 0; int result = 1; for (int i = 2; i <= val; i++) result *= i; return result; }

		/// <summary>
		/// Given a desired <paramref name="totalValue"/> distributed across <paramref name="maxLevel"/> levels with
		/// percentage growth per level <paramref name="valueGrow"/> and a <paramref name="surplus"/> reserve %,
		/// returns the level-1 base value that produces the requested total.
		/// </summary>
		public static float CalcBaseValue(int maxLevel, float totalValue, float valueGrow, float surplus = 0) { float total = totalValue * (1f - surplus / 100f); if (maxLevel <= 1) return total; float denominator = 1; for (int i = 1; i < maxLevel; i++) denominator += Mathf.Pow(1 + valueGrow / 100f, i); return total / denominator; }

		/// <summary>Compound growth: returns <paramref name="baseValue"/> compounded by <paramref name="grow"/>% for (<paramref name="level"/> − 1) periods.</summary>
		public static float CalcCompoundingValue(float baseValue, float grow, int level) => level <= 1 ? baseValue : baseValue * Mathf.Pow(1 + grow / 100f, level - 1);
	}

	/// <summary>Numeric extension methods on primitives and Unity vectors.</summary>
	public static class MathExtension
	{
		/// <summary>Sign of <paramref name="x"/> as -1/0/1.</summary>
		public static int Sign(this int x) => x > 0 ? 1 : x < 0 ? -1 : 0;
		/// <summary>Sign of <paramref name="x"/> as -1/0/1 (0 returned for exact zero, unlike <see cref="Mathf.Sign"/> which returns 1).</summary>
		public static float Sign(this float x) => x > 0f ? 1f : x < 0f ? -1f : 0f;
		/// <summary>Binary representation padded to <paramref name="bits"/>.</summary>
		public static string ToBinary(this int val, int bits) => MathHelper.IntToBinary(val, bits);
		/// <summary>Component-wise round to <paramref name="decimalPlaces"/> places. See <see cref="MathHelper.Round(Vector3, int)"/>.</summary>
		public static Vector3 Round(this Vector3 vector, int decimalPlaces) => MathHelper.Round(vector, decimalPlaces);
		/// <summary>Returns an Euler angle vector where every negative component has 360 added so all components are in <c>[0, 360)</c>.</summary>
		public static Vector3 ToPositiveAngle(this Vector3 angle) { if (angle.x < 0) angle.x += 360f; if (angle.y < 0) angle.y += 360f; if (angle.z < 0) angle.z += 360f; return angle; }
		/// <summary>Wraps an angle into <c>[0, 360)</c>.</summary>
		public static float ToPositiveAngle(this float angle) => (angle % 360f + 360f) % 360f;
		/// <summary>Component-wise <see cref="Mathf.Approximately"/>.</summary>
		public static bool Approximately(this Vector3 value, Vector3 refValue) => Mathf.Approximately(value.x, refValue.x) && Mathf.Approximately(value.y, refValue.y) && Mathf.Approximately(value.z, refValue.z);
		/// <summary>Component-wise sign using <see cref="Mathf.Sign"/> (which returns 1 for zero).</summary>
		public static Vector3 Sign(this Vector3 val) => new(Mathf.Sign(val.x), Mathf.Sign(val.y), Mathf.Sign(val.z));
		/// <summary>Fluent variant of <see cref="Vector3Int.Set"/> — returns the modified vector.</summary>
		public static Vector3Int SetValue(this Vector3Int vector, int x, int y, int z) { vector.Set(x, y, z); return vector; }
		/// <summary>World-space distance from <paramref name="root"/> to the position of any other component.</summary>
		public static float DistanceTo<T>(this Transform root, T target) where T : Component => Vector3.Distance(root.position, target.transform.position);
		/// <summary>World-space distance from <paramref name="root"/> to <paramref name="target"/>.</summary>
		public static float DistanceTo(this Transform root, Vector3 target) => Vector3.Distance(root.position, target);
		/// <summary>Distance between two points.</summary>
		public static float DistanceTo(this Vector3 root, Vector3 target) => Vector3.Distance(root, target);
	}
}
