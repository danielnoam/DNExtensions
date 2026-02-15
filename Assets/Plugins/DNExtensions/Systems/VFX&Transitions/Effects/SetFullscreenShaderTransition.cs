using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.Systems.VFXManager
{
    [Serializable]
    public class SetFullscreenShaderTransition : EffectBase
    {
        [Header("Shader Material")]
        [Tooltip("Material using the FullscreenTransition shader. If null, will create one at runtime.")]
        [SerializeField] private Material transitionMaterial;
        
        [Header("Shape Settings")]
        [SerializeField] private MaskShape maskShape = MaskShape.Circle;
        [SerializeField] private Texture2D customMaskTexture;
        
        [Header("Main Texture & Color")]
        [SerializeField] private Sprite sprite;
        [SerializeField] private PropertyAnimation<Color> color = new PropertyAnimation<Color>
        {
            animate = true,
            useDefault = false,
            startValue = Color.clear,
            endValue = Color.black,
            ease = Ease.Linear
        };
        
        [Header("Transform")]
        [SerializeField] private PropertyAnimation<Vector2> tileCount = new PropertyAnimation<Vector2>
        {
            animate = false,
            useDefault = false,
            startValue = Vector2.one,
            endValue = Vector2.one,
            ease = Ease.Linear
        };
        
        [SerializeField] private PropertyAnimation<Vector2> offset = new PropertyAnimation<Vector2>
        {
            animate = false,
            useDefault = false,
            startValue = Vector2.zero,
            endValue = Vector2.zero,
            ease = Ease.Linear
        };
        
        [SerializeField] private PropertyAnimation<float> rotation = new PropertyAnimation<float>
        {
            animate = false,
            useDefault = false,
            startValue = 0f,
            endValue = 0f,
            ease = Ease.Linear
        };
        
        [Header("Animation")]
        [Tooltip("Progress controls the reveal/hide of the transition. 0 = hidden, 1 = fully visible")]
        [SerializeField] private PropertyAnimation<float> progress = new PropertyAnimation<float>
        {
            animate = true,
            useDefault = false,
            startValue = 0f,
            endValue = 1f,
            ease = Ease.Linear
        };
        
        [SerializeField] private PropertyAnimation<float> expansion = new PropertyAnimation<float>
        {
            animate = false,
            useDefault = false,
            startValue = 1f,
            endValue = 1f,
            ease = Ease.Linear
        };
        
        [Header("Visual Quality")]
        [SerializeField, Range(0f, 0.5f)] private float edgeFeather = 0.1f;
        
        [Header("Advanced Effects")]
        [SerializeField] private Vector2 scrollSpeed = Vector2.zero;
        [SerializeField, Range(0f, 50f)] private float noiseScale = 10f;
        [SerializeField, Range(0f, 1f)] private float noiseStrength = 0f;
        
        public enum MaskShape
        {
            Square = 0,
            Circle = 1,
            Diamond = 2,
            Custom = 3
        }
        
        private Image _image;
        private Material _runtimeMaterial;
        private Sequence _sequence;
        
        // Shader property IDs (cached for performance)
        private static readonly int ColorID = Shader.PropertyToID("_Color");
        private static readonly int MaskShapeID = Shader.PropertyToID("_MaskShape");
        private static readonly int MaskTexID = Shader.PropertyToID("_MaskTex");
        private static readonly int TileCountID = Shader.PropertyToID("_TileCount");
        private static readonly int OffsetID = Shader.PropertyToID("_Offset");
        private static readonly int RotationID = Shader.PropertyToID("_Rotation");
        private static readonly int ProgressID = Shader.PropertyToID("_Progress");
        private static readonly int ExpansionID = Shader.PropertyToID("_Expansion");
        private static readonly int FeatherID = Shader.PropertyToID("_Feather");
        private static readonly int ScrollSpeedID = Shader.PropertyToID("_ScrollSpeed");
        private static readonly int NoiseScaleID = Shader.PropertyToID("_NoiseScale");
        private static readonly int NoiseStrengthID = Shader.PropertyToID("_NoiseStrength");
        
        protected override bool Initialize()
        {
            if (!base.Initialize()) return false;
            
            _image = VFXManager.Instance.FullScreenImage;
            if (_image == null)
            {
                Debug.LogWarning("FullScreenImage not found in VFXManager. SetFullscreenShaderTransition effect will not play.", VFXManager.Instance);
                return false;
            }
            
            // Create runtime material if none provided
            if (transitionMaterial == null)
            {
                Shader shader = Shader.Find("DNExtensions/FullscreenTransition");
                if (shader == null)
                {
                    Debug.LogError("FullscreenTransition shader not found! Make sure the shader is in your project.");
                    return false;
                }
                _runtimeMaterial = new Material(shader);
            }
            else
            {
                // Create instance to avoid modifying the original material
                _runtimeMaterial = new Material(transitionMaterial);
            }
            
            return true;
        }
        
        protected override void OnPlayEffect(float sequenceDuration)
        {
            var startDelay = GetStartDelay(sequenceDuration);
            var effectDuration = GetEffectDuration(sequenceDuration);
            
            // Apply material to image
            _image.material = _runtimeMaterial;
            
            // Set sprite if provided
            if (sprite != null)
            {
                _image.sprite = sprite;
            }
            
            // Set instant properties
            _runtimeMaterial.SetFloat(MaskShapeID, (float)maskShape);
            if (maskShape == MaskShape.Custom && customMaskTexture != null)
            {
                _runtimeMaterial.SetTexture(MaskTexID, customMaskTexture);
            }
            _runtimeMaterial.SetFloat(FeatherID, edgeFeather);
            _runtimeMaterial.SetVector(ScrollSpeedID, new Vector4(scrollSpeed.x, scrollSpeed.y, 0, 0));
            _runtimeMaterial.SetFloat(NoiseScaleID, noiseScale);
            _runtimeMaterial.SetFloat(NoiseStrengthID, noiseStrength);
            
            // Stop existing animations
            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true);
            
            // Animate Color
            if (color.animate)
            {
                var colorStart = color.GetStartValue(VFXManager.Instance.DefaultFullScreenColor);
                _sequence.Group(Tween.Custom(colorStart, color.endValue, effectDuration,
                    onValueChange: value => _runtimeMaterial.SetColor(ColorID, value),
                    ease: color.ease,
                    startDelay: startDelay));
            }
            else
            {
                _runtimeMaterial.SetColor(ColorID, color.startValue);
            }
            
            // Animate Progress (most important - controls visibility)
            if (progress.animate)
            {
                var progressStart = progress.GetStartValue(0f);
                _sequence.Group(Tween.Custom(progressStart, progress.endValue, effectDuration,
                    onValueChange: value => _runtimeMaterial.SetFloat(ProgressID, value),
                    ease: progress.ease,
                    startDelay: startDelay));
            }
            else
            {
                _runtimeMaterial.SetFloat(ProgressID, progress.startValue);
            }
            
            // Animate Expansion
            if (expansion.animate)
            {
                var expansionStart = expansion.GetStartValue(1f);
                _sequence.Group(Tween.Custom(expansionStart, expansion.endValue, effectDuration,
                    onValueChange: value => _runtimeMaterial.SetFloat(ExpansionID, value),
                    ease: expansion.ease,
                    startDelay: startDelay));
            }
            else
            {
                _runtimeMaterial.SetFloat(ExpansionID, expansion.startValue);
            }
            
            // Animate Tile Count
            if (tileCount.animate)
            {
                var tileStart = tileCount.GetStartValue(Vector2.one);
                _sequence.Group(Tween.Custom(tileStart, tileCount.endValue, effectDuration,
                    onValueChange: value => _runtimeMaterial.SetVector(TileCountID, new Vector4(value.x, value.y, 0, 0)),
                    ease: tileCount.ease,
                    startDelay: startDelay));
            }
            else
            {
                _runtimeMaterial.SetVector(TileCountID, new Vector4(tileCount.startValue.x, tileCount.startValue.y, 0, 0));
            }
            
            // Animate Offset
            if (offset.animate)
            {
                var offsetStart = offset.GetStartValue(Vector2.zero);
                _sequence.Group(Tween.Custom(offsetStart, offset.endValue, effectDuration,
                    onValueChange: value => _runtimeMaterial.SetVector(OffsetID, new Vector4(value.x, value.y, 0, 0)),
                    ease: offset.ease,
                    startDelay: startDelay));
            }
            else
            {
                _runtimeMaterial.SetVector(OffsetID, new Vector4(offset.startValue.x, offset.startValue.y, 0, 0));
            }
            
            // Animate Rotation
            if (rotation.animate)
            {
                var rotationStart = rotation.GetStartValue(0f);
                _sequence.Group(Tween.Custom(rotationStart, rotation.endValue, effectDuration,
                    onValueChange: value => _runtimeMaterial.SetFloat(RotationID, value),
                    ease: rotation.ease,
                    startDelay: startDelay));
            }
            else
            {
                _runtimeMaterial.SetFloat(RotationID, rotation.startValue);
            }
        }
        
        protected override void OnResetEffect()
        {
            if (_sequence.isAlive) _sequence.Stop();
            
            // Reset material to default (null removes custom material)
            _image.material = null;
            _image.sprite = VFXManager.Instance.DefaultFullScreenSprite;
            _image.color = VFXManager.Instance.DefaultFullScreenColor;
            
            // Clean up runtime material
            if (_runtimeMaterial != null && transitionMaterial == null)
            {
                UnityEngine.Object.Destroy(_runtimeMaterial);
                _runtimeMaterial = null;
            }
        }
    }
}