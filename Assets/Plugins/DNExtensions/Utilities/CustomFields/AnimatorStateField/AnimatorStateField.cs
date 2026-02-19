using System;
using UnityEngine;

namespace DNExtensions.Utilities.CustomFields
{
    [Serializable]
    public struct AnimatorStateField
    {
        public string StateName => stateName;
        public int StateHash => stateHash;
        public bool IsAssigned => assigned;
        public Animator Animator => animator;
        public AnimatorSource Source => source;
        public RuntimeAnimatorController AssetController => assetController;

        [SerializeField] private Animator animator;
        [SerializeField] private RuntimeAnimatorController assetController;
        [SerializeField] private AnimatorSource source;
        [SerializeField] private string stateName;
        [SerializeField] private int stateHash;
        [SerializeField] private bool assigned;

        public AnimatorStateField(string stateName, Animator animator = null)
        {
            this.animator = animator;
            this.assetController = null;
            this.source = AnimatorSource.Component;
            this.stateName = stateName;
            stateHash = Animator.StringToHash(stateName);
            assigned = !string.IsNullOrEmpty(stateName);
        }

        public static implicit operator int(AnimatorStateField field) => field.stateHash;
        public static implicit operator string(AnimatorStateField field) => field.stateName;
    }

    public static class AnimatorStateFieldExtensions
    {
        public static void Play(this Animator animator, AnimatorStateField state, int layer = -1, float normalizedTime = float.NegativeInfinity)
        {
            if (!state.IsAssigned) return;
            animator.Play(state.StateHash, layer, normalizedTime);
        }

        public static void CrossFade(this Animator animator, AnimatorStateField state, float duration, int layer = -1, float normalizedTime = 0f)
        {
            if (!state.IsAssigned) return;
            animator.CrossFade(state.StateHash, duration, layer, normalizedTime);
        }
    }
}