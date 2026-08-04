using DNExtensions.Utilities.Button;
using PrimeTween;
using UnityEngine;

namespace DNExtensions.Utilities
{
    [AddComponentMenu("DNExtensions/Material Property Tweener")]
    public class MaterialPropertyTweener : MonoBehaviour
    {
        [SerializeField] private Material material;
        [SerializeField] private string propertyName = "_Visibility";

        [Header("Animate")] 
        [SerializeField] private float showValue = 1f;
        [SerializeField] private float hideValue;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private Ease ease = Ease.InOutSine;

        [Header("Punch")] 
        [SerializeField] private int punchCount = 1;
        [SerializeField] private float punchDuration = 0.25f;
        [SerializeField] private Ease punchEase = Ease.Linear;

        private int _propertyId;
        private Sequence _sequence;

        private void Awake()
        {
            if (!material)
            {
                enabled = false;
                return;
            }
            _propertyId = Shader.PropertyToID(propertyName);
            material.SetFloat(_propertyId, hideValue);
        }

        private void OnDestroy()
        {
            if (_sequence.isAlive) _sequence.Stop();
            material.SetFloat(_propertyId, hideValue);
        }

        private void Animate(float target)
        {
            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(Tween.MaterialProperty(material, _propertyId, target, duration, ease));
        }

        public void SetValue(float value)
        {
            if (_sequence.isAlive) _sequence.Stop();
            material.SetFloat(_propertyId, value);
        }

        [Button]
        public void Punch()
        {
            if (_sequence.isAlive) _sequence.Stop();

            _sequence = Sequence.Create(cycles: punchCount);
            _sequence.Group(Tween.MaterialProperty(material, _propertyId, showValue, punchDuration * 0.5f, punchEase));
            _sequence.Chain(Tween.MaterialProperty(material, _propertyId, hideValue, punchDuration * 0.5f, punchEase));
        }

        [Button]
        public void Show() => Animate(showValue);

        [Button]
        public void Hide() => Animate(hideValue);

    }
}