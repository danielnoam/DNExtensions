using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DNExtensions.Utilities
{
    public static class StringExtensions
    {
        /// <summary>
        /// Checks if a string is null or white space
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Checks if a string is null or empty
        /// </summary>
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Checks if a string is null, empty, or white space
        /// </summary>
        public static bool IsBlank(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Returns the string or an empty string if null
        /// </summary>
        public static string OrEmpty(this string value)
        {
            return value ?? string.Empty;
        }

        /// <summary>
        /// Shortens a string to the specified maximum length
        /// </summary>
        public static string Shorten(this string value, int maxLength)
        {
            if (value.IsBlank()) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        /// <summary>
        /// Shortens a string to the specified maximum length and adds an ellipsis if truncated
        /// </summary>
        public static string ShortenWithEllipsis(this string value, int maxLength, string ellipsis = "...")
        {
            if (value.IsBlank()) return value;
            if (value.Length <= maxLength) return value;
            
            int targetLength = maxLength - ellipsis.Length;
            if (targetLength <= 0) return ellipsis;
            
            return value.Substring(0, targetLength) + ellipsis;
        }

        /// <summary>
        /// Slices a string from the start index to the end index (supports negative indices)
        /// </summary>
        public static string Slice(this string value, int startIndex, int endIndex)
        {
            if (value.IsBlank())
            {
                throw new ArgumentNullException(nameof(value), "Value cannot be null or empty.");
            }

            if (startIndex < 0 || startIndex > value.Length - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            endIndex = endIndex < 0 ? value.Length + endIndex : endIndex;

            if (endIndex < 0 || endIndex < startIndex || endIndex > value.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(endIndex));
            }

            return value.Substring(startIndex, endIndex - startIndex);
        }

        /// <summary>
        /// Converts the string to an alphanumeric string, optionally allowing periods
        /// </summary>
        public static string ToAlphanumeric(this string input, bool allowPeriods = false)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            List<char> filteredChars = new List<char>();
            int lastValidIndex = -1;

            foreach (char character in input
                .Where(character => char.IsLetterOrDigit(character) || character == '_' || (allowPeriods && character == '.'))
                .Where(character => filteredChars.Count != 0 || (!char.IsDigit(character) && character != '.')))
            {
                filteredChars.Add(character);
                lastValidIndex = filteredChars.Count - 1;
            }

            while (lastValidIndex >= 0 && filteredChars[lastValidIndex] == '.')
            {
                lastValidIndex--;
            }

            return lastValidIndex >= 0
                ? new string(filteredChars.ToArray(), 0, lastValidIndex + 1)
                : string.Empty;
        }

        /// <summary>
        /// Removes all whitespace from the string
        /// </summary>
        public static string RemoveWhitespace(this string value)
        {
            if (value.IsBlank()) return value;
            return new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        /// <summary>
        /// Reverses the string
        /// </summary>
        public static string Reverse(this string value)
        {
            if (value.IsBlank()) return value;
            char[] chars = value.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        /// <summary>
        /// Repeats the string the specified number of times
        /// </summary>
        public static string Repeat(this string value, int count)
        {
            if (value.IsBlank() || count <= 0) return string.Empty;
            
            StringBuilder sb = new StringBuilder(value.Length * count);
            for (int i = 0; i < count; i++)
            {
                sb.Append(value);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Counts the occurrences of a substring within the string
        /// </summary>
        public static int CountOccurrences(this string value, string substring)
        {
            if (value.IsBlank() || substring.IsBlank()) return 0;
            
            int count = 0;
            int index = 0;
            
            while ((index = value.IndexOf(substring, index, StringComparison.Ordinal)) != -1)
            {
                count++;
                index += substring.Length;
            }
            
            return count;
        }

        #region Rich Text Formatting (Unity UI)

        /// <summary>
        /// Starts a fluent rich text builder for combining multiple formatting options
        /// </summary>
        public static RichTextBuilder Rich(this string text)
        {
            return new RichTextBuilder(text);
        }

        /// <summary>
        /// Wraps the text in a color tag for Unity rich text
        /// </summary>
        public static string RichColor(this string text, string color)
        {
            return $"<color={color}>{text}</color>";
        }

        /// <summary>
        /// Wraps the text in a size tag for Unity rich text
        /// </summary>
        public static string RichSize(this string text, int size)
        {
            return $"<size={size}>{text}</size>";
        }

        /// <summary>
        /// Wraps the text in a bold tag for Unity rich text
        /// </summary>
        public static string RichBold(this string text)
        {
            return $"<b>{text}</b>";
        }

        /// <summary>
        /// Wraps the text in an italic tag for Unity rich text
        /// </summary>
        public static string RichItalic(this string text)
        {
            return $"<i>{text}</i>";
        }

        /// <summary>
        /// Wraps the text in an underline tag for Unity rich text
        /// </summary>
        public static string RichUnderline(this string text)
        {
            return $"<u>{text}</u>";
        }

        /// <summary>
        /// Wraps the text in a strikethrough tag for Unity rich text
        /// </summary>
        public static string RichStrikethrough(this string text)
        {
            return $"<s>{text}</s>";
        }

        /// <summary>
        /// Wraps the text in a font tag for Unity rich text
        /// </summary>
        public static string RichFont(this string text, string font)
        {
            return $"<font={font}>{text}</font>";
        }

        /// <summary>
        /// Wraps the text in an align tag for Unity rich text
        /// </summary>
        public static string RichAlign(this string text, string align)
        {
            return $"<align={align}>{text}</align>";
        }

        /// <summary>
        /// Wraps the text in a gradient tag for Unity rich text (TextMeshPro)
        /// </summary>
        public static string RichGradient(this string text, string color1, string color2)
        {
            return $"<gradient={color1},{color2}>{text}</gradient>";
        }

        /// <summary>
        /// Wraps the text in a rotation tag for Unity rich text (TextMeshPro)
        /// </summary>
        public static string RichRotation(this string text, float angle)
        {
            return $"<rotate={angle}>{text}</rotate>";
        }

        /// <summary>
        /// Wraps the text in a space tag for Unity rich text (TextMeshPro)
        /// </summary>
        public static string RichSpace(this string text, float space)
        {
            return $"<space={space}>{text}</space>";
        }

        #endregion
    }

    /// <summary>
    /// Fluent builder for combining multiple Unity rich text formatting options
    /// </summary>
    public class RichTextBuilder
    {
        private string _text;

        public RichTextBuilder(string text)
        {
            _text = text;
        }

        public RichTextBuilder Color(string color)
        {
            _text = $"<color={color}>{_text}</color>";
            return this;
        }

        public RichTextBuilder Size(int size)
        {
            _text = $"<size={size}>{_text}</size>";
            return this;
        }

        public RichTextBuilder Bold()
        {
            _text = $"<b>{_text}</b>";
            return this;
        }

        public RichTextBuilder Italic()
        {
            _text = $"<i>{_text}</i>";
            return this;
        }

        public RichTextBuilder Underline()
        {
            _text = $"<u>{_text}</u>";
            return this;
        }

        public RichTextBuilder Strikethrough()
        {
            _text = $"<s>{_text}</s>";
            return this;
        }

        public RichTextBuilder Font(string font)
        {
            _text = $"<font={font}>{_text}</font>";
            return this;
        }

        public RichTextBuilder Align(string align)
        {
            _text = $"<align={align}>{_text}</align>";
            return this;
        }

        public RichTextBuilder Gradient(string color1, string color2)
        {
            _text = $"<gradient={color1},{color2}>{_text}</gradient>";
            return this;
        }

        public RichTextBuilder Rotation(float angle)
        {
            _text = $"<rotate={angle}>{_text}</rotate>";
            return this;
        }

        public RichTextBuilder Space(float space)
        {
            _text = $"<space={space}>{_text}</space>";
            return this;
        }

        public override string ToString()
        {
            return _text;
        }

        public static implicit operator string(RichTextBuilder builder)
        {
            return builder._text;
        }
    }
}