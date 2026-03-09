using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace DNExtensions.Systems.VFXManager
{
    public struct ImageSettings
    {
        public Sprite Sprite;
        public Color Color;
        public Vector3 LocalPosition;
        public Vector3 LocalScale;
        public Vector3 LocalEulerAngles;
    }

    public struct VignetteSettings
    {
        public float Intensity;
        public float Smoothness;
        public Vector2 Center;
        public bool Rounded;
    }

    public struct LensDistortionSettings
    {
        public float Intensity;
        public float XMultiplier;
        public float YMultiplier;
        public Vector2 Center;
        public float Scale;
    }

    public struct ChromaticAberrationSettings
    {
        public float Intensity;
    }

    public struct MotionBlurSettings
    {
        public float Intensity;
        public float Clamp;
    }

    public struct PaniniProjectionSettings
    {
        public float Distance;
        public float CropToFit;
    }

    public struct DepthOfFieldSettings
    {
        public float FocusDistance;
        public float Aperture;
        public float FocalLength;
    }

    public static class VFXSettingsExtensions
    {
        public static ImageSettings CopyToSettings(this Image image) => new ImageSettings
        {
            Sprite = image.sprite,
            Color = image.color,
            LocalPosition = image.rectTransform.localPosition,
            LocalScale = image.rectTransform.localScale,
            LocalEulerAngles = image.rectTransform.localEulerAngles
        };

        public static void ApplyTo(this ImageSettings settings, Image image)
        {
            image.sprite = settings.Sprite;
            image.color = settings.Color;
            image.rectTransform.localPosition = settings.LocalPosition;
            image.rectTransform.localScale = settings.LocalScale;
            image.rectTransform.localEulerAngles = settings.LocalEulerAngles;
        }

        public static VignetteSettings CopyToSettings(this Vignette vignette) => new VignetteSettings
        {
            Intensity = vignette.intensity.value,
            Smoothness = vignette.smoothness.value,
            Center = vignette.center.value,
            Rounded = vignette.rounded.value
        };

        public static void ApplyTo(this VignetteSettings settings, Vignette vignette)
        {
            vignette.intensity.value = settings.Intensity;
            vignette.smoothness.value = settings.Smoothness;
            vignette.center.value = settings.Center;
            vignette.rounded.value = settings.Rounded;
        }

        public static LensDistortionSettings CopyToSettings(this LensDistortion lensDistortion) => new LensDistortionSettings
        {
            Intensity = lensDistortion.intensity.value,
            XMultiplier = lensDistortion.xMultiplier.value,
            YMultiplier = lensDistortion.yMultiplier.value,
            Center = lensDistortion.center.value,
            Scale = lensDistortion.scale.value
        };

        public static void ApplyTo(this LensDistortionSettings settings, LensDistortion lensDistortion)
        {
            lensDistortion.intensity.value = settings.Intensity;
            lensDistortion.xMultiplier.value = settings.XMultiplier;
            lensDistortion.yMultiplier.value = settings.YMultiplier;
            lensDistortion.center.value = settings.Center;
            lensDistortion.scale.value = settings.Scale;
        }

        public static ChromaticAberrationSettings CopyToSettings(this ChromaticAberration chromaticAberration) => new ChromaticAberrationSettings
        {
            Intensity = chromaticAberration.intensity.value
        };

        public static void ApplyTo(this ChromaticAberrationSettings settings, ChromaticAberration chromaticAberration)
        {
            chromaticAberration.intensity.value = settings.Intensity;
        }

        public static MotionBlurSettings CopyToSettings(this MotionBlur motionBlur) => new MotionBlurSettings
        {
            Intensity = motionBlur.intensity.value,
            Clamp = motionBlur.clamp.value
        };

        public static void ApplyTo(this MotionBlurSettings settings, MotionBlur motionBlur)
        {
            motionBlur.intensity.value = settings.Intensity;
            motionBlur.clamp.value = settings.Clamp;
        }

        public static PaniniProjectionSettings CopyToSettings(this PaniniProjection paniniProjection) => new PaniniProjectionSettings
        {
            Distance = paniniProjection.distance.value,
            CropToFit = paniniProjection.cropToFit.value
        };

        public static void ApplyTo(this PaniniProjectionSettings settings, PaniniProjection paniniProjection)
        {
            paniniProjection.distance.value = settings.Distance;
            paniniProjection.cropToFit.value = settings.CropToFit;
        }

        public static DepthOfFieldSettings CopyToSettings(this DepthOfField depthOfField) => new DepthOfFieldSettings
        {
            FocusDistance = depthOfField.focusDistance.value,
            Aperture = depthOfField.aperture.value,
            FocalLength = depthOfField.focalLength.value
        };

        public static void ApplyTo(this DepthOfFieldSettings settings, DepthOfField depthOfField)
        {
            depthOfField.focusDistance.value = settings.FocusDistance;
            depthOfField.aperture.value = settings.Aperture;
            depthOfField.focalLength.value = settings.FocalLength;
        }
    }
}