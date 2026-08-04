using System;
using UnityEngine;

namespace DNExtensions.Utilities
{
    public static class ColorExtensions
    {
        #region Component Setters

        /// <summary>
        /// Returns a new color with the specified red component
        /// </summary>
        public static Color SetRed(this Color color, float r)
        {
            return new Color(r, color.g, color.b, color.a);
        }

        /// <summary>
        /// Returns a new color with the specified green component
        /// </summary>
        public static Color SetGreen(this Color color, float g)
        {
            return new Color(color.r, g, color.b, color.a);
        }

        /// <summary>
        /// Returns a new color with the specified blue component
        /// </summary>
        public static Color SetBlue(this Color color, float b)
        {
            return new Color(color.r, color.g, b, color.a);
        }

        /// <summary>
        /// Returns a new color with the specified alpha component
        /// </summary>
        public static Color SetAlpha(this Color color, float a)
        {
            return new Color(color.r, color.g, color.b, a);
        }

        /// <summary>
        /// Returns a new color with alpha set to 0 (fully transparent)
        /// </summary>
        public static Color Transparent(this Color color)
        {
            return color.SetAlpha(0f);
        }

        /// <summary>
        /// Returns a new color with alpha set to 1 (fully opaque)
        /// </summary>
        public static Color Opaque(this Color color)
        {
            return color.SetAlpha(1f);
        }

        #endregion

        #region Math Operations

        /// <summary>
        /// Adds the RGBA components of two colors and clamps the result between 0 and 1
        /// </summary>
        public static Color Add(this Color thisColor, Color otherColor)
        {
            return (thisColor + otherColor).Clamp01();
        }

        /// <summary>
        /// Subtracts the RGBA components of one color from another and clamps the result between 0 and 1
        /// </summary>
        public static Color Subtract(this Color thisColor, Color otherColor)
        {
            return (thisColor - otherColor).Clamp01();
        }

        /// <summary>
        /// Multiplies the RGB components by a factor (alpha unchanged)
        /// </summary>
        public static Color Multiply(this Color color, float factor)
        {
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a).Clamp01();
        }

        /// <summary>
        /// Clamps the RGBA components of the color between 0 and 1
        /// </summary>
        public static Color Clamp01(this Color color)
        {
            return new Color(
                Mathf.Clamp01(color.r),
                Mathf.Clamp01(color.g),
                Mathf.Clamp01(color.b),
                Mathf.Clamp01(color.a)
            );
        }

        #endregion

        #region Blending and Mixing

        /// <summary>
        /// Blends two colors with a specified ratio (same as Color.Lerp)
        /// </summary>
        /// <param name="color1">The first color</param>
        /// <param name="color2">The second color</param>
        /// <param name="ratio">The blend ratio (0 = color1, 1 = color2)</param>
        public static Color Blend(this Color color1, Color color2, float ratio)
        {
            return Color.Lerp(color1, color2, ratio);
        }

        /// <summary>
        /// Inverts the RGB components (alpha unchanged)
        /// </summary>
        public static Color Invert(this Color color)
        {
            return new Color(1f - color.r, 1f - color.g, 1f - color.b, color.a);
        }

        /// <summary>
        /// Converts the color to grayscale using luminance weights
        /// </summary>
        public static Color Grayscale(this Color color)
        {
            float gray = color.grayscale;
            return new Color(gray, gray, gray, color.a);
        }

        #endregion

        #region Hex Conversion

        /// <summary>
        /// Converts a Color to a hexadecimal string (with alpha)
        /// </summary>
        public static string ToHex(this Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(color);
        }

        /// <summary>
        /// Converts a Color to a hexadecimal string (without alpha)
        /// </summary>
        public static string ToHexRGB(this Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }

        /// <summary>
        /// Converts a hexadecimal string to a Color
        /// </summary>
        public static Color FromHex(this string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                return color;
            }
            throw new ArgumentException($"Invalid hex string: {hex}", nameof(hex));
        }

        /// <summary>
        /// Tries to convert a hexadecimal string to a Color
        /// </summary>
        public static bool TryFromHex(this string hex, out Color color)
        {
            return ColorUtility.TryParseHtmlString(hex, out color);
        }

        #endregion

        #region HSV Conversion

        /// <summary>
        /// Converts RGB to HSV values
        /// </summary>
        public static void ToHSV(this Color color, out float h, out float s, out float v)
        {
            Color.RGBToHSV(color, out h, out s, out v);
        }

        /// <summary>
        /// Creates a Color from HSV values
        /// </summary>
        public static Color FromHSV(float h, float s, float v, float a = 1f)
        {
            Color color = Color.HSVToRGB(h, s, v);
            color.a = a;
            return color;
        }

        /// <summary>
        /// Adjusts the hue of the color
        /// </summary>
        /// <param name="color">The original color</param>
        /// <param name="hueShift">Amount to shift hue (0-1, wraps around)</param>
        public static Color ShiftHue(this Color color, float hueShift)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            h = (h + hueShift) % 1f;
            if (h < 0f) h += 1f;
            return Color.HSVToRGB(h, s, v).SetAlpha(color.a);
        }

        /// <summary>
        /// Adjusts the saturation of the color
        /// </summary>
        public static Color SetSaturation(this Color color, float saturation)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            return Color.HSVToRGB(h, Mathf.Clamp01(saturation), v).SetAlpha(color.a);
        }

        /// <summary>
        /// Adjusts the brightness/value of the color
        /// </summary>
        public static Color SetBrightness(this Color color, float brightness)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            return Color.HSVToRGB(h, s, Mathf.Clamp01(brightness)).SetAlpha(color.a);
        }

        #endregion

        #region Color32 Conversion

        /// <summary>
        /// Converts Color to Color32
        /// </summary>
        public static Color32 ToColor32(this Color color)
        {
            return color;
        }

        /// <summary>
        /// Converts Color32 to Color
        /// </summary>
        public static Color ToColor(this Color32 color32)
        {
            return color32;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Returns a random color with full opacity
        /// </summary>
        public static Color Random()
        {
            return new Color(
                UnityEngine.Random.value,
                UnityEngine.Random.value,
                UnityEngine.Random.value,
                1f
            );
        }

        /// <summary>
        /// Returns a random color with random alpha
        /// </summary>
        public static Color RandomWithAlpha()
        {
            return new Color(
                UnityEngine.Random.value,
                UnityEngine.Random.value,
                UnityEngine.Random.value,
                UnityEngine.Random.value
            );
        }

        /// <summary>
        /// Checks if two colors are approximately equal
        /// </summary>
        public static bool Approximately(this Color color1, Color color2, float tolerance = 0.01f)
        {
            return Mathf.Abs(color1.r - color2.r) < tolerance &&
                   Mathf.Abs(color1.g - color2.g) < tolerance &&
                   Mathf.Abs(color1.b - color2.b) < tolerance &&
                   Mathf.Abs(color1.a - color2.a) < tolerance;
        }

        #endregion
    }
}