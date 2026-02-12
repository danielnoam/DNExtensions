#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
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
        
        private const float FieldPadding = 5f;
        private const float FieldWidth = 50f;
        private const float FieldHeight = 18f;
        private const int DefaultMinRange = -1;
        private const int DefaultMaxRange = 1;
        private const bool ShowRangeValue = true;
        private const float LabelYOffset = 15f;

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

            Rect minFieldRect = new Rect(position.x, position.y, FieldWidth, FieldHeight);
            Rect sliderRect = new Rect(minFieldRect.xMax + FieldPadding, position.y, 
                position.width - (FieldWidth * 2) - (FieldPadding * 2), FieldHeight);
            Rect maxFieldRect = new Rect(sliderRect.xMax + FieldPadding, position.y, FieldWidth, FieldHeight);

            EditorGUI.BeginChangeCheck();
            int minValue = EditorGUI.IntField(minFieldRect, minProp.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                minProp.intValue = Mathf.Min(minValue, maxProp.intValue);
            }

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

            EditorGUI.BeginChangeCheck();
            float tempMin = minProp.intValue;
            float tempMax = maxProp.intValue;
            EditorGUI.MinMaxSlider(sliderRect, ref tempMin, ref tempMax, rangeMin, rangeMax);
            if (EditorGUI.EndChangeCheck())
            {
                minProp.intValue = Mathf.RoundToInt(tempMin);
                maxProp.intValue = Mathf.RoundToInt(tempMax);
            }

            EditorGUI.BeginChangeCheck();
            int maxValue = EditorGUI.IntField(maxFieldRect, maxProp.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                maxProp.intValue = Mathf.Max(maxValue, minProp.intValue);
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif