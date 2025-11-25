using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DNExtensions
{
    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(RangedInt), true)]
    public class RangedIntDrawer : PropertyDrawer
    {
        private static GUIStyle _labelStyle;
        
        private static GUIStyle GetLabelStyle()
        {
            return _labelStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter
            };
        }
        
        // Customizable UI constants
        private const float FieldPadding = 5f;     // Spacing between UI elements
        private const float FieldWidth = 50f;      // Width of the min/max input fields
        private const float FieldHeight = 18f;     // Height of the min/max input fields
        private const int DefaultMinRange = -1;    // Default minimum range when no attribute is specified
        private const int DefaultMaxRange = 1;     // Default maximum range when no attribute is specified
        
        // Range label settings
        private const bool ShowRangeValue = true;  // Toggle to show/hide the range value
        private const float LabelYOffset = 15f;    // How far above the slider to show the label


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return ShowRangeValue ? FieldHeight + 15f : FieldHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Draw the label
            position = EditorGUI.PrefixLabel(position, label);

            SerializedProperty minProp = property.FindPropertyRelative("minValue");
            SerializedProperty maxProp = property.FindPropertyRelative("maxValue");

            // Get custom range attributes or use defaults
            int rangeMin, rangeMax;
            var ranges = (MinMaxRangeAttribute[])fieldInfo.GetCustomAttributes(typeof(MinMaxRangeAttribute), true);
            if (ranges.Length > 0)
            {
                rangeMin = Mathf.RoundToInt(ranges[0].Min);
                rangeMax = Mathf.RoundToInt(ranges[0].Max);
            }
            else
            {
                rangeMin = DefaultMinRange;
                rangeMax = DefaultMaxRange;
            }

            // Calculate rects
            Rect minFieldRect = new Rect(position.x, position.y, FieldWidth, FieldHeight);
            Rect sliderRect = new Rect(minFieldRect.xMax + FieldPadding, position.y, 
                position.width - (FieldWidth * 2) - (FieldPadding * 2), FieldHeight);
            Rect maxFieldRect = new Rect(sliderRect.xMax + FieldPadding, position.y, FieldWidth, FieldHeight);

            // Min field
            EditorGUI.BeginChangeCheck();
            int minValue = EditorGUI.IntField(minFieldRect, minProp.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                minProp.intValue = Mathf.Min(minValue, maxProp.intValue);
            }

            // Draw range value above slider if enabled
            if (ShowRangeValue)
            {
                int rangeValue = maxProp.intValue - minProp.intValue;
    
                Rect labelRect = new Rect(
                    sliderRect.x, 
                    sliderRect.y + LabelYOffset, 
                    sliderRect.width, 
                    20
                );
    
                EditorGUI.LabelField(labelRect, "Range " + rangeValue, GetLabelStyle());
            }

            // Slider (using float values for smooth sliding, then rounding to ints)
            EditorGUI.BeginChangeCheck();
            float tempMin = minProp.intValue;
            float tempMax = maxProp.intValue;
            EditorGUI.MinMaxSlider(sliderRect, ref tempMin, ref tempMax, rangeMin, rangeMax);
            if (EditorGUI.EndChangeCheck())
            {
                minProp.intValue = Mathf.RoundToInt(tempMin);
                maxProp.intValue = Mathf.RoundToInt(tempMax);
            }

            // Max field
            EditorGUI.BeginChangeCheck();
            int maxValue = EditorGUI.IntField(maxFieldRect, maxProp.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                maxProp.intValue = Mathf.Max(maxValue, minProp.intValue);
            }

            EditorGUI.EndProperty();
        }
    }
    #endif

    /// <summary>
    /// A serializable struct that represents a range of integer values with utility methods.
    /// Provides a min-max slider interface in the Unity Inspector when used with the MinMaxRangeAttribute.
    /// </summary>
    [System.Serializable]
    public struct RangedInt
    {
        /// <summary>
        /// The minimum value of the range.
        /// </summary>
        public int minValue;
        
        /// <summary>
        /// The maximum value of the range.
        /// </summary>
        public int maxValue;

        /// <summary>
        /// Initializes a new instance of the RangedInt struct.
        /// </summary>
        /// <param name="min">The minimum value of the range.</param>
        /// <param name="max">The maximum value of the range.</param>
        public RangedInt(int min, int max)
        {
            minValue = min;
            maxValue = max;
        }

        /// <summary>
        /// Implicitly converts an int value to a RangedInt with range from - value to +value.
        /// </summary>
        /// <param name="value">The value to convert (range will be - value to +value).</param>
        /// <returns>A RangedInt with range from - value to +value.</returns>
        public static implicit operator RangedInt(int value)
        {
            return new RangedInt(-value, value);
        }

        /// <summary>
        /// Gets a random value within the range (inclusive of both minValue and maxValue).
        /// </summary>
        public int RandomValue => Random.Range(minValue, maxValue + 1);
        
        /// <summary>
        /// Gets the size of the range (maxValue - minValue).
        /// </summary>
        public int Range => maxValue - minValue;
        
        
        /// <summary>
        /// Gets the average (middle) value of the range as a float.
        /// </summary>
        public float Average => (minValue + maxValue) * 0.5f;
        
        /// <summary>
        /// Linearly interpolates between minValue and maxValue, returning the result as an integer.
        /// </summary>
        /// <param name="t">The interpolation parameter (0 returns minValue, 1 returns maxValue).</param>
        /// <returns>The interpolated value between minValue and maxValue, rounded to the nearest integer.</returns>
        public int Lerp(float t) => Mathf.RoundToInt(Mathf.Lerp(minValue, maxValue, t));
        
        /// <summary>
        /// Checks if the specified value is within the range (inclusive).
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is within the range, false otherwise.</returns>
        public bool Contains(int value) => value >= minValue && value <= maxValue;
        
        /// <summary>
        /// Clamps the specified value to be within the range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <returns>The value clamped to be within minValue and maxValue.</returns>
        public int Clamp(int value) => Mathf.Clamp(value, minValue, maxValue);
        
        /// <summary>
        /// Returns a string representation of the range.
        /// </summary>
        /// <returns>A formatted string showing the range (min - max).</returns>
        public override string ToString() => $"({minValue} - {maxValue})";
    }
}