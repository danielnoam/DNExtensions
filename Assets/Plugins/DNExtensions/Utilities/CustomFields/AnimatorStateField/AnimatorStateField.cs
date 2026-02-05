using System;
using UnityEngine;

namespace DNExtensions.Utilities.CustomFields
{
    /// <summary>
    /// Type-safe reference to an Animator state with editor validation
    /// </summary>
    [Serializable]
    public struct AnimatorStateField
    {
        public string StateName => stateName;
        public int StateHash => stateHash;
        public bool IsAssigned => assigned;
        public Animator Animator => animator;

        [SerializeField] private Animator animator;
        [SerializeField] private string stateName;
        [SerializeField] private int stateHash;
        [SerializeField] private bool assigned;

        public AnimatorStateField(string stateName, Animator animator = null)
        {
            this.animator = animator;
            this.stateName = stateName;
            stateHash = Animator.StringToHash(stateName);
            assigned = !string.IsNullOrEmpty(stateName);
        }

        public static implicit operator int(AnimatorStateField field) => field.stateHash;
        public static implicit operator string(AnimatorStateField field) => field.stateName;
    }

    public static class AnimatorStateFieldExtensions
    {
        /// <summary>
        /// Play the animation state on the specified animator
        /// </summary>
        public static void Play(this Animator animator, AnimatorStateField state, int layer = -1, float normalizedTime = float.NegativeInfinity)
        {
            if (!state.IsAssigned) return;
            animator.Play(state.StateHash, layer, normalizedTime);
        }

        /// <summary>
        /// Cross-fade to the animation state on the specified animator
        /// </summary>
        public static void CrossFade(this Animator animator, AnimatorStateField state, float duration, int layer = -1, float normalizedTime = 0f)
        {
            if (!state.IsAssigned) return;
            animator.CrossFade(state.StateHash, duration, layer, normalizedTime);
        }
    }
}