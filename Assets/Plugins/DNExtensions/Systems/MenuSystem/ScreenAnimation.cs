
using System;
using DNExtensions.Utilities.SerializableSelector;
using PrimeTween;
using UnityEngine;

namespace DNExtensions.MenuSystem
{
    [Serializable]
    public abstract class ScreenAnimation
    {
        public float duration = 0.25f;

        public abstract Sequence CreateSequence(Screen screen);
    }

    [Serializable]
    [SerializableSelectorAllowOnce]
    public class FadeAnimation : ScreenAnimation
    {
        public bool startFromCurrentAlpha;
        [Range(0f, 1f)] public float startAlpha;
        [Range(0f, 1f)] public float endAlpha = 1f;
        public Ease ease = Ease.OutCubic;

        public override Sequence CreateSequence(Screen screen)
        {
            float from = startFromCurrentAlpha ? screen.CanvasGroup.alpha : startAlpha;

            return Sequence.Create()
                .Group(Tween.Alpha(
                    screen.CanvasGroup,
                    from,
                    endAlpha,
                    duration,
                    ease
                ));
        }
    }

    [Serializable]
    [SerializableSelectorAllowOnce]
    public class ScaleAnimation : ScreenAnimation
    {
        public bool startFromCurrentScale;
        public bool endInOriginalScale = true;
        public Vector3 startScale = Vector3.zero;
        public Vector3 endScale = Vector3.one;
        public Ease ease = Ease.OutBack;

        public override Sequence CreateSequence(Screen screen)
        {
            Vector3 from = startFromCurrentScale
                ? screen.RectTransform.localScale
                : startScale;

            Vector3 to = endInOriginalScale
                ? screen.TransformOriginalScale
                : endScale;

            return Sequence.Create()
                .Group(Tween.Scale(
                    screen.RectTransform,
                    from,
                    to,
                    duration,
                    ease
                ));
        }
    }

    [Serializable]
    [SerializableSelectorAllowOnce]
    public class SlideAnimation : ScreenAnimation
    {
        public SlideDirection direction = SlideDirection.Left;
        public float distance = 500f;
        public bool useOriginalPosition = true;
        public Ease ease = Ease.OutCubic;

        public enum SlideDirection
        {
            Left,
            Right,
            Up,
            Down
        }

        public override Sequence CreateSequence(Screen screen)
        {
            Vector3 originalPos = screen.TransformOriginalAnchoredPosition;
            Vector3 offset = direction switch
            {
                SlideDirection.Left => new Vector3(-distance, 0, 0),
                SlideDirection.Right => new Vector3(distance, 0, 0),
                SlideDirection.Up => new Vector3(0, distance, 0),
                SlideDirection.Down => new Vector3(0, -distance, 0),
                _ => Vector3.zero
            };

            Vector3 startPos = originalPos + offset;
            Vector3 endPos = useOriginalPosition ? originalPos : screen.RectTransform.anchoredPosition3D;

            screen.RectTransform.anchoredPosition3D = startPos;

            return Sequence.Create()
                .Group(Tween.Position(
                    screen.RectTransform,
                    endPos,
                    duration,
                    ease
                ));
        }
    }

    [Serializable]
    [SerializableSelectorAllowOnce]
    public class RotateAnimation : ScreenAnimation
    {
        public Vector3 startRotation = Vector3.zero;
        public Vector3 endRotation = Vector3.zero;
        public bool useOriginalRotation = true;
        public Ease ease = Ease.OutCubic;

        public override Sequence CreateSequence(Screen screen)
        {
            Vector3 to = useOriginalRotation
                ? screen.RectTransform.localEulerAngles
                : endRotation;

            screen.RectTransform.localEulerAngles = startRotation;

            return Sequence.Create()
                .Group(Tween.Rotation(
                    screen.RectTransform,
                    to,
                    duration,
                    ease
                ));
        }
    }
}