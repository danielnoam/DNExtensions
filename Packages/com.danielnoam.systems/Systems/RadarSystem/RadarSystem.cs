using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DNExtensions.Utilities.CustomFields;
using PrimeTween;

namespace DNExtensions.Systems.RadarSystem
{
    [AddComponentMenu("DNExtensions/Radar System")]
    public class RadarSystem : MonoBehaviour
    {
        public static RadarSystem Instance { get; private set; }

        [Header("Settings")]
        public float radarRange = 55f;
        public PositionField worldCenter;

        [Header("UI")]
        public float radiusMultiplier = 1;
        public OptionalField<Transform> rotationTarget;
        public RectTransform radarPanel;
        public Transform blipHolder;
        public Graphic blipPrefab;
        public Graphic pingPrefab;

        private readonly HashSet<RadarTarget> _targets = new();
        private readonly Dictionary<RadarTarget, Graphic> _blips = new();
        private readonly Dictionary<Graphic, RadarTarget> _pings = new();
        private readonly Dictionary<RadarTarget, (Sequence sequence, Vector3 baseScale)> _punches = new();

        private float _radarRadius;

        private void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _radarRadius = radarPanel.rect.width * 0.5f * radiusMultiplier;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void LateUpdate()
        {
            UpdateBlips();
            UpdatePings();
        }

        private void UpdateBlips()
        {
            Vector3 center = worldCenter.Position;
            float angle = rotationTarget && rotationTarget.Value ? rotationTarget.Value.eulerAngles.y : 0f;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            foreach (RadarTarget target in _targets)
            {
                if (!_blips.TryGetValue(target, out Graphic blip)) continue;

                Vector3 offset = target.transform.position - center;
                Vector2 radarOffset = new Vector2(offset.x, offset.z) / radarRange * _radarRadius;
                radarOffset = rotation * radarOffset;
                float distance = radarOffset.magnitude;

                if (distance > _radarRadius)
                {
                    if (target.ShowOutOfRange)
                    {
                        radarOffset = radarOffset.normalized * _radarRadius;
                        blip.enabled = true;
                    }
                    else
                    {
                        blip.enabled = false;
                        continue;
                    }
                }
                else
                {
                    blip.enabled = true;
                }

                blip.rectTransform.anchoredPosition = radarOffset;
            }
        }

        private void UpdatePings()
        {
            Vector3 center = worldCenter.Position;
            float angle = rotationTarget && rotationTarget.Value ? rotationTarget.Value.eulerAngles.y : 0f;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            foreach (var (ping, target) in _pings)
            {
                if (!ping || !target) continue;

                Vector3 offset = target.transform.position - center;
                Vector2 radarOffset = new Vector2(offset.x, offset.z) / radarRange * _radarRadius;
                radarOffset = rotation * radarOffset;

                if (radarOffset.magnitude > _radarRadius)
                    radarOffset = radarOffset.normalized * _radarRadius;

                ping.rectTransform.anchoredPosition = radarOffset;
            }
        }

        private void DestroyPing(Graphic ping)
        {
            if (!ping) return;
            _pings.Remove(ping);
            Destroy(ping.gameObject);
        }

        public void PingTarget(RadarTarget target, Color? colorOverride = null)
        {
            if (!pingPrefab) return;
            if (!_blips.TryGetValue(target, out Graphic blip)) return;

            var ping = Instantiate(pingPrefab, blipHolder ? blipHolder : transform);
            ping.rectTransform.anchoredPosition = blip.rectTransform.anchoredPosition;
            if (colorOverride.HasValue) ping.color = colorOverride.Value;
            _pings[ping] = target;

            Sequence.Create()
                .ChainDelay(0.15f)
                .Group(Tween.Alpha(ping, target.PingStartAlpha, 0, target.PingDuration))
                .Group(Tween.Scale(ping.transform, ping.transform.localScale * target.PingSizeMultiplier, target.PingDuration))
                .OnComplete(() => DestroyPing(ping));
        }

        public void PunchTarget(RadarTarget target, Color color)
        {
            if (!_blips.TryGetValue(target, out Graphic blip)) return;

            if (_punches.TryGetValue(target, out var existing) && existing.sequence.isAlive)
            {
                existing.sequence.Stop();
                blip.color = target.BlipColor;
                blip.transform.localScale = existing.baseScale;
            }

            Vector3 baseScale = blip.transform.localScale;
            float d = target.PunchDuration;

            var sequence = Sequence.Create()
                .Group(Tween.Color(blip, color, d))
                .Group(Tween.Scale(blip.transform, baseScale * target.PunchSizeMultiplier, d))
                .Chain(Tween.Color(blip, target.BlipColor, d))
                .Group(Tween.Scale(blip.transform, baseScale, d));

            _punches[target] = (sequence, baseScale);
        }

        public void Register(RadarTarget target)
        {
            if (!_targets.Add(target)) return;

            var blip = Instantiate(blipPrefab, blipHolder ? blipHolder : transform);
            blip.transform.localScale *= target.BlipSizeMultiplier;
            blip.color = target.BlipColor;
            if (blip is Image image && target.BlipSprite) image.sprite = target.BlipSprite;
            _blips[target] = blip;
        }

        public void Unregister(RadarTarget target)
        {
            if (!_targets.Remove(target)) return;

            if (_punches.Remove(target, out var punch) && punch.sequence.isAlive) punch.sequence.Stop();

            if (_blips.Remove(target, out Graphic blip)) Destroy(blip.gameObject);
        }
    }
}
