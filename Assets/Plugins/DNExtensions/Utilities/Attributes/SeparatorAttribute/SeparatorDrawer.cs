#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace DNExtensions
{
    /// <summary>
    /// Custom property drawer for the Separator attribute.
    /// Draws a visual separator line with optional title text above the property.
    /// </summary>
    [CustomPropertyDrawer(typeof(SeparatorAttribute))]
    public class SeparatorDrawer : DecoratorDrawer
    {
        // Visual constants - easily editable
        private static readonly Color LineColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        private const float LineThickness = 4f;
        private const float LineTopSpacingMultiplier = 3f;
        private const float LineBottomSpacingMultiplier = 2f;
        
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
            
            // Add spacing above if requested
            if (SeparatorAttribute.AddSpacing)
            {
                height += EditorGUIUtility.standardVerticalSpacing * LineTopSpacingMultiplier;
            }
            
            // Add height for title if present
            if (!string.IsNullOrEmpty(SeparatorAttribute.Title))
            {
                height += EditorGUIUtility.singleLineHeight;
                height += EditorGUIUtility.standardVerticalSpacing;
            }
            
            // Add height for separator line
            height += LineThickness;
            
            // Add spacing below
            height += EditorGUIUtility.standardVerticalSpacing * LineBottomSpacingMultiplier;
            
            return height;
        }

        /// <summary>
        /// Draws the separator line and optional title in the inspector.
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the separator</param>
        public override void OnGUI(Rect position)
        {
            float currentY = position.y;
            
            // Add spacing above if requested
            if (SeparatorAttribute.AddSpacing)
            {
                currentY += EditorGUIUtility.standardVerticalSpacing * LineTopSpacingMultiplier;
            }
            
            // Draw title if present
            if (!string.IsNullOrEmpty(SeparatorAttribute.Title))
            {
                Rect titleRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                
                GUIStyle titleStyle = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = SeparatorAttribute.TitleStyle,
                    alignment = TextAnchor.MiddleLeft
                };
                
                // Use default Unity text color for title
                EditorGUI.LabelField(titleRect, SeparatorAttribute.Title, titleStyle);
                
                currentY += EditorGUIUtility.singleLineHeight;
                currentY += EditorGUIUtility.standardVerticalSpacing;
            }
            
            // Draw separator line
            Rect lineRect = new Rect(
                position.x, 
                currentY, 
                position.width, 
                LineThickness
            );
            
            DrawSeparatorLine(lineRect, LineColor);
        }

        /// <summary>
        /// Draws a horizontal separator line with the specified color.
        /// </summary>
        /// <param name="rect">The rectangle to draw the line in</param>
        /// <param name="color">The color of the line</param>
        private void DrawSeparatorLine(Rect rect, Color color)
        {
            Color originalColor = GUI.color;
            GUI.color = color;
            
            // Draw the line using GUI.DrawTexture with a white texture
            EditorGUI.DrawRect(rect, color);
            
            GUI.color = originalColor;
        }
    }
}
#endif