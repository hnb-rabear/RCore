using System.Collections.Generic;
using UnityEngine;

namespace RevCore
{
	public static class MathHelper
	{
		public static float CalcAngle(Vector3 dir1, Vector3 dir2)
		{
			float dot = Vector3.Dot(dir1.normalized, dir2.normalized);
			dot = Mathf.Clamp(dot, -1f, 1f);
			return Mathf.Acos(dot) * Mathf.Rad2Deg;
		}

		public static float CalcAngle(Vector2 direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		public static Vector3 CalcAngle360(Vector3 fromDir, Vector3 toDir) => Quaternion.FromToRotation(Vector3.forward, toDir - fromDir).eulerAngles;
		public static float CalcAngle360X(Vector3 fromDir, Vector3 toDir) => Quaternion.FromToRotation(Vector3.right, toDir - fromDir).eulerAngles.z;
		public static float CalcAngle360Z(Vector3 fromDir, Vector3 toDir) => Quaternion.FromToRotation(Vector3.up, toDir - fromDir).eulerAngles.z;
		public static float CalcAngle360Y(Vector3 fromDir, Vector3 toDir) => Quaternion.FromToRotation(Vector3.forward, toDir - fromDir).eulerAngles.y;

		public static float WrapAngle(float angle)
		{
			angle %= 360;
			if (angle > 180) return angle - 360;
			if (angle < -180) return angle + 360;
			return angle;
		}

		public static bool InsideAngle(float angle, float minAngle, float maxAngle)
		{
			float normalizedAngle = (angle % 360 + 360) % 360;
			float normalizedMin = (minAngle % 360 + 360) % 360;
			float normalizedMax = (maxAngle % 360 + 360) % 360;
			return normalizedMin <= normalizedMax
				? normalizedAngle >= normalizedMin && normalizedAngle <= normalizedMax
				: normalizedAngle >= normalizedMin || normalizedAngle <= normalizedMax;
		}

		public static Vector3 DirOfYAngle(float angleDegrees)
		{
			float rad = angleDegrees * Mathf.Deg2Rad;
			return new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
		}

		public static Vector3 DirOfXAngle(float angleDegrees) => Quaternion.AngleAxis(angleDegrees, Vector3.right) * Vector3.forward;
		public static Vector3 DirOfZAngle(float angleDegrees) => Quaternion.AngleAxis(angleDegrees, Vector3.forward) * Vector3.up;
		public static Vector3 DirOfAngle(Vector3 angle) => Quaternion.Euler(angle) * Vector3.forward;
		public static float SinRad(float radian) => Mathf.Sin(radian);
		public static float CosRad(float radian) => Mathf.Cos(radian);
		public static float SinDeg(float degree) => Mathf.Sin(degree * Mathf.Deg2Rad);
		public static float CosDeg(float degree) => Mathf.Cos(degree * Mathf.Deg2Rad);
		public static float TanDeg(float degree) => Mathf.Tan(degree * Mathf.Deg2Rad);
		public static float Deg2Rad(float degree) => degree * Mathf.Deg2Rad;
		public static float Rad2Deg(float radian) => radian * Mathf.Rad2Deg;
		public static float Ded2Rad(float degree) => Deg2Rad(degree);
		public static float Tad2Deg(float radian) => Rad2Deg(radian);
		public static float AngleDeg(Vector2 from, Vector2 to) => AtanDeg(to.y - from.y, to.x - from.x);
		public static float AtanDeg(float dy, float dx) => Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
		public static float AtanRad(float dy, float dx) => Mathf.Atan2(dy, dx);

		public static float Dot(Transform root, Transform target) => Vector3.Dot(root.forward, (target.position - root.position).normalized);
		public static bool OnRightOrLeft(Transform root, Vector3 position) => Vector3.Dot(root.right, position - root.position) > 0;

		public static bool IsInside(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
		{
			float denom = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
			if (Mathf.Abs(denom) < Mathf.Epsilon) return false;
			float w1 = ((b.y - c.y) * (p.x - c.x) + (c.x - b.x) * (p.y - c.y)) / denom;
			float w2 = ((c.y - a.y) * (p.x - c.x) + (a.x - c.x) * (p.y - c.y)) / denom;
			float w3 = 1f - w1 - w2;
			return w1 >= 0 && w2 >= 0 && w3 >= 0;
		}

		public static bool IsBetween(Vector3 from, Vector3 to, Vector3 mid) => Vector3.Dot(mid - from, to - from) > 0 && Vector3.Dot(mid - to, from - to) > 0;
		public static bool IsBetweenXZ(Vector3 from, Vector3 to, Vector3 mid) => (from.z * mid.x - from.x * mid.z) * (to.z * mid.x - to.x * mid.z) < 0;
		public static bool IsBetween(Vector2 from, Vector2 to, Vector2 mid) => (from.y * mid.x - from.x * mid.y) * (to.y * mid.x - to.x * mid.y) < 0;
		public static Vector3 CalcPosition(Vector3 rootPos, float distance, Vector3 dir) => rootPos + dir.normalized * distance;
		public static Vector3 LeftDirection(Vector3 forward, Vector3 up) => Vector3.Cross(up, forward);
		public static Vector3 RightDirection(Vector3 forward, Vector3 up) => Vector3.Cross(forward, up);
		public static Vector3 LeftDirectionXZ(Vector3 forward) => new(-forward.z, forward.y, forward.x);
		public static Vector3 RightDirectionXZ(Vector3 forward) => new(forward.z, forward.y, -forward.x);

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

		public static Vector3 RandomPointOnCircleEdge_XZ(Vector3 center, float radius)
		{
			Vector2 pos = Random.insideUnitCircle.normalized * radius;
			return new Vector3(center.x + pos.x, center.y, center.z + pos.y);
		}

		public static Vector3 RandomPointOnCircle_XZ(Vector3 center, float radius)
		{
			Vector2 pos = Random.insideUnitCircle * radius;
			return new Vector3(center.x + pos.x, center.y, center.z + pos.y);
		}

		public static Vector3 RandomPointOnCircleEdge_XY(Vector2 center, float radius)
		{
			Vector2 pos = Random.insideUnitCircle.normalized * radius;
			return new Vector3(center.x + pos.x, center.y + pos.y, 0);
		}

		public static Vector2 GetPosOnCircle(float angleDeg, float radius) => new(CosDeg(angleDeg) * radius, SinDeg(angleDeg) * radius);
		public static Vector3 GetPosOnCircle(Vector3 root, float angleDeg, float radius) => root + DirOfYAngle(angleDeg) * radius;

		public static int GetRandomIndexFromChances(List<int> chances) => GetRandomIndexFromChances(chances.ToArray());
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

		public static List<Vector3> CalcGridNodes(Vector3 rootPos, int width, int length, float tileSize, bool rootIsCenter = false)
		{
			var list = new List<Vector3>();
			Vector3 startOffset = rootIsCenter ? new Vector3(-width * tileSize / 2f, 0, -length * tileSize / 2f) : Vector3.zero;
			for (int i = 0; i < width; i++)
				for (int j = 0; j < length; j++)
					list.Add(rootPos + startOffset + new Vector3(tileSize * i + tileSize / 2f, 0, tileSize * j + tileSize / 2f));
			return list;
		}

		public static float Round(float value, int decimalPlaces)
		{
			float pow = Mathf.Pow(10, decimalPlaces);
			return Mathf.Round(value * pow) / pow;
		}

		public static Vector2 Round(Vector2 value, int decimalPlaces)
		{
			float pow = Mathf.Pow(10, decimalPlaces);
			value.x = Mathf.Round(value.x * pow) / pow;
			value.y = Mathf.Round(value.y * pow) / pow;
			return value;
		}

		public static Vector3 Round(Vector3 value, int decimalPlaces)
		{
			float pow = Mathf.Pow(10, decimalPlaces);
			value.x = Mathf.Round(value.x * pow) / pow;
			value.y = Mathf.Round(value.y * pow) / pow;
			value.z = Mathf.Round(value.z * pow) / pow;
			return value;
		}

		public static string IntToBinary(int val, int bits) => System.Convert.ToString(val, 2).PadLeft(bits, '0');
		public static float Lerp(float from, float to, float factor) => from * (1f - factor) + to * factor;
		public static int ClampIndex(int val, int max) => val < 0 ? 0 : val < max ? val : max - 1;
		public static int RepeatIndex(int val, int max) => max < 1 ? 0 : (val % max + max) % max;
		public static int HexToDecimal(char ch) => ch >= '0' && ch <= '9' ? ch - '0' : ch >= 'A' && ch <= 'F' ? ch - 'A' + 10 : ch >= 'a' && ch <= 'f' ? ch - 'a' + 10 : -1;
		public static char DecimalToHexChar(int num) => num < 0 || num > 15 ? '?' : num < 10 ? (char)('0' + num) : (char)('A' + num - 10);
		public static string DecimalToHex8(int num) => (num & 0xFF).ToString("X2");
		public static string DecimalToHex24(int num) => (num & 0xFFFFFF).ToString("X6");
		public static string DecimalToHex32(int num) => num.ToString("X8");
		public static int Sum(params int[] numbers) { int sum = 0; for (int i = 0; i < numbers.Length; i++) sum += numbers[i]; return sum; }
		public static int GCD(int a, int b) { while (b > 0) (a, b) = (b, a % b); return a; }
		public static int Factorial(int val) { if (val < 0) return 0; int result = 1; for (int i = 2; i <= val; i++) result *= i; return result; }
		public static float CalcBaseValue(int maxLevel, float totalValue, float valueGrow, float surplus = 0) { float total = totalValue * (1f - surplus / 100f); if (maxLevel <= 1) return total; float denominator = 1; for (int i = 1; i < maxLevel; i++) denominator += Mathf.Pow(1 + valueGrow / 100f, i); return total / denominator; }
		public static float CalcCompoundingValue(float baseValue, float grow, int level) => level <= 1 ? baseValue : baseValue * Mathf.Pow(1 + grow / 100f, level - 1);
	}

	public static class MathExtension
	{
		public static int Sign(this int x) => x > 0 ? 1 : x < 0 ? -1 : 0;
		public static float Sign(this float x) => x > 0f ? 1f : x < 0f ? -1f : 0f;
		public static string ToBinary(this int val, int bits) => MathHelper.IntToBinary(val, bits);
		public static Vector3 Round(this Vector3 vector, int decimalPlaces) => MathHelper.Round(vector, decimalPlaces);
		public static Vector3 ToPositiveAngle(this Vector3 angle) { if (angle.x < 0) angle.x += 360f; if (angle.y < 0) angle.y += 360f; if (angle.z < 0) angle.z += 360f; return angle; }
		public static float ToPositiveAngle(this float angle) => (angle % 360f + 360f) % 360f;
		public static bool Approximately(this Vector3 value, Vector3 refValue) => Mathf.Approximately(value.x, refValue.x) && Mathf.Approximately(value.y, refValue.y) && Mathf.Approximately(value.z, refValue.z);
		public static Vector3 Sign(this Vector3 val) => new(Mathf.Sign(val.x), Mathf.Sign(val.y), Mathf.Sign(val.z));
		public static Vector3Int SetValue(this Vector3Int vector, int x, int y, int z) { vector.Set(x, y, z); return vector; }
		public static float DistanceTo<T>(this Transform root, T target) where T : Component => Vector3.Distance(root.position, target.transform.position);
		public static float DistanceTo(this Transform root, Vector3 target) => Vector3.Distance(root.position, target);
		public static float DistanceTo(this Vector3 root, Vector3 target) => Vector3.Distance(root, target);
	}
}
