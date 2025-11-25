using UnityEngine;

namespace DNExtensions
{
    /// <summary>
    /// Property attribute that displays a preview of sprites/2D textures and prefabs in the Unity Inspector.
    /// Shows a small thumbnail below the object field for easy visual identification.
    /// </summary>
    /// <example>
    /// <code>
    /// [Preview]
    /// public Sprite characterSprite;
    /// 
    /// [Preview(size: 128)]
    /// public Texture2D backgroundTexture;
    /// 
    /// [Preview(size: 64, showLabel: true)]
    /// public GameObject enemyPrefab;
    /// </code>
    /// </example>
    public class PreviewAttribute : PropertyAttribute
    {
        /// <summary>
        /// Size of the preview thumbnail in pixels (width and height).
        /// </summary>
        public float Size { get; }
        
        /// <summary>
        /// Whether to show a label below the preview.
        /// </summary>
        public bool ShowLabel { get; }
        
        /// <summary>
        /// Whether to show preview even when the field is null/empty.
        /// If true, shows a placeholder when no asset is assigned.
        /// </summary>
        public bool ShowWhenEmpty { get; }
        
        /// <summary>
        /// Background color for the preview area.
        /// </summary>
        public Color BackgroundColor { get; }
        
        /// <summary>
        /// Whether to show asset details (dimensions, file size, etc.).
        /// </summary>
        public bool ShowDetails { get; }

        /// <summary>
        /// Creates a preview attribute with default settings.
        /// </summary>
        public PreviewAttribute() : this(64f) { }

        /// <summary>
        /// Creates a preview attribute with specified size.
        /// </summary>
        /// <param name="size">Size of the preview thumbnail in pixels</param>
        public PreviewAttribute(float size) : this(size, false) { }

        /// <summary>
        /// Creates a preview attribute with specified size and label option.
        /// </summary>
        /// <param name="size">Size of the preview thumbnail in pixels</param>
        /// <param name="showLabel">Whether to show a label below the preview</param>
        public PreviewAttribute(float size, bool showLabel) : this(size, showLabel, false) { }

        /// <summary>
        /// Creates a preview attribute with full customization.
        /// </summary>
        /// <param name="size">Size of the preview thumbnail in pixels</param>
        /// <param name="showLabel">Whether to show a label below the preview</param>
        /// <param name="showWhenEmpty">Whether to show preview area when no asset is assigned</param>
        /// <param name="r">Red component of background color (0-1)</param>
        /// <param name="g">Green component of background color (0-1)</param>
        /// <param name="b">Blue component of background color (0-1)</param>
        /// <param name="a">Alpha component of background color (0-1)</param>
        /// <param name="showDetails">Whether to show asset details</param>
        public PreviewAttribute(float size, bool showLabel, bool showWhenEmpty, 
            float r = 0.2f, float g = 0.2f, float b = 0.2f, float a = 1f, bool showDetails = false)
        {
            Size = Mathf.Clamp(size, 16f, 256f);
            ShowLabel = showLabel;
            ShowWhenEmpty = showWhenEmpty;
            BackgroundColor = new Color(r, g, b, a);
            ShowDetails = showDetails;
        }
    }
}