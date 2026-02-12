#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
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
        
        private const float FieldPadding = 5f;
        private const float FieldWidth = 50f;
        private const float FieldHeight = 18f;
        private const float DefaultMinRange = -1f;
        private const float DefaultMaxRange = 1f;
        private const bool ShowRangeValue = true;
        private const float LabelYOffset = 15f;
        private const int DecimalPlaces = 1;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return ShowRangeValue ? FieldHeight + 15f : FieldHeight;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            position = EditorGUI.PrefixLabel(position, label);

            SerializedProperty minProp = property.FindPropertyRelative("minValue");
            SerializedProperty maxProp = property.FindPropertyRelative("maxValue");

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

            Rect minFieldRect = new Rect(position.x, position.y, FieldWidth, FieldHeight);
            Rect sliderRect = new Rect(minFieldRect.xMax + FieldPadding, position.y, 
                position.width - (FieldWidth * 2) - (FieldPadding * 2), FieldHeight);
            Rect maxFieldRect = new Rect(sliderRect.xMax + FieldPadding, position.y, FieldWidth, FieldHeight);

            EditorGUI.BeginChangeCheck();
            float minValue = EditorGUI.FloatField(minFieldRect, minProp.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                minProp.floatValue = Mathf.Min(minValue, maxProp.floatValue);
            }

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

            EditorGUI.BeginChangeCheck();
            float tempMin = minProp.floatValue;
            float tempMax = maxProp.floatValue;
            EditorGUI.MinMaxSlider(sliderRect, ref tempMin, ref tempMax, rangeMin, rangeMax);
            if (EditorGUI.EndChangeCheck())
            {
                minProp.floatValue = tempMin;
                maxProp.floatValue = tempMax;
            }

            EditorGUI.BeginChangeCheck();
            float maxValue = EditorGUI.FloatField(maxFieldRect, maxProp.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                maxProp.floatValue = Mathf.Max(maxValue, minProp.floatValue);
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif