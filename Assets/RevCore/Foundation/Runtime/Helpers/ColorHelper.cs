using UnityEngine;

namespace RevCore
{
	public static class ColorExtension
	{
		public static Color SetAlpha(this Color color, float alpha) { color.a = alpha; return color; }
		public static Color Invert(this Color color) => new(1f - color.r, 1f - color.g, 1f - color.b, color.a);
		public static Color Opaque(this Color color) { color.a = 1f; return color; }
		public static bool IsApproximatelyBlack(this Color color) => color.r + color.g + color.b <= Mathf.Epsilon;
		public static bool IsApproximatelyWhite(this Color color) => color.r + color.g + color.b >= 3f - Mathf.Epsilon;
		public static Color Lighter(this Color color) => new(Mathf.Clamp01(color.r + 0.2f), Mathf.Clamp01(color.g + 0.2f), Mathf.Clamp01(color.b + 0.2f), color.a);
		public static Color Darker(this Color color) => new(Mathf.Clamp01(color.r - 0.2f), Mathf.Clamp01(color.g - 0.2f), Mathf.Clamp01(color.b - 0.2f), color.a);
		public static float Brightness(this Color color) => 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
		public static string ToRGBAHexString(this Color color) => $"#{ColorUtility.ToHtmlStringRGBA(color)}";
		public static int ToRGBAHex(this Color color) { var c = (Color32)color; return (c.r << 24) | (c.g << 16) | (c.b << 8) | c.a; }
		public static int ToRGBAHex(this Color32 color) => (color.r << 24) | (color.g << 16) | (color.b << 8) | color.a;
		public static int ToInt(this Color c) { var c32 = (Color32)c; return (c32.r << 24) | (c32.g << 16) | (c32.b << 8) | c32.a; }
	}

	public static class ColorHelper
	{
		public static Color HexToColor(string hex)
		{
			ColorUtility.TryParseHtmlString(hex, out var color);
			return color;
		}
	}
}
