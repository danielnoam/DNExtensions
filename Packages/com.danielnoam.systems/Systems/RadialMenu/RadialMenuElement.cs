using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.Systems.RadialMenu
{
    [AddComponentMenu("DNExtensions/Radial Menu Element")]
    public class RadialMenuElement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;

        [Header("Settings")]
        [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.4f;

        private Color _normalColor = Color.white;
        private Color _hoveredColor = new Color(1f, 0.647f, 0f);
        private bool _isHovered;
        private bool _isDisabled;

        public string Info { get; private set; }

        public void SetUp(Color normalColor, Color hoveredColor)
        {
            _normalColor = normalColor;
            _hoveredColor = hoveredColor;
            ApplyVisuals();
        }

        public void Configure(string info, Sprite icon, bool isAvailable)
        {
            Info = info;
            _isDisabled = !isAvailable;
            ApplyVisuals();

            if (icon)
            {
                iconImage.sprite = icon;
            }
            else if (iconImage)
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        public void SetHovered()
        {
            if (_isHovered) return;
            _isHovered = true;
            ApplyVisuals();
        }

        public void SetNormal()
        {
            if (!_isHovered) return;
            _isHovered = false;
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            if (!backgroundImage) return;
            Color target = _isHovered ? _hoveredColor : _normalColor;
            target.a = _isDisabled ? disabledAlpha : 1f;
            backgroundImage.color = target;
        }
    }
}
