using System;
using UnityEngine;

namespace DNExtensions.Utilities.CustomFields
{
    public enum AnimatorParameterType { Trigger, Bool, Int, Float }
    public enum AnimatorSource { Component, Asset }

    [Serializable]
    public struct AnimatorParameterField
    {
        public string ParameterName => parameterName;
        public int ParameterHash => parameterHash;
        public bool IsAssigned => !string.IsNullOrEmpty(parameterName);
        public AnimatorParameterType ParameterType => parameterType;
        public AnimatorSource Source => source;
        public Animator ComponentAnimator => componentAnimator;
        public RuntimeAnimatorController AssetController => assetController;

        [SerializeField] private AnimatorParameterType parameterType;
        [SerializeField] private AnimatorSource source;
        [SerializeField] private Animator componentAnimator;
        [SerializeField] private RuntimeAnimatorController assetController;
        [SerializeField] private string parameterName;
        [SerializeField] private int parameterHash;

        public AnimatorParameterField(AnimatorParameterType parameterType, AnimatorSource source = AnimatorSource.Component)
        {
            this.parameterType = parameterType;
            this.source = source;
            componentAnimator = null;
            assetController = null;
            parameterName = string.Empty;
            parameterHash = 0;
        }

        public static implicit operator string(AnimatorParameterField field) => field.parameterName;
        public static implicit operator int(AnimatorParameterField field) => field.parameterHash;
    }

    public static class AnimatorParameterFieldExtensions
    {
        public static void SetTrigger(this Animator animator, AnimatorParameterField field)
        {
            if (!field.IsAssigned) return;
            animator.SetTrigger(field.ParameterHash);
        }

        public static void SetBool(this Animator animator, AnimatorParameterField field, bool value)
        {
            if (!field.IsAssigned) return;
            animator.SetBool(field.ParameterHash, value);
        }

        public static void SetInt(this Animator animator, AnimatorParameterField field, int value)
        {
            if (!field.IsAssigned) return;
            animator.SetInteger(field.ParameterHash, value);
        }

        public static void SetFloat(this Animator animator, AnimatorParameterField field, float value)
        {
            if (!field.IsAssigned) return;
            animator.SetFloat(field.ParameterHash, value);
        }
    }
}