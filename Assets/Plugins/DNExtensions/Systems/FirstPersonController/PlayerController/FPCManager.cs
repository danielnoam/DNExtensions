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
    public class FpcManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, AutoGetSelf] private FPCInput fpcInput;
        [SerializeField, AutoGetSelf] private FPCLocomotion fpcLocomotion;
        [SerializeField, AutoGetSelf] private FPCInteraction fpcInteraction;
        [SerializeField, AutoGetSelf] private FPCCamera fpcCamera;
        [SerializeField, AutoGetSelf] private FPCEffects fpcEffects;
        [SerializeField, AutoGetSelf] private FPCRigidBodyPush fpcRigidBodyPush;
        [SerializeField, AutoGetSelf] private CharacterController characterController;
        [SerializeField, AutoGetSelf] private ControllerRumbleSource controllerRumbleSource;
        [SerializeField, AutoGetSelf] private CinemachineImpulseSource cinemachineImpulseSource;

        public FPCInput FpcInput => fpcInput;
        public FPCLocomotion FpcLocomotion => fpcLocomotion;
        public FPCEffects FpcEffects => fpcEffects;
        public FPCInteraction FpcInteraction => fpcInteraction;
        public FPCCamera FpcCamera => fpcCamera;
        public FPCRigidBodyPush FpcRigidBodyPush => fpcRigidBodyPush;
        public CharacterController CharacterController => characterController;
        public ControllerRumbleSource ControllerRumbleSource => controllerRumbleSource;
        public CinemachineImpulseSource CinemachineImpulseSource => cinemachineImpulseSource;

        [Button]
        private void ValidateMissingComponents()
        {
            if (!fpcInput) fpcInput = this.GetOrAddComponent<FPCInput>();
            if (!fpcLocomotion) fpcLocomotion = this.GetOrAddComponent<FPCLocomotion>();
            if (!fpcInteraction) fpcInteraction = this.GetOrAddComponent<FPCInteraction>();
            if (!fpcCamera) fpcCamera = this.GetOrAddComponent<FPCCamera>();
            if (!fpcEffects) fpcEffects = this.GetOrAddComponent<FPCEffects>();
            if (!fpcRigidBodyPush) fpcRigidBodyPush = this.GetOrAddComponent<FPCRigidBodyPush>();
            if (!characterController) characterController = this.GetOrAddComponent<CharacterController>();
            if (!controllerRumbleSource) controllerRumbleSource = this.GetOrAddComponent<ControllerRumbleSource>();
            if (!cinemachineImpulseSource) cinemachineImpulseSource = this.GetOrAddComponent<CinemachineImpulseSource>();
        }
    }
}