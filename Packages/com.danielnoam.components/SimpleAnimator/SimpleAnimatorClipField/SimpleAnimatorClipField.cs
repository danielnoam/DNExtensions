using System;
using UnityEngine;

namespace DNExtensions.Utilities.CustomFields
{
    /// <summary>
    /// Inspector-friendly field that references a specific clip on a <see cref="SimpleAnimator"/>.
    /// </summary>
    [Serializable]
    public struct SimpleAnimatorClipField
    {
        public SimpleAnimator SimpleAnimator => simpleAnimator;
        public string ClipName => clipName;
        public int ClipIndex => clipIndex;
        public bool IsAssigned => assigned;

        [SerializeField] private SimpleAnimator simpleAnimator;
        [SerializeField] private string clipName;
        [SerializeField] private int clipIndex;
        [SerializeField] private bool assigned;

        public SimpleAnimatorClipField(SimpleAnimator simpleAnimator, int clipIndex = -1)
        {
            this.simpleAnimator = simpleAnimator;
            this.clipIndex = clipIndex;
            this.clipName = string.Empty;
            this.assigned = false;

            if (!simpleAnimator || clipIndex < 0 || clipIndex >= simpleAnimator.ClipCount) return;

            var clip = simpleAnimator.GetClip(clipIndex);
            if (!clip) return;

            this.clipName = clip.name;
            this.assigned = true;
        }

        public static implicit operator int(SimpleAnimatorClipField field) => field.clipIndex;
        public static implicit operator string(SimpleAnimatorClipField field) => field.clipName;
    }

    public static class SimpleAnimatorClipFieldExtensions
    {
        /// <summary>
        /// Plays the referenced clip and holds the last frame when finished.
        /// </summary>
        public static void Play(this SimpleAnimatorClipField field, float crossfadeDuration = 0f, Action onFinished = null)
        {
            if (!field.IsAssigned || !field.SimpleAnimator) return;

            field.SimpleAnimator.Play(field.ClipIndex, crossfadeDuration, onFinished);
        }

        /// <summary>
        /// Plays the referenced clip once, then stops the graph and releases the transforms.
        /// </summary>
        public static void PlayOnce(this SimpleAnimatorClipField field, float crossfadeDuration = 0f, Action onFinished = null)
        {
            if (!field.IsAssigned || !field.SimpleAnimator) return;

            field.SimpleAnimator.PlayOnce(field.ClipIndex, crossfadeDuration, onFinished);
        }
    }
}