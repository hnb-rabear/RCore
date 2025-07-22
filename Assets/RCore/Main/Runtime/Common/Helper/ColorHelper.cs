/***
 * Author HNB-RaBear - 2019
 **/

using UnityEngine;

namespace RCore
{
    /// <summary>
    /// A static class containing extension methods for Unity's Color and Color32 structs.
    /// </summary>
    public static class ColorExtension
    {
        private const float LightOffset = 0.0625f;
        private const float DarkerFactor = 0.9f;

        /// <summary>
        /// Returns a new Color with the same RGB values as the original but with the specified alpha value.
        /// </summary>
        /// <param name="color">The original color.</param>
        /// <param name="alpha">The new alpha value (0 to 1).</param>
        /// <returns>A new color with the new alpha.</returns>
        public static Color SetAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// Returns a new color that is the RGB inverse of the original color. Alpha is preserved.
        /// </summary>
        /// <param name="color">The original color.</param>
        /// <returns>The inverted color.</returns>
        public static Color Invert(this Color color)
        {
            return new Color(1 - color.r, 1 - color.g, 1 - color.b, color.a);
        }

        /// <summary>
        /// Returns a new color with the same RGB values but with an alpha of 1 (fully opaque).
        /// </summary>
        /// <param name="color">The original color.</param>
        /// <returns>An opaque version of the color.</returns>
        public static Color Opaque(this Color color)
        {
            return new Color(color.r, color.g, color.b, 1f);
        }

        /// <summary>
        /// Checks if the color's RGB components are all very close to zero.
        /// </summary>
        /// <param name="color">The color to check.</param>
        /// <returns>True if the color is approximately black.</returns>
        public static bool IsApproximatelyBlack(this Color color)
        {
            return color.r + color.g + color.b <= Mathf.Epsilon;
        }

        /// <summary>
        /// Checks if the color's RGB components are all very close to one.
        /// </summary>
        /// <param name="color">The color to check.</param>
        /// <returns>True if the color is approximately white.</returns>
        public static bool IsApproximatelyWhite(this Color color)
        {
            return color.r + color.g + color.b >= 3 - Mathf.Epsilon;
        }

        /// <summary>
        /// Returns a new color that is slightly lighter than the original by a fixed offset.
        /// </summary>
        /// <param name="color">The original color.</param>
        /// <returns>A lighter color.</returns>
        public static Color Lighter(this Color color)
        {
            return new Color(
                color.r + LightOffset,
                color.g + LightOffset,
                color.b + LightOffset,
                color.a);
        }

        /// <summary>
        /// Returns a new color that is slightly darker than the original by a fixed offset.
        /// </summary>
        /// <param name="color">The original color.</param>
        /// <returns>A darker color.</returns>
        public static Color Darker(this Color color)
        {
            return new Color(
                color.r - LightOffset,
                color.g - LightOffset,
                color.b - LightOffset,
                color.a);
        }

        /// <summary>
        /// Calculates the average brightness of the color's RGB components.
        /// </summary>
        /// <param name="color">The color to check.</param>
        /// <returns>A float representing the brightness (0 to 1).</returns>
        public static float Brightness(this Color color)
        {
            return (color.r + color.g + color.b) / 3;
        }

        /// <summary>
        /// Converts the color to an RRGGBBAA hexadecimal string (e.g., "FF0000FF" for red).
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The hexadecimal string representation.</returns>
        public static string ToRGBAHexString(this Color color)
        {
            Color32 c32 = color;
            return $"{c32.r:X2}{c32.g:X2}{c32.b:X2}{c32.a:X2}";
        }
        
        /// <summary>
        /// Converts the color to a 32-bit integer representation (RRGGBBAA).
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The integer representation of the color.</returns>
        public static int ToRGBAHex(this Color color)
        {
            Color32 c = color;
            return c.ToRGBAHex();
        }

        /// <summary>
        /// Converts the color to a 32-bit integer representation (RRGGBBAA).
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The integer representation of the color.</returns>
        public static int ToRGBAHex(this Color32 color)
        {
            return color.r << 24 | color.g << 16 | color.b << 8 | color.a;
        }
        
        /// <summary>
        /// Converts the color to a 32-bit integer representation (RRGGBBAA).
        /// </summary>
        /// <param name="c">The color to convert.</param>
        /// <returns>The integer representation of the color.</returns>
        public static int ToInt(this Color c)
        {
            int retVal = 0;
            retVal |= Mathf.RoundToInt(c.r * 255f) << 24;
            retVal |= Mathf.RoundToInt(c.g * 255f) << 16;
            retVal |= Mathf.RoundToInt(c.b * 255f) << 8;
            retVal |= Mathf.RoundToInt(c.a * 255f);
            return retVal;
        }
    }

    /// <summary>
    /// A static class containing color-related helper methods and a collection of predefined color constants.
    /// </summary>
    public static class ColorHelper
    {
        #region Predefined Colors
        // A collection of light, predefined colors.
        public static readonly Color LightAzure = HexToColor("#92cbf1");
        public static readonly Color LightBrown = HexToColor("#d3b683");
        public static readonly Color LightLime = HexToColor("#aefd6c");
        public static readonly Color LightGold = HexToColor("#fddc5c");
        public static readonly Color LightPurple = HexToColor("#bf77f6");
        public static readonly Color LightYellow = HexToColor("#fffe7a");
        
        // A collection of dark, predefined colors.
        public static readonly Color DarkBlueGray = HexToColor("#666699");
        public static readonly Color DarkBrown = HexToColor("#654321");
        public static readonly Color DarkByzantium = HexToColor("#5D3954");
        public static readonly Color DarkCornflowerBlue = HexToColor("#26428B");
        public static readonly Color DarkCyan = HexToColor("#008B8B");
        public static readonly Color DarkElectricBlue = HexToColor("#536878");
        public static readonly Color DarkGreen = HexToColor("#013220");
        public static readonly Color DarkGreenX11 = HexToColor("#006400");
        public static readonly Color DarkJungleGreen = HexToColor("#1A2421");
        public static readonly Color DarkKhaki = HexToColor("#BDB76B");
        public static readonly Color DarkLava = HexToColor("#483C32");
        public static readonly Color DarkLiver = HexToColor("#534B4F");
        public static readonly Color DarkLiverHorses = HexToColor("#543D37");
        public static readonly Color DarkMagenta = HexToColor("#8B008B");
        public static readonly Color DarkMossGreen = HexToColor("#4A5D23");
        public static readonly Color DarkOliveGreen = HexToColor("#556B2F");
        public static readonly Color DarkOrange = HexToColor("#FF8C00");
        public static readonly Color DarkOrchid = HexToColor("#9932CC");
        public static readonly Color DarkPastelGreen = HexToColor("#03C03C");
        public static readonly Color DarkPurple = HexToColor("#301934");
        public static readonly Color DarkRed = HexToColor("#8B0000");
        public static readonly Color DarkSalmon = HexToColor("#E9967A");
        public static readonly Color DarkSeaGreen = HexToColor("#8FBC8F");
        public static readonly Color DarkSienna = HexToColor("#3C1414");
        public static readonly Color DarkSkyBlue = HexToColor("#8CBED6");
        public static readonly Color DarkSlateBlue = HexToColor("#483D8B");
        public static readonly Color DarkSlateGray = HexToColor("#2F4F4F");
        public static readonly Color DarkSpringGreen = HexToColor("#177245");
        #endregion
        
        /// <summary>
        /// Converts a Color object to a hexadecimal string (e.g., #FF0000). Alpha is ignored.
        /// </summary>
        /// <param name="pColor">The color to convert.</param>
        /// <returns>The hexadecimal string representation.</returns>
        public static string ToHex(Color pColor)
        {
            Color32 color32 = pColor;
            string hex = "#" + color32.r.ToString("X2") + color32.g.ToString("X2") + color32.b.ToString("X2");
            return hex;
        }

        /// <summary>
        /// Converts a hexadecimal string (e.g., "#FF0000" or "FF0000FF") to a Color object.
        /// </summary>
        /// <param name="pHex">The hexadecimal string. Can start with #, 0x, or nothing.</param>
        /// <returns>The corresponding Color object.</returns>
        public static Color HexToColor(string pHex)
        {
            pHex = pHex.Replace("0x", "");//in case the string is formatted 0xFFFFFF
            pHex = pHex.Replace("#", "");//in case the string is formatted #FFFFFF
            byte a = 255;//assume fully visible unless specified in hex
            byte r = byte.Parse(pHex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(pHex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(pHex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            //Only use alpha if the string has enough characters
            if (pHex.Length == 8)
            {
                a = byte.Parse(pHex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return new Color32(r, g, b, a);
        }

        /// <summary>
        /// Converts a Color object to a 32-bit integer representation (RRGGBBAA).
        /// </summary>
        /// <param name="c">The color to convert.</param>
        /// <returns>The integer representation of the color.</returns>
        public static int ColorToInt(Color c)
        {
            int retVal = 0;
            retVal |= Mathf.RoundToInt(c.r * 255f) << 24;
            retVal |= Mathf.RoundToInt(c.g * 255f) << 16;
            retVal |= Mathf.RoundToInt(c.b * 255f) << 8;
            retVal |= Mathf.RoundToInt(c.a * 255f);
            return retVal;
        }
        
        /// <summary>
        /// Converts a 32-bit integer (RRGGBBAA) to a Color object.
        /// </summary>
        /// <param name="val">The integer representation of the color.</param>
        /// <returns>The corresponding Color object.</returns>
        public static Color IntToColor(int val)
        {
            float inv = 1f / 255f;
            var c = Color.black;
            c.r = inv * (val >> 24 & 0xFF);
            c.g = inv * (val >> 16 & 0xFF);
            c.b = inv * (val >> 8 & 0xFF);
            c.a = inv * (val & 0xFF);
            return c;
        }
    }
}