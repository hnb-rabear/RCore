using UnityEngine;

namespace RevCore
{
	/// <summary>Extension methods on <see cref="Color"/> and <see cref="Color32"/>.</summary>
	public static class ColorExtension
	{
		/// <summary>Returns a copy of <paramref name="color"/> with its alpha replaced by <paramref name="alpha"/>.</summary>
		public static Color SetAlpha(this Color color, float alpha) { color.a = alpha; return color; }

		/// <summary>Returns the color inversion (1 - r, 1 - g, 1 - b) with alpha preserved.</summary>
		public static Color Invert(this Color color) => new(1f - color.r, 1f - color.g, 1f - color.b, color.a);

		/// <summary>Returns a copy of <paramref name="color"/> with alpha set to 1.</summary>
		public static Color Opaque(this Color color) { color.a = 1f; return color; }

		/// <summary>Returns <c>true</c> if R + G + B is at or below <see cref="Mathf.Epsilon"/> (alpha ignored).</summary>
		public static bool IsApproximatelyBlack(this Color color) => color.r + color.g + color.b <= Mathf.Epsilon;

		/// <summary>Returns <c>true</c> if R + G + B is at or above 3 − <see cref="Mathf.Epsilon"/> (alpha ignored).</summary>
		public static bool IsApproximatelyWhite(this Color color) => color.r + color.g + color.b >= 3f - Mathf.Epsilon;

		/// <summary>Returns a clamped lighter variant by adding 0.2 to each RGB channel.</summary>
		public static Color Lighter(this Color color) => new(Mathf.Clamp01(color.r + 0.2f), Mathf.Clamp01(color.g + 0.2f), Mathf.Clamp01(color.b + 0.2f), color.a);

		/// <summary>Returns a clamped darker variant by subtracting 0.2 from each RGB channel.</summary>
		public static Color Darker(this Color color) => new(Mathf.Clamp01(color.r - 0.2f), Mathf.Clamp01(color.g - 0.2f), Mathf.Clamp01(color.b - 0.2f), color.a);

		/// <summary>Returns the perceptual brightness (Rec. 709 luma): 0.2126·R + 0.7152·G + 0.0722·B.</summary>
		public static float Brightness(this Color color) => 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;

		/// <summary>Renders the color as <c>"#RRGGBBAA"</c> hex.</summary>
		public static string ToRGBAHexString(this Color color) => $"#{ColorUtility.ToHtmlStringRGBA(color)}";

		/// <summary>Packs the color into a single <see cref="int"/> in RGBA byte order (R is high byte, A is low byte).</summary>
		public static int ToRGBAHex(this Color color) { var c = (Color32)color; return (c.r << 24) | (c.g << 16) | (c.b << 8) | c.a; }

		/// <summary>Packs the color into a single <see cref="int"/> in RGBA byte order (R is high byte, A is low byte).</summary>
		public static int ToRGBAHex(this Color32 color) => (color.r << 24) | (color.g << 16) | (color.b << 8) | color.a;

		/// <summary>Alias of <see cref="ToRGBAHex(Color)"/> kept for compatibility with older code.</summary>
		public static int ToInt(this Color c) { var c32 = (Color32)c; return (c32.r << 24) | (c32.g << 16) | (c32.b << 8) | c32.a; }
	}

	/// <summary>Static helpers for parsing color strings.</summary>
	public static class ColorHelper
	{
		/// <summary>
		/// Parses an HTML-style hex color string. On any failure (invalid format, null, or empty
		/// input) returns whatever Unity's <see cref="ColorUtility.TryParseHtmlString"/> happens
		/// to leave in its <c>out</c> parameter — observed as <see cref="Color.white"/> on Unity
		/// 2022.3 — without raising. This silent fallback is retained for backward compatibility.
		/// Prefer <see cref="TryHexToColor"/> in new code; it enforces a stable failure value.
		/// </summary>
		public static Color HexToColor(string hex)
		{
			ColorUtility.TryParseHtmlString(hex, out var color);
			return color;
		}

		/// <summary>
		/// Tries to parse an HTML-style hex color string. Returns <c>true</c> on success and writes
		/// the parsed color to <paramref name="color"/>; on failure returns <c>false</c> with
		/// <paramref name="color"/> set to <see cref="Color.clear"/>.
		/// </summary>
		/// <param name="hex">Hex string in any form accepted by <see cref="ColorUtility.TryParseHtmlString"/>
		/// (e.g. <c>#RRGGBB</c>, <c>#RRGGBBAA</c>, <c>red</c>). May be <c>null</c>.</param>
		/// <param name="color">The parsed color when successful; otherwise <see cref="Color.clear"/>.</param>
		public static bool TryHexToColor(string hex, out Color color)
		{
			if (string.IsNullOrEmpty(hex))
			{
				color = Color.clear;
				return false;
			}
			if (!ColorUtility.TryParseHtmlString(hex, out color))
			{
				// Unity's TryParseHtmlString leaves its out param at Color.white on failure;
				// enforce the documented Color.clear contract here.
				color = Color.clear;
				return false;
			}
			return true;
		}
	}
}
