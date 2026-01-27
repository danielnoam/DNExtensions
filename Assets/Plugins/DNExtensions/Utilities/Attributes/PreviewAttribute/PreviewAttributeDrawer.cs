#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Custom property drawer for the Preview attribute that displays thumbnails of sprites, textures, and prefabs.
    /// Provides visual feedback in the Unity Inspector with customizable preview size and styling.
    /// </summary>
    [CustomPropertyDrawer(typeof(PreviewAttribute))]
    public class PreviewAttributeDrawer : PropertyDrawer
    {
        private const float SpacingBetweenFieldAndPreview = 4f;
        private const float LabelHeight = 16f;
        private const float DetailsHeight = 32f;
        
        /// <summary>
        /// Gets the Preview attribute for this drawer.
        /// </summary>
        private PreviewAttribute PreviewAttribute => (PreviewAttribute)attribute;

        /// <summary>
        /// Calculates the total height needed for the property including preview area.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight; // Base field height
            
            bool shouldShowPreview = ShouldShowPreview(property);
            
            if (shouldShowPreview)
            {
                totalHeight += SpacingBetweenFieldAndPreview;
                totalHeight += PreviewAttribute.Size;
                
                if (PreviewAttribute.ShowLabel)
                    totalHeight += LabelHeight;
                    
                if (PreviewAttribute.ShowDetails)
                    totalHeight += DetailsHeight;
            }

            return totalHeight;
        }

        /// <summary>
        /// Draws the field and preview in the inspector.
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw the main field
            Rect fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(fieldRect, property, label);

            bool shouldShowPreview = ShouldShowPreview(property);

            if (shouldShowPreview)
            {
                DrawPreview(position, property, fieldRect);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Determines whether a preview should be shown for the given property.
        /// </summary>
        private bool ShouldShowPreview(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    Object targetObject = property.objectReferenceValue;
                    if (targetObject == null)
                        return PreviewAttribute.ShowWhenEmpty;
                    return IsPreviewableType(targetObject);
                    
                case SerializedPropertyType.Gradient:
                    return true;
                    
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Vector3Int:
                    return true;
                    
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if the object type can be previewed.
        /// </summary>
        private bool IsPreviewableType(Object obj)
        {
            if (obj == null) return false;
            
            return obj is Sprite || 
                   obj is Texture2D || 
                   obj is Texture || 
                   obj is GameObject ||
                   (obj is MonoScript && AssetDatabase.GetAssetPath(obj).EndsWith(".prefab"));
        }

        /// <summary>
        /// Draws the preview area below the field.
        /// </summary>
        private void DrawPreview(Rect position, SerializedProperty property, Rect fieldRect)
        {
            float currentY = fieldRect.yMax + SpacingBetweenFieldAndPreview;
            
            // Calculate preview area
            float previewSize = PreviewAttribute.Size;
            Rect previewRect = new Rect(
                position.x + EditorGUIUtility.labelWidth, 
                currentY, 
                previewSize, 
                previewSize);

            // Draw background
            DrawPreviewBackground(previewRect);

            // Draw the appropriate preview based on property type
            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    DrawObjectPreview(previewRect, property.objectReferenceValue);
                    break;
                    
                case SerializedPropertyType.Gradient:
                    DrawGradientPreview(previewRect, property.gradientValue);
                    break;
                    
                case SerializedPropertyType.Vector2:
                    DrawVectorPreview(previewRect, property.vector2Value);
                    break;
                    
                case SerializedPropertyType.Vector3:
                    DrawVectorPreview(previewRect, property.vector3Value);
                    break;
                    
                case SerializedPropertyType.Vector4:
                    DrawVectorPreview(previewRect, property.vector4Value);
                    break;
                    
                case SerializedPropertyType.Vector2Int:
                    DrawVectorPreview(previewRect, new Vector2(property.vector2IntValue.x, property.vector2IntValue.y));
                    break;
                    
                case SerializedPropertyType.Vector3Int:
                    DrawVectorPreview(previewRect, new Vector3(property.vector3IntValue.x, property.vector3IntValue.y, property.vector3IntValue.z));
                    break;
            }
            
            // Draw label if requested
            if (PreviewAttribute.ShowLabel)
            {
                Rect labelRect = new Rect(previewRect.x, previewRect.yMax + 2, previewRect.width, LabelHeight);
                DrawPreviewLabel(labelRect, property);
            }
            
            // Draw details if requested
            if (PreviewAttribute.ShowDetails)
            {
                float detailY = PreviewAttribute.ShowLabel ? 
                    previewRect.yMax + LabelHeight + 4 : 
                    previewRect.yMax + 4;
                Rect detailRect = new Rect(previewRect.x, detailY, position.width - EditorGUIUtility.labelWidth, DetailsHeight);
                DrawPreviewDetails(detailRect, property);
            }
        }

        /// <summary>
        /// Draws the background for the preview area.
        /// </summary>
        private void DrawPreviewBackground(Rect rect)
        {
            Color previousColor = GUI.color;
            GUI.color = PreviewAttribute.BackgroundColor;
            EditorGUI.DrawRect(rect, PreviewAttribute.BackgroundColor);
            GUI.color = previousColor;
            
            // Draw border
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), Color.gray);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), Color.gray);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), Color.gray);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), Color.gray);
        }

        /// <summary>
        /// Draws the preview for the assigned object.
        /// </summary>
        private void DrawObjectPreview(Rect rect, Object obj)
        {
            Texture2D previewTexture = GetPreviewTexture(obj);
            
            if (previewTexture != null)
            {
                // Calculate aspect ratio to fit within the preview rect
                float aspectRatio = (float)previewTexture.width / previewTexture.height;
                Rect textureRect = GetAspectRect(rect, aspectRatio);
                
                GUI.DrawTexture(textureRect, previewTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                // Draw default Unity asset preview
                Texture2D assetPreview = AssetPreview.GetAssetPreview(obj);
                if (assetPreview != null)
                {
                    float aspectRatio = (float)assetPreview.width / assetPreview.height;
                    Rect textureRect = GetAspectRect(rect, aspectRatio);
                    GUI.DrawTexture(textureRect, assetPreview, ScaleMode.ScaleToFit);
                }
                else
                {
                    // Show asset icon as fallback
                    Texture2D icon = AssetPreview.GetMiniThumbnail(obj);
                    if (icon != null)
                    {
                        Rect iconRect = new Rect(
                            rect.center.x - 16, 
                            rect.center.y - 16, 
                            32, 32);
                        GUI.DrawTexture(iconRect, icon);
                    }
                }
            }
        }

        /// <summary>
        /// Gets an appropriate preview texture for the object.
        /// </summary>
        private Texture2D GetPreviewTexture(Object obj)
        {
            switch (obj)
            {
                case Sprite sprite:
                    return sprite.texture;
                case Texture2D texture2D:
                    return texture2D;
                case Texture texture:
                    return texture as Texture2D;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Calculates a rect that maintains aspect ratio within the given bounds.
        /// </summary>
        private Rect GetAspectRect(Rect bounds, float aspectRatio)
        {
            float boundsAspect = bounds.width / bounds.height;
            
            if (aspectRatio > boundsAspect)
            {
                // Wider than bounds - fit to width
                float height = bounds.width / aspectRatio;
                return new Rect(
                    bounds.x,
                    bounds.y + (bounds.height - height) * 0.5f,
                    bounds.width,
                    height);
            }
            else
            {
                // Taller than bounds - fit to height
                float width = bounds.height * aspectRatio;
                return new Rect(
                    bounds.x + (bounds.width - width) * 0.5f,
                    bounds.y,
                    width,
                    bounds.height);
            }
        }

        /// <summary>
        /// Draws a label showing the field information.
        /// </summary>
        private void DrawPreviewLabel(Rect rect, SerializedProperty property)
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            
            string labelText = GetPropertyLabelText(property);
            EditorGUI.LabelField(rect, labelText, labelStyle);
        }

        /// <summary>
        /// Draws detailed information about the field.
        /// </summary>
        private void DrawPreviewDetails(Rect rect, SerializedProperty property)
        {
            string details = GetPropertyDetails(property);
            
            GUIStyle detailStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                wordWrap = true
            };
            
            EditorGUI.LabelField(rect, details, detailStyle);
        }

        /// <summary>
        /// Gets appropriate label text for the property.
        /// </summary>
        private string GetPropertyLabelText(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue != null ? property.objectReferenceValue.name : "None";
                case SerializedPropertyType.Gradient:
                    return "Gradient";
                case SerializedPropertyType.Vector2:
                    return $"Vector2 ({property.vector2Value.x:F1}, {property.vector2Value.y:F1})";
                case SerializedPropertyType.Vector3:
                    return $"Vector3 ({property.vector3Value.x:F1}, {property.vector3Value.y:F1}, {property.vector3Value.z:F1})";
                case SerializedPropertyType.Vector4:
                    return $"Vector4 ({property.vector4Value.x:F1}, {property.vector4Value.y:F1}, {property.vector4Value.z:F1}, {property.vector4Value.w:F1})";
                case SerializedPropertyType.Vector2Int:
                    return $"Vector2Int ({property.vector2IntValue.x}, {property.vector2IntValue.y})";
                case SerializedPropertyType.Vector3Int:
                    return $"Vector3Int ({property.vector3IntValue.x}, {property.vector3IntValue.y}, {property.vector3IntValue.z})";
                default:
                    return property.displayName;
            }
        }

        /// <summary>
        /// Gets detailed information string for the property.
        /// </summary>
        private string GetPropertyDetails(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    return GetAssetDetails(property.objectReferenceValue);
                    
                case SerializedPropertyType.Gradient:
                    var gradient = property.gradientValue;
                    return $"Type: Gradient\nKeys: {gradient.colorKeys.Length} color, {gradient.alphaKeys.Length} alpha\nMode: {gradient.mode}";
                    
                case SerializedPropertyType.Vector2:
                    var v2 = property.vector2Value;
                    return $"Type: Vector2\nMagnitude: {v2.magnitude:F3}\nNormalized: ({v2.normalized.x:F2}, {v2.normalized.y:F2})";
                    
                case SerializedPropertyType.Vector3:
                    var v3 = property.vector3Value;
                    return $"Type: Vector3\nMagnitude: {v3.magnitude:F3}\nNormalized: ({v3.normalized.x:F2}, {v3.normalized.y:F2}, {v3.normalized.z:F2})";
                    
                case SerializedPropertyType.Vector4:
                    var v4 = property.vector4Value;
                    return $"Type: Vector4\nMagnitude: {v4.magnitude:F3}\nSum: {(v4.x + v4.y + v4.z + v4.w):F3}";
                    
                case SerializedPropertyType.Vector2Int:
                    var v2i = property.vector2IntValue;
                    return $"Type: Vector2Int\nMagnitude: {v2i.magnitude:F3}";
                    
                case SerializedPropertyType.Vector3Int:
                    var v3i = property.vector3IntValue;
                    return $"Type: Vector3Int\nMagnitude: {v3i.magnitude:F3}";
                    
                default:
                    return $"Type: {property.propertyType}";
            }
        }

        /// <summary>
        /// Draws a placeholder when no object is assigned.
        /// </summary>
        private void DrawEmptyPreview(Rect rect)
        {
            GUIStyle emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUI.LabelField(rect, "No Preview\nAvailable", emptyStyle);
        }

        /// <summary>
        /// Gets detailed information string for the asset.
        /// </summary>
        private string GetAssetDetails(Object obj)
        {
            string details = $"Type: {obj.GetType().Name}";
            
            switch (obj)
            {
                case Sprite sprite:
                    details += $"\nSize: {sprite.rect.width}x{sprite.rect.height}";
                    if (sprite.texture != null)
                        details += $"\nTexture: {sprite.texture.width}x{sprite.texture.height}";
                    break;
                    
                case Texture2D texture2D:
                    details += $"\nSize: {texture2D.width}x{texture2D.height}";
                    details += $"\nFormat: {texture2D.format}";
                    break;
                    
                case GameObject prefab:
                    var components = prefab.GetComponents<Component>();
                    details += $"\nComponents: {components.Length}";
                    break;
            }
            
            // Add file size
            string assetPath = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(assetPath) && File.Exists(assetPath))
            {
                FileInfo fileInfo = new FileInfo(assetPath);
                details += $"\nSize: {FormatFileSize(fileInfo.Length)}";
            }
            
            return details;
        }

        /// <summary>
        /// Formats file size in human-readable format.
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Draws a preview for gradient values.
        /// </summary>
        private void DrawGradientPreview(Rect rect, Gradient gradient)
        {
            if (gradient == null)
            {
                DrawEmptyPreview(rect);
                return;
            }

            // Use Unity's built-in gradient preview if available
            if (Event.current.type == EventType.Repaint)
            {
                // Create a simple gradient representation using GUI.DrawTexture with a solid color approach
                int steps = Mathf.RoundToInt(rect.width);
                float stepWidth = rect.width / steps;
                
                for (int i = 0; i < steps; i++)
                {
                    float normalizedX = (float)i / (steps - 1);
                    Color gradientColor = gradient.Evaluate(normalizedX);
                    
                    Rect stepRect = new Rect(rect.x + i * stepWidth, rect.y, stepWidth + 1, rect.height);
                    EditorGUI.DrawRect(stepRect, gradientColor);
                }
            }
        }

        /// <summary>
        /// Draws a visual representation of vector values.
        /// </summary>
        private void DrawVectorPreview(Rect rect, Vector2 vector2)
        {
            DrawVectorPreview(rect, new Vector3(vector2.x, vector2.y, 0f));
        }

        /// <summary>
        /// Draws a visual representation of vector values.
        /// </summary>
        private void DrawVectorPreview(Rect rect, Vector3 vector)
        {
            Vector3 center = new Vector3(rect.center.x, rect.center.y, 0);
            float scale = Mathf.Min(rect.width, rect.height) * 0.4f;
            
            // Normalize vector for display (but keep original magnitude for color)
            float magnitude = vector.magnitude;
            Vector3 normalizedVector = magnitude > 0.001f ? vector.normalized : Vector3.zero;
            
            // Calculate end point
            Vector3 endPoint = center + new Vector3(
                normalizedVector.x * scale, 
                -normalizedVector.y * scale, // Flip Y for GUI coordinates
                0);
            
            // Color based on magnitude
            Color arrowColor = GetVectorColor(magnitude);
            
            // Draw coordinate system (faint grid)
            DrawVectorGrid(rect, center, scale);
            
            // Draw vector arrow
            if (magnitude > 0.001f)
            {
                DrawArrow(center, endPoint, arrowColor, 2f);
            }
            else
            {
                // Draw a small dot at origin for zero vectors
                DrawCircle(center, 3f, Color.gray);
            }
            
            // Draw magnitude text
            string magnitudeText = magnitude.ToString("F2");
            GUIStyle textStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.LowerRight,
                normal = { textColor = Color.white }
            };
            
            Rect textRect = new Rect(rect.x + 2, rect.yMax - 14, rect.width - 4, 12);
            GUI.Label(textRect, magnitudeText, textStyle);
        }

        /// <summary>
        /// Draws a visual representation of Vector4 values.
        /// </summary>
        private void DrawVectorPreview(Rect rect, Vector4 vector4)
        {
            // For Vector4, we'll show it as RGBA color bars or as XY + ZW components
            if (IsLikelyColorVector(vector4))
            {
                DrawVector4AsColor(rect, vector4);
            }
            else
            {
                DrawVector4AsComponents(rect, vector4);
            }
        }

        /// <summary>
        /// Determines if a Vector4 is likely representing a color (values between 0-1).
        /// </summary>
        private bool IsLikelyColorVector(Vector4 vector)
        {
            return vector.x >= 0f && vector.x <= 1f &&
                   vector.y >= 0f && vector.y <= 1f &&
                   vector.z >= 0f && vector.z <= 1f &&
                   vector.w >= 0f && vector.w <= 1f;
        }

        /// <summary>
        /// Draws Vector4 as a color preview.
        /// </summary>
        private void DrawVector4AsColor(Rect rect, Vector4 vector)
        {
            Color color = new Color(vector.x, vector.y, vector.z, vector.w);
            
            // Draw color swatch
            Rect colorRect = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8);
            EditorGUI.DrawRect(colorRect, color);
            
            // Draw border
            EditorGUI.DrawRect(new Rect(colorRect.x - 1, colorRect.y - 1, colorRect.width + 2, colorRect.height + 2), Color.black);
        }

        /// <summary>
        /// Draws Vector4 as component bars.
        /// </summary>
        private void DrawVector4AsComponents(Rect rect, Vector4 vector)
        {
            string[] labels = { "X", "Y", "Z", "W" };
            float[] values = { vector.x, vector.y, vector.z, vector.w };
            Color[] colors = { Color.red, Color.green, Color.blue, Color.white };
            
            float barHeight = (rect.height - 12) / 4f;
            
            for (int i = 0; i < 4; i++)
            {
                Rect barRect = new Rect(rect.x + 20, rect.y + 2 + i * (barHeight + 1), rect.width - 25, barHeight);
                Rect labelRect = new Rect(rect.x + 2, rect.y + 2 + i * (barHeight + 1), 16, barHeight);
                
                // Draw label
                GUI.Label(labelRect, labels[i], EditorStyles.miniLabel);
                
                // Draw background
                EditorGUI.DrawRect(barRect, Color.gray * 0.3f);
                
                // Draw value bar
                float normalizedValue = Mathf.Clamp01(Mathf.Abs(values[i]) / 5f); // Scale for visibility
                Rect valueRect = new Rect(barRect.x, barRect.y, barRect.width * normalizedValue, barRect.height);
                EditorGUI.DrawRect(valueRect, colors[i] * 0.8f);
            }
        }

        /// <summary>
        /// Gets a color representing vector magnitude.
        /// </summary>
        private Color GetVectorColor(float magnitude)
        {
            if (magnitude < 0.1f) return Color.gray;
            if (magnitude < 1f) return Color.Lerp(Color.yellow, Color.green, magnitude);
            if (magnitude < 5f) return Color.Lerp(Color.green, Color.red, (magnitude - 1f) / 4f);
            return Color.red;
        }

        /// <summary>
        /// Draws a coordinate grid for vector visualization.
        /// </summary>
        private void DrawVectorGrid(Rect rect, Vector3 center, float scale)
        {
            Color gridColor = Color.gray * 0.3f;
            
            // Draw X axis
            Vector3 xStart = center + Vector3.left * scale;
            Vector3 xEnd = center + Vector3.right * scale;
            DrawLine(xStart, xEnd, gridColor, 1f);
            
            // Draw Y axis (flipped for GUI)
            Vector3 yStart = center + Vector3.up * scale;
            Vector3 yEnd = center + Vector3.down * scale;
            DrawLine(yStart, yEnd, gridColor, 1f);
            
            // Draw center dot
            DrawCircle(center, 2f, gridColor);
        }

        /// <summary>
        /// Draws an arrow from start to end point.
        /// </summary>
        private void DrawArrow(Vector3 start, Vector3 end, Color color, float thickness)
        {
            // Draw main line
            DrawLine(start, end, color, thickness);
            
            // Draw arrowhead
            Vector3 direction = (end - start).normalized;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);
            
            float arrowSize = 8f;
            Vector3 arrowPoint1 = end - direction * arrowSize + perpendicular * (arrowSize * 0.5f);
            Vector3 arrowPoint2 = end - direction * arrowSize - perpendicular * (arrowSize * 0.5f);
            
            DrawLine(end, arrowPoint1, color, thickness);
            DrawLine(end, arrowPoint2, color, thickness);
        }

        /// <summary>
        /// Draws a line between two points.
        /// </summary>
        private void DrawLine(Vector3 start, Vector3 end, Color color, float thickness)
        {
            Vector3 direction = end - start;
            float distance = direction.magnitude;
            
            if (distance < 0.1f) return;
            
            Rect lineRect = new Rect(
                start.x, 
                start.y - thickness * 0.5f, 
                distance, 
                thickness);
            
            // Rotate the rect (simplified for horizontal/vertical lines)
            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, start);
            
            EditorGUI.DrawRect(lineRect, color);
            
            GUI.matrix = matrix;
        }

        /// <summary>
        /// Draws a circle at the specified position.
        /// </summary>
        private void DrawCircle(Vector3 center, float radius, Color color)
        {
            Rect circleRect = new Rect(
                center.x - radius, 
                center.y - radius, 
                radius * 2, 
                radius * 2);
            
            EditorGUI.DrawRect(circleRect, color);
        }
    }
}
#endif