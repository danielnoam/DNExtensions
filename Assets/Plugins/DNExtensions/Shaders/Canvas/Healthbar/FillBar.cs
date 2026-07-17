using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using DNExtensions.Utilities.CustomFields;

namespace DNExtensions.Utilities
{
    [AddComponentMenu("DNExtensions/Fill Bar")]
    public class FillBar : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, Min(0f)] private float trailDelay = 0.1f;
        [SerializeField, Min(0f)] private float valueChangeDuration = 0.25f;
        [SerializeField, Min(0f)] private float trailChangeDuration = 2f;

        [Header("Trail Colors")]
        [SerializeField] private Color drainColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        [SerializeField] private Color fillColor = new Color(0.3f, 1f, 0.3f, 0.8f);

        [Header("Punch")]
        [SerializeField] private float punchStrength = 0.2f;
        [SerializeField, Min(0f)] private float punchDuration = 0.3f;
        [SerializeField] private Color punchColor = Color.white;

        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image barFill;
        [SerializeField] private Image trailFill;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private OptionalField<string> prefix = new OptionalField<string>("", false);

        private Sequence _sequence;
        private Tween _punchTween;
        private Tween _colorPunchTween;
        private Color _baseFillColor;
        private bool _hasBaseFillColor;

        private void Awake()
        {
            if (barFill)
            {
                _baseFillColor = barFill.color;
                _hasBaseFillColor = true;
            }
        }

        private void OnDestroy()
        {
            if (_sequence.isAlive) _sequence.Stop();
            if (_punchTween.isAlive) _punchTween.Stop();
            if (_colorPunchTween.isAlive) _colorPunchTween.Stop();
        }

        public void Punch()
        {
            transform.localScale = Vector3.one;
            if (_punchTween.isAlive) _punchTween.Stop();
            if (punchDuration > 0) _punchTween = Tween.PunchScale(transform, Vector3.one * punchStrength, duration: punchDuration);

            if (barFill && _hasBaseFillColor && punchDuration > 0)
            {
                if (_colorPunchTween.isAlive) _colorPunchTween.Stop();
                barFill.color = punchColor;
                _colorPunchTween = Tween.Color(barFill, _baseFillColor, duration: punchDuration);
            }
        }

        public void SetValue(float current, float max)
        {
            if (!barFill || !trailFill || max <= 0) return;
            if (_sequence.isAlive) _sequence.Stop();

            float targetFill = Mathf.Clamp01(current / max);
            if (Mathf.Approximately(targetFill, barFill.fillAmount))
            {
                UpdateText(current, max);
                return;
            }
            bool isDrain = targetFill < barFill.fillAmount;

            var leadBar = isDrain ? barFill : trailFill;
            var followBar = isDrain ? trailFill : barFill;

            trailFill.color = isDrain ? drainColor : fillColor;
            UpdateText(current, max);

            if (valueChangeDuration <= 0 && trailChangeDuration <= 0 || !isDrain)
            {
                barFill.fillAmount = targetFill;
                trailFill.fillAmount = targetFill;
                return;
            }

            var seq = Sequence.Create(useUnscaledTime: true, sequenceEase: Ease.InOutSine);

            if (valueChangeDuration > 0)
                seq.Group(Tween.UIFillAmount(leadBar, targetFill, duration: valueChangeDuration));
            else
                leadBar.fillAmount = targetFill;

            if (trailChangeDuration > 0)
            {
                if (trailDelay > 0) seq.ChainDelay(trailDelay);
                seq.Chain(Tween.UIFillAmount(followBar, targetFill, duration: trailChangeDuration));
            }
            else
            {
                followBar.fillAmount = targetFill;
            }

            _sequence = seq;
        }

        public void SetImmediate(float current, float max)
        {
            if (_sequence.isAlive) _sequence.Stop();
            if (max <= 0) return;

            float fill = Mathf.Clamp01(current / max);
            if (barFill) barFill.fillAmount = fill;
            if (trailFill) trailFill.fillAmount = fill;
            UpdateText(current, max);
        }

        private void UpdateText(float current, float max)
        {
            if (!valueText) return;
            valueText.text = prefix.isSet ? $"{prefix.Value}{current:N0} / {max:N0}" : $"{current:N0} / {max:N0}";
        }
    }
}
