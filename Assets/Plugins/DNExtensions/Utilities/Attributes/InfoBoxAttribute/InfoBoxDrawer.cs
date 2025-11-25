#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace DNExtensions
{
    /// <summary>
    /// Custom property drawer for the InfoBox attribute.
    /// Draws an informational message box with appropriate styling based on the message type.
    /// </summary>
    [CustomPropertyDrawer(typeof(InfoBoxAttribute))]
    public class InfoBoxDrawer : DecoratorDrawer
    {
        private readonly Color _borderColor = Color.gray;
        private const float Padding = 4f;
        private const float IconSize = 16f;
        private const float IconSpacing = 4f;

        /// <summary>
        /// Gets the InfoBoxAttribute for this drawer.
        /// </summary>
        private InfoBoxAttribute InfoBoxAttribute => (InfoBoxAttribute)attribute;

        /// <summary>
        /// Calculates the height needed for the info box including padding and spacing.
        /// </summary>
        /// <returns>The total height in pixels</returns>
        public override float GetHeight()
        {
            float height = 0f;
            
            // Add spacing above if requested
            if (InfoBoxAttribute.AddSpacing)
            {
                height += EditorGUIUtility.standardVerticalSpacing * 2f;
            }
            
            // Calculate text height with word wrapping
            GUIStyle textStyle = GetTextStyle();
            float textWidth = EditorGUIUtility.currentViewWidth - (Padding * 2f) - IconSize - IconSpacing - 20f; // 20f for scrollbar
            float textHeight = textStyle.CalcHeight(new GUIContent(InfoBoxAttribute.Message), textWidth);
            
            // Info box height = padding + max(icon height, text height) + padding
            height += Padding;
            height += Mathf.Max(IconSize, textHeight);
            height += Padding;
            
            // Add spacing below
            height += EditorGUIUtility.standardVerticalSpacing;
            
            return height;
        }

        /// <summary>
        /// Draws the info box with appropriate styling and icon in the inspector.
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the info box</param>
        public override void OnGUI(Rect position)
        {
            float currentY = position.y;
            
            // Add spacing above if requested
            if (InfoBoxAttribute.AddSpacing)
            {
                currentY += EditorGUIUtility.standardVerticalSpacing * 2f;
            }
            
            // Calculate box rect
            float boxHeight = GetHeight() - (InfoBoxAttribute.AddSpacing ? EditorGUIUtility.standardVerticalSpacing * 2f : 0f) - EditorGUIUtility.standardVerticalSpacing;
            Rect boxRect = new Rect(position.x, currentY, position.width, boxHeight);
            
            // Draw the background box
            DrawInfoBox(boxRect, InfoBoxAttribute.Type);
            
            // Draw icon and text
            DrawContent(boxRect, InfoBoxAttribute.Message, InfoBoxAttribute.Type);
        }

        /// <summary>
        /// Draws the background box with appropriate color based on the info type.
        /// </summary>
        /// <param name="rect">The rectangle to draw the box in</param>
        /// <param name="type">The type of info box</param>
        private void DrawInfoBox(Rect rect, InfoBoxType type)
        {
            Color backgroundColor = GetBackgroundColor(type);
            
            // Draw background
            EditorGUI.DrawRect(rect, backgroundColor);
            
            // Draw border
            Rect borderRect = new Rect(rect.x, rect.y, rect.width, 1f);
            EditorGUI.DrawRect(borderRect, _borderColor); // Top
            borderRect.y = rect.y + rect.height - 1f;
            EditorGUI.DrawRect(borderRect, _borderColor); // Bottom
            borderRect = new Rect(rect.x, rect.y, 1f, rect.height);
            EditorGUI.DrawRect(borderRect, _borderColor); // Left
            borderRect.x = rect.x + rect.width - 1f;
            EditorGUI.DrawRect(borderRect, _borderColor); // Right
        }

        /// <summary>
        /// Draws the icon and text content inside the info box.
        /// </summary>
        /// <param name="rect">The rectangle of the info box</param>
        /// <param name="message">The message text to display</param>
        /// <param name="type">The type of info box</param>
        private void DrawContent(Rect rect, string message, InfoBoxType type)
        {
            // Icon rect
            Rect iconRect = new Rect(
                rect.x + Padding, 
                rect.y + Padding, 
                IconSize, 
                IconSize
            );
            
            // Text rect
            Rect textRect = new Rect(
                rect.x + Padding + IconSize + IconSpacing,
                rect.y + Padding,
                rect.width - (Padding * 2f) - IconSize - IconSpacing,
                rect.height - (Padding * 2f)
            );
            
            // Draw icon
            DrawIcon(iconRect, type);
            
            // Draw text
            GUIStyle textStyle = GetTextStyle();
            Color originalColor = GUI.color;
            GUI.color = GetTextColor(type);
            EditorGUI.LabelField(textRect, message, textStyle);
            GUI.color = originalColor;
        }

        /// <summary>
        /// Draws the appropriate icon for the info box type.
        /// </summary>
        /// <param name="rect">The rectangle to draw the icon in</param>
        /// <param name="type">The type of info box</param>
        private void DrawIcon(Rect rect, InfoBoxType type)
        {
            string icon = GetIconString(type);
            Color iconColor = GetIconColor(type);
            
            Color originalColor = GUI.color;
            GUI.color = iconColor;
            
            GUIStyle iconStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            
            EditorGUI.LabelField(rect, new GUIContent(icon), iconStyle);
            GUI.color = originalColor;
        }

        /// <summary>
        /// Gets the text style for the info box message.
        /// </summary>
        /// <returns>GUIStyle for the text</returns>
        private GUIStyle GetTextStyle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11
            };
        }

        /// <summary>
        /// Gets the background color for the specified info box type.
        /// </summary>
        /// <param name="type">The info box type</param>
        /// <returns>Background color</returns>
        private Color GetBackgroundColor(InfoBoxType type)
        {
            return type switch
            {
                InfoBoxType.Info => new Color(0.4f, 0.4f, 0.4f, 0.3f),   
                InfoBoxType.Warning => new Color(1f, 0.9f, 0.6f, 0.5f),   // Light yellow
                InfoBoxType.Error => new Color(1f, 0.8f, 0.8f, 0.5f),     // Light red
                InfoBoxType.Success => new Color(0.8f, 1f, 0.8f, 0.5f),   // Light green
                _ => new Color(0.9f, 0.9f, 0.9f, 0.3f)                    // Default gray
            };
        }
        

        /// <summary>
        /// Gets the text color for the specified info box type.
        /// </summary>
        /// <param name="type">The info box type</param>
        /// <returns>Text color</returns>
        private Color GetTextColor(InfoBoxType type)
        {
            return type switch
            {
                InfoBoxType.Info => new Color(0.8f, 0.8f, 0.8f, 1f),     
                InfoBoxType.Warning => new Color(0.8f, 0.5f, 0f, 1f),     // Dark orange
                InfoBoxType.Error => new Color(0.8f, 0.2f, 0.2f, 1f),     // Dark red
                InfoBoxType.Success => new Color(0.2f, 0.6f, 0.2f, 1f),   // Dark green
                _ => Color.black                                           // Default black
            };
        }

        /// <summary>
        /// Gets the icon color for the specified info box type.
        /// </summary>
        /// <param name="type">The info box type</param>
        /// <returns>Icon color</returns>
        private Color GetIconColor(InfoBoxType type)
        {
            return GetTextColor(type);
        }

        /// <summary>
        /// Gets the unicode icon string for the specified info box type.
        /// </summary>
        /// <param name="type">The info box type</param>
        /// <returns>Unicode icon string</returns>
        private string GetIconString(InfoBoxType type)
        {
            return type switch
            {
                InfoBoxType.Info => "ℹ",       // Info symbol
                InfoBoxType.Warning => "⚠",    // Warning symbol
                InfoBoxType.Error => "✕",      // Error symbol
                InfoBoxType.Success => "✓",    // Success symbol
                _ => "•"                        // Default bullet
            };
        }
    }
}
#endif