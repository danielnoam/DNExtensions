#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Custom property drawer for the Separator attribute.
    /// Draws a visual separator line with optional title text centered on the line.
    /// </summary>
    [CustomPropertyDrawer(typeof(SeparatorAttribute))]
    public class SeparatorDrawer : DecoratorDrawer
    {
        private static readonly Color LineColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        private const float LineThickness = 2f;
        private const float LineTopSpacingMultiplier = 3f;
        private const float LineBottomSpacingMultiplier = 2f;
        private const float TitlePadding = 8f;
        
        /// <summary>
        /// Gets the SeparatorAttribute for this drawer.
        /// </summary>
        private SeparatorAttribute SeparatorAttribute => (SeparatorAttribute)attribute;

        /// <summary>
        /// Calculates the height needed for the separator including title and spacing.
        /// </summary>
        /// <returns>The total height in pixels</returns>
        public override float GetHeight()
        {
            float height = 0f;
            
            if (SeparatorAttribute.AddSpacing)
            {
                height += EditorGUIUtility.standardVerticalSpacing * LineTopSpacingMultiplier;
            }
            
            if (!string.IsNullOrEmpty(SeparatorAttribute.Title))
            {
                height += EditorGUIUtility.singleLineHeight;
            }
            else
            {
                height += LineThickness;
            }
            
            height += EditorGUIUtility.standardVerticalSpacing * LineBottomSpacingMultiplier;
            
            return height;
        }

        /// <summary>
        /// Draws the separator line with optional title centered on the line.
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the separator</param>
        public override void OnGUI(Rect position)
        {
            float currentY = position.y;
            
            if (SeparatorAttribute.AddSpacing)
            {
                currentY += EditorGUIUtility.standardVerticalSpacing * LineTopSpacingMultiplier;
            }
            
            if (!string.IsNullOrEmpty(SeparatorAttribute.Title))
            {
                GUIStyle titleStyle = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = SeparatorAttribute.TitleStyle,
                    alignment = TextAnchor.MiddleCenter
                };
                
                GUIContent titleContent = new GUIContent(SeparatorAttribute.Title);
                Vector2 titleSize = titleStyle.CalcSize(titleContent);
                
                float lineCenterY = currentY + EditorGUIUtility.singleLineHeight * 0.5f;
                
                float leftLineWidth = (position.width - titleSize.x - TitlePadding * 2f) * 0.5f;
                float rightLineStart = position.x + leftLineWidth + titleSize.x + TitlePadding * 2f;
                float rightLineWidth = position.width - leftLineWidth - titleSize.x - TitlePadding * 2f;
                
                Rect leftLineRect = new Rect(
                    position.x,
                    lineCenterY - LineThickness * 0.5f,
                    leftLineWidth,
                    LineThickness
                );
                
                Rect rightLineRect = new Rect(
                    rightLineStart,
                    lineCenterY - LineThickness * 0.5f,
                    rightLineWidth,
                    LineThickness
                );
                
                EditorGUI.DrawRect(leftLineRect, LineColor);
                EditorGUI.DrawRect(rightLineRect, LineColor);
                
                Rect titleRect = new Rect(
                    position.x + leftLineWidth + TitlePadding,
                    currentY,
                    titleSize.x,
                    EditorGUIUtility.singleLineHeight
                );
                
                EditorGUI.LabelField(titleRect, SeparatorAttribute.Title, titleStyle);
            }
            else
            {
                Rect lineRect = new Rect(
                    position.x,
                    currentY,
                    position.width,
                    LineThickness
                );
                
                EditorGUI.DrawRect(lineRect, LineColor);
            }
        }
    }
}
#endif