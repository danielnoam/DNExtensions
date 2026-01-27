using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DNExtensions.Utilities.RangedValues
{
    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(RangedFloat), true)]
    public class RangedFloatDrawer : PropertyDrawer
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
        private const float FieldHeight = 18f;    // Height of the min/max input fields
        private const float DefaultMinRange = -1f; // Default minimum range when no attribute is specified
        private const float DefaultMaxRange = 1f;  // Default maximum range when no attribute is specified
        
        // Range label settings
        private const bool ShowRangeValue = true;  // Toggle to show/hide the range value
        private const float LabelYOffset = 15f;   // How far above the slider to show the label
        private const int DecimalPlaces = 1;        // Decimal places for range value


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
            float rangeMin, rangeMax;
            var ranges = (MinMaxRangeAttribute[])fieldInfo.GetCustomAttributes(typeof(MinMaxRangeAttribute), true);
            if (ranges.Length > 0)
            {
                rangeMin = ranges[0].Min;
                rangeMax = ranges[0].Max;
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
            float minValue = EditorGUI.FloatField(minFieldRect, minProp.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                minProp.floatValue = Mathf.Min(minValue, maxProp.floatValue);
            }

            // Draw range value above slider if enabled
            if (ShowRangeValue)
            {
                float rangeValue = maxProp.floatValue - minProp.floatValue;
    
                Rect labelRect = new Rect(
                    sliderRect.x, 
                    sliderRect.y + LabelYOffset, 
                    sliderRect.width, 
                    20
                );
    
                EditorGUI.LabelField(labelRect, "Range " + rangeValue.ToString($"F{DecimalPlaces}"), GetLabelStyle());
            }

            // Slider
            EditorGUI.BeginChangeCheck();
            float tempMin = minProp.floatValue;
            float tempMax = maxProp.floatValue;
            EditorGUI.MinMaxSlider(sliderRect, ref tempMin, ref tempMax, rangeMin, rangeMax);
            if (EditorGUI.EndChangeCheck())
            {
                minProp.floatValue = tempMin;
                maxProp.floatValue = tempMax;
            }

            // Max field
            EditorGUI.BeginChangeCheck();
            float maxValue = EditorGUI.FloatField(maxFieldRect, maxProp.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                maxProp.floatValue = Mathf.Max(maxValue, minProp.floatValue);
            }

            EditorGUI.EndProperty();
        }
    }
    #endif
    
    /// <summary>
    /// A serializable struct that represents a range of float values with utility methods.
    /// Provides a min-max slider interface in the Unity Inspector when used with the MinMaxRangeAttribute.
    /// </summary>
    [System.Serializable]
    public struct RangedFloat
    {
        /// <summary>
        /// The minimum value of the range.
        /// </summary>
        public float minValue;
        
        /// <summary>
        /// The maximum value of the range.
        /// </summary>
        public float maxValue;

        /// <summary>
        /// Initializes a new instance of the RangedFloat struct.
        /// </summary>
        /// <param name="min">The minimum value of the range.</param>
        /// <param name="max">The maximum value of the range.</param>
        public RangedFloat(float min, float max)
        {
            minValue = min;
            maxValue = max;
        }

        /// <summary>
        /// Implicitly converts a float value to a RangedFloat with range from - value to +value.
        /// </summary>
        /// <param name="value">The value to convert (range will be - value to +value).</param>
        /// <returns>A RangedFloat with range from - value to +value.</returns>
        public static implicit operator RangedFloat(float value)
        {
            return new RangedFloat(-value, value);
        }
        
        /// <summary>
        /// Gets a random value within the range (inclusive of minValue, exclusive of maxValue).
        /// </summary>
        public float RandomValue => Random.Range(minValue, maxValue);
        
        /// <summary>
        /// Gets the size of the range (maxValue - minValue).
        /// </summary>
        public float Range => maxValue - minValue;
        
        /// <summary>
        /// Gets the average (middle) value of the range.
        /// </summary>
        public float Average => (minValue + maxValue) * 0.5f;
        
        /// <summary>
        /// Linearly interpolates between minValue and maxValue.
        /// </summary>
        /// <param name="t">The interpolation parameter (0 returns minValue, 1 returns maxValue).</param>
        /// <returns>The interpolated value between minValue and maxValue.</returns>
        public float Lerp(float t) => Mathf.Lerp(minValue, maxValue, t);
        
        /// <summary>
        /// Checks if the specified value is within the range (inclusive).
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is within the range, false otherwise.</returns>
        public bool Contains(float value) => value >= minValue && value <= maxValue;
        
        /// <summary>
        /// Clamps the specified value to be within the range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <returns>The value clamped to be within minValue and maxValue.</returns>
        public float Clamp(float value) => Mathf.Clamp(value, minValue, maxValue);
        
        /// <summary>
        /// Returns a string representation of the range.
        /// </summary>
        /// <returns>A formatted string showing the range (min - max).</returns>
        public override string ToString() => $"({minValue:F2} - {maxValue:F2})";
    }
}