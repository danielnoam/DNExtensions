/*
 * RangedInt Custom Attribute
 * 
 * This attribute creates a min-max slider in the Unity Inspector for integer ranges.
 * 
 * Usage Examples:
 * 1. With MinMaxRange attribute (recommended):
 *    [MinMaxRange(-10, 10)] 
 *    public RangedInt spawnCount;
 * 
 * 2. With MinMaxRange and direct int initialization:
 *    [MinMaxRange(-10, 10)] 
 *    public RangedInt damage = 5; // Creates range from -10 to 10
 * 
 * 3. With direct value initialization (no attribute):
 *    public RangedInt health = new RangedInt(50, 100);
 * 
 * 4. Without any initialization (defaults to 0-1 range):
 *    public RangedInt defaultRange;
 * 
 * Available Functions:
 * - RandomValue: Get a random value within the range (inclusive)
 *     int random = myRange.RandomValue;
 * 
 * - Lerp: Interpolate within the range
 *     int interpolated = myRange.Lerp(0.5f);  // Get middle value
 * 
 * - Contains: Check if a value is within the range
 *     bool isInRange = myRange.Contains(value);
 * 
 * - Clamp: Force a value to be within the range
 *     int clamped = myRange.Clamp(value);
 * 
 * - Range: Get the size of the range
 *     int size = myRange.Range;
 * 
 * - Average: Get the middle value of the range (as float)
 *     float middle = myRange.Average;
 */

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
        // Cached style for better performance
        private static GUIStyle _labelStyle;
        private static GUIStyle GetLabelStyle()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.UpperCenter
                };
            }
            return _labelStyle;
        }
        
        // Customizable UI constants
        private const float FieldPadding = 5f;     // Spacing between UI elements
        private const float FieldWidth = 50f;      // Width of the min/max input fields
        private const float FieldHeight = 18f;     // Height of the min/max input fields
        private const int DefaultMinRange = 0;     // Default minimum range when no attribute is specified
        private const int DefaultMaxRange = 1;     // Default maximum range when no attribute is specified
        private const float RangePaddingPercent = 0.2f; // Padding percentage for dynamic range calculation
        
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
                if (minProp.intValue == 0 && maxProp.intValue == 0)
                {
                    rangeMin = DefaultMinRange;
                    rangeMax = DefaultMaxRange;
                }
                else
                {
                    int padding = Mathf.Max(1, Mathf.RoundToInt((maxProp.intValue - minProp.intValue) * RangePaddingPercent));
                    rangeMin = minProp.intValue - padding;
                    rangeMax = maxProp.intValue + padding;
                }
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
    
                EditorGUI.LabelField(labelRect, "Range " + rangeValue.ToString(), GetLabelStyle());
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

    [System.Serializable]
    public struct RangedInt
    {
        public int minValue;
        public int maxValue;

        // Constructor
        public RangedInt(int min, int max)
        {
            minValue = min;
            maxValue = max;
        }

        // Implicit conversion from int
        public static implicit operator RangedInt(int value)
        {
            return new RangedInt(-value, value);
        }

        // Utility properties
        public int RandomValue => Random.Range(minValue, maxValue + 1); // +1 for inclusive max
        public int Range => maxValue - minValue;
        public float Average => (minValue + maxValue) * 0.5f;
        public int Lerp(float t) => Mathf.RoundToInt(Mathf.Lerp(minValue, maxValue, t));
        public bool Contains(int value) => value >= minValue && value <= maxValue;
        public int Clamp(int value) => Mathf.Clamp(value, minValue, maxValue);
        public override string ToString() => $"({minValue} - {maxValue})";
    }
}