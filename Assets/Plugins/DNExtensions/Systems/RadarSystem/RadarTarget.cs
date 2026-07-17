using DNExtensions.Utilities.Button;
using UnityEngine;

namespace DNExtensions.Systems.RadarSystem
{
    [AddComponentMenu("DNExtensions/Radar Target")]
    public class RadarTarget : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool autoRegisterOnEnable = true;
        [SerializeField] private bool showOutOfRange;
        [SerializeField] private Sprite blipSprite;
        [SerializeField] private Color blipColor = Color.white;
        [SerializeField, Range(0.1f, 2.0f)] private float blipSizeMultiplier = 1.0f;

        [Header("Ping")]
        [SerializeField, Min(0.1f)] private float pingDuration = 1f;
        [SerializeField, Min(0.1f)] private float pingSizeMultiplier = 10;
        [SerializeField, Range(0.1f, 1f)] private float pingStartAlpha = 1f;

        [Header("Punch")]
        [SerializeField, Min(0.1f)] private float punchDuration = 0.15f;
        [SerializeField, Range(0.1f, 2.0f)] private float punchSizeMultiplier = 1.25f;

        public Sprite BlipSprite => blipSprite;
        public Color BlipColor => blipColor;
        public float BlipSizeMultiplier => blipSizeMultiplier;
        public bool ShowOutOfRange => showOutOfRange;
        public float PingDuration => pingDuration;
        public float PingSizeMultiplier => pingSizeMultiplier;
        public float PingStartAlpha => pingStartAlpha;
        public float PunchDuration => punchDuration;
        public float PunchSizeMultiplier => punchSizeMultiplier;

        private void OnEnable()
        {
            if (autoRegisterOnEnable) EnableBlip();
        }

        private void OnDisable()
        {
            DisableBlip();
        }

        [Button]
        public void EnableBlip()
        {
            RadarSystem.Instance?.Register(this);
        }

        [Button]
        public void DisableBlip()
        {
            RadarSystem.Instance?.Unregister(this);
        }

        public void PingBlip(Color? colorOverride = null)
        {
            RadarSystem.Instance?.PingTarget(this, colorOverride);
        }

        [Button]
        public void PunchBlip(Color color)
        {
            RadarSystem.Instance?.PunchTarget(this, color);
        }
    }
}
