using DNExtensions.Utilities.Button;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace DNExtensions.Systems.VFXManager
{
    [DisallowMultipleComponent]
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("If true, the post-processing effects will be automatically set up on Awake.")]
        [SerializeField] private bool autoSetupPostProcessing = true;
        [SerializeField] private bool autoSetCameraIfUsingCameraOverlay;

        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image fullScreenImage;
        [SerializeField] private Canvas fullscreenCanvas;
        [SerializeField] private EffectSequence[] effectsSequences;

        private EffectSequence _currentSequence;
        private Volume _postProcessingVolume;

        public Image FullScreenImage => fullScreenImage;
        public Image IconImage => iconImage;
        public LensDistortion LensDistortion { get; private set; }
        public ChromaticAberration ChromaticAberration { get; private set; }
        public MotionBlur MotionBlur { get; private set; }
        public Vignette Vignette { get; private set; }
        public PaniniProjection PaniniProjection { get; private set; }
        public DepthOfField DepthOfField { get; private set; }
        
        public ImageSettings DefaultIconImage { get; private set; }
        public ImageSettings DefaultFullScreenImage { get; private set; }
        public VignetteSettings DefaultVignette { get; private set; }
        public LensDistortionSettings DefaultLensDistortion { get; private set; }
        public ChromaticAberrationSettings DefaultChromaticAberration { get; private set; }
        public MotionBlurSettings DefaultMotionBlur { get; private set; }
        public PaniniProjectionSettings DefaultPaniniProjection { get; private set; }
        public DepthOfFieldSettings DefaultDepthOfField { get; private set; }


        private void OnValidate()
        {
            if (effectsSequences == null || effectsSequences.Length == 0) return;
            for (int i = 0; i < effectsSequences.Length; i++)
            {
                for (int j = i + 1; j < effectsSequences.Length; j++)
                {
                    if (effectsSequences[i] == effectsSequences[j])
                    {
                        Debug.LogWarning($"Duplicate EffectSequence found: {effectsSequences[i].name}.", this);
                    }
                }
            }
        }

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (iconImage)
            {
                iconImage.color = Color.clear;
                DefaultIconImage = iconImage.CopyToSettings();
            }

            if (fullScreenImage)
            {
                fullScreenImage.color = Color.clear;
                DefaultFullScreenImage = fullScreenImage.CopyToSettings();
            }
            

            if (fullscreenCanvas && fullscreenCanvas.renderMode == RenderMode.ScreenSpaceCamera && autoSetCameraIfUsingCameraOverlay && Camera.main)
            {
                fullscreenCanvas.worldCamera = Camera.main;
            }

            SetupPostProcessingVolume();
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private void OnActiveSceneChanged(Scene currentScene, Scene nextScene)
        {

            if (fullscreenCanvas && fullscreenCanvas.renderMode == RenderMode.ScreenSpaceCamera && autoSetCameraIfUsingCameraOverlay && Camera.main)
            {
                fullscreenCanvas.worldCamera = Camera.main;
            }

            SetupPostProcessingVolume();
        }

        private void SetupPostProcessingVolume()
        {
            if (!_postProcessingVolume) _postProcessingVolume = FindFirstObjectByType<Volume>();
            if (!_postProcessingVolume)
            {
                Debug.LogWarning("No Post Processing Volume found in the scene!");
                return;
            }

            if (_postProcessingVolume.profile.TryGet(out Vignette vignette))
            {
                Vignette = vignette;
                DefaultVignette = vignette.CopyToSettings();
                if (autoSetupPostProcessing)
                {
                    Vignette.active = true;
                    Vignette.color.overrideState = true;
                    Vignette.center.overrideState = true;
                    Vignette.intensity.overrideState = true;
                    Vignette.smoothness.overrideState = true;
                    Vignette.rounded.overrideState = true;
                }
            }

            if (_postProcessingVolume.profile.TryGet(out LensDistortion lensDistortion))
            {
                LensDistortion = lensDistortion;
                DefaultLensDistortion = lensDistortion.CopyToSettings();
                if (autoSetupPostProcessing)
                {
                    LensDistortion.active = true;
                    LensDistortion.intensity.overrideState = true;
                    LensDistortion.center.overrideState = true;
                    LensDistortion.scale.overrideState = true;
                    LensDistortion.xMultiplier.overrideState = true;
                    LensDistortion.yMultiplier.overrideState = true;
                }
            }

            if (_postProcessingVolume.profile.TryGet(out ChromaticAberration chromaticAberration))
            {
                ChromaticAberration = chromaticAberration;
                DefaultChromaticAberration = chromaticAberration.CopyToSettings();
                if (autoSetupPostProcessing)
                {
                    ChromaticAberration.active = true;
                }
            }

            if (_postProcessingVolume.profile.TryGet(out MotionBlur motionBlur))
            {
                MotionBlur = motionBlur;
                DefaultMotionBlur = motionBlur.CopyToSettings();
                if (autoSetupPostProcessing)
                {
                    MotionBlur.active = true;
                    MotionBlur.intensity.overrideState = true;
                    MotionBlur.quality.overrideState = true;
                    MotionBlur.clamp.overrideState = true;
                }
            }

            if (_postProcessingVolume.profile.TryGet(out PaniniProjection paniniProjection))
            {
                PaniniProjection = paniniProjection;
                DefaultPaniniProjection = paniniProjection.CopyToSettings();
                if (autoSetupPostProcessing)
                {
                    PaniniProjection.active = true;
                    PaniniProjection.distance.overrideState = true;
                    PaniniProjection.cropToFit.overrideState = true;
                }
            }

            if (_postProcessingVolume.profile.TryGet(out DepthOfField depthOfField))
            {
                DepthOfField = depthOfField;
                DefaultDepthOfField = depthOfField.CopyToSettings();
                if (autoSetupPostProcessing)
                {
                    DepthOfField.active = true;
                    DepthOfField.focusDistance.overrideState = true;
                    DepthOfField.aperture.overrideState = true;
                    DepthOfField.focalLength.overrideState = true;
                }
            }
        }

        /// <summary>
        /// Plays a specific visual effects sequence.
        /// </summary>
        /// <returns>The duration of the sequence.</returns>
        public float PlaySequence(EffectSequence sequence)
        {
            if (!sequence) return 0;
            if (!sequence.IsAdditive) ResetActiveEffects();

            _currentSequence = sequence;
            return _currentSequence.PlaySequence();
        }

        /// <summary>
        /// Gets a random sequence from the sequence list.
        /// </summary>
        public EffectSequence GetRandomSequence()
        {
            if (effectsSequences == null || effectsSequences.Length == 0) return null;
            return effectsSequences[Random.Range(0, effectsSequences.Length)];
        }

        /// <summary>
        /// Resets all effects to their default state.
        /// </summary>
        [Button(ButtonPlayMode.OnlyWhenPlaying)]
        public void ResetActiveEffects()
        {
            if (_currentSequence)
            {
                _currentSequence.ResetSequenceEffects();
                _currentSequence = null;
            }

            if (iconImage) DefaultIconImage.ApplyTo(iconImage);
            if (fullScreenImage) DefaultFullScreenImage.ApplyTo(fullScreenImage);
            if (Vignette) DefaultVignette.ApplyTo(Vignette);
            if (LensDistortion) DefaultLensDistortion.ApplyTo(LensDistortion);
            if (ChromaticAberration) DefaultChromaticAberration.ApplyTo(ChromaticAberration);
            if (MotionBlur) DefaultMotionBlur.ApplyTo(MotionBlur);
            if (PaniniProjection) DefaultPaniniProjection.ApplyTo(PaniniProjection);
            if (DepthOfField) DefaultDepthOfField.ApplyTo(DepthOfField);
        }
    }
}