using System;
using DNExtensions.Systems.ControllerRumble;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.Button;
using Unity.Cinemachine;
using UnityEngine;

namespace DNExtensions.Systems.FirstPersonController
{
    /// <summary>
    /// FPC Manager. All FPC systems are referenced here for easy access.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [AddComponentMenu("DNExtensions/FPC Manager")]
    public class FpcManager : MonoBehaviour
    {
        public enum LocomotionMode
        {
            Standard,
            Momentum
        }

        [Header("Locomotion")]
        [Tooltip("Which locomotion component the player uses, changing this swaps the component")]
        [SerializeField] private LocomotionMode locomotionMode = LocomotionMode.Standard;

        [Header("References")]
        [SerializeField, AutoGetSelf] private FPCInput fpcInput;
        [SerializeField, AutoGetSelf] private FPCLocomotionBase fpcLocomotion;
        [SerializeField, AutoGetSelf] private FPCInteraction fpcInteraction;
        [SerializeField, AutoGetSelf] private FPCCamera fpcCamera;
        [SerializeField, AutoGetSelf] private FPCEffects fpcEffects;
        [SerializeField, AutoGetSelf] private FPCRigidBodyPush fpcRigidBodyPush;
        [SerializeField, AutoGetSelf] private CharacterController characterController;
        [SerializeField, AutoGetSelf] private ControllerRumbleSource controllerRumbleSource;
        [SerializeField, AutoGetSelf] private CinemachineImpulseSource cinemachineImpulseSource;

        public FPCInput FpcInput => fpcInput;
        public FPCLocomotionBase FpcLocomotion => fpcLocomotion;
        public FPCEffects FpcEffects => fpcEffects;
        public FPCInteraction FpcInteraction => fpcInteraction;
        public FPCCamera FpcCamera => fpcCamera;
        public FPCRigidBodyPush FpcRigidBodyPush => fpcRigidBodyPush;
        public CharacterController CharacterController => characterController;
        public ControllerRumbleSource ControllerRumbleSource => controllerRumbleSource;
        public CinemachineImpulseSource CinemachineImpulseSource => cinemachineImpulseSource;
        public LocomotionMode CurrentLocomotionMode => locomotionMode;

        public event Action<FPCLocomotionBase> OnLocomotionChanged;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
            RefreshLocomotionMode();
        }

        /// <summary>
        /// Swaps the locomotion component for the one matching the given mode, carrying the current velocity over.
        /// </summary>
        public FPCLocomotionBase SetLocomotionMode(LocomotionMode mode)
        {
            locomotionMode = mode;
            return ApplyLocomotionMode();
        }

        private FPCLocomotionBase ApplyLocomotionMode()
        {
            Type wantedType = GetLocomotionType(locomotionMode);
            FPCLocomotionBase wantedLocomotion = null;
            Vector3 velocity = Vector3.zero;
            bool hasVelocityToCarry = false;

            foreach (FPCLocomotionBase locomotion in GetComponents<FPCLocomotionBase>())
            {
                if (!wantedLocomotion && locomotion.GetType() == wantedType)
                {
                    wantedLocomotion = locomotion;
                    continue;
                }

                velocity = locomotion.Velocity;
                hasVelocityToCarry = true;
                locomotion.enabled = false;
                DestroyLocomotion(locomotion);
            }

            if (!wantedLocomotion)
            {
                wantedLocomotion = (FPCLocomotionBase)gameObject.AddComponent(wantedType);
            }

            if (hasVelocityToCarry)
            {
                wantedLocomotion.SetVelocity(velocity);
            }

            if (fpcLocomotion == wantedLocomotion) return wantedLocomotion;

            fpcLocomotion = wantedLocomotion;
            OnLocomotionChanged?.Invoke(wantedLocomotion);
            return wantedLocomotion;
        }

        private bool IsLocomotionModeApplied()
        {
            FPCLocomotionBase[] locomotions = GetComponents<FPCLocomotionBase>();

            return locomotions.Length == 1 && locomotions[0] == fpcLocomotion && fpcLocomotion.GetType() == GetLocomotionType(locomotionMode);
        }

        private static Type GetLocomotionType(LocomotionMode mode)
        {
            return mode switch
            {
                LocomotionMode.Momentum => typeof(FPCMomentumLocomotion),
                _ => typeof(FPCStandardLocomotion)
            };
        }

        private void DestroyLocomotion(FPCLocomotionBase locomotion)
        {
            if (Application.isPlaying)
            {
                Destroy(locomotion);
                return;
            }

            DestroyImmediate(locomotion, true);
        }

        private void RefreshLocomotionMode()
        {
            if (IsLocomotionModeApplied()) return;

            if (Application.isPlaying)
            {
                ApplyLocomotionMode();
                return;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (!this || IsLocomotionModeApplied()) return;

                ApplyLocomotionMode();
                UnityEditor.EditorUtility.SetDirty(gameObject);
            };
#endif
        }

        [Button(ButtonPlayMode.OnlyWhenNotPlaying)]
        private void ValidateMissingComponents()
        {
            if (!fpcInput) fpcInput = this.GetOrAddComponent<FPCInput>();
            if (!fpcInteraction) fpcInteraction = this.GetOrAddComponent<FPCInteraction>();
            if (!fpcCamera) fpcCamera = this.GetOrAddComponent<FPCCamera>();
            if (!fpcEffects) fpcEffects = this.GetOrAddComponent<FPCEffects>();
            if (!fpcRigidBodyPush) fpcRigidBodyPush = this.GetOrAddComponent<FPCRigidBodyPush>();
            if (!characterController) characterController = this.GetOrAddComponent<CharacterController>();
            if (!controllerRumbleSource) controllerRumbleSource = this.GetOrAddComponent<ControllerRumbleSource>();
            if (!cinemachineImpulseSource) cinemachineImpulseSource = this.GetOrAddComponent<CinemachineImpulseSource>();
            ApplyLocomotionMode();
        }
    }
}
