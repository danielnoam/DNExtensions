using DNExtensions.Systems.ControllerRumble;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace DNExtensions.Systems.FirstPersonController
{
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FPCMovement))]
    [RequireComponent(typeof(FPCInteraction))]
    [RequireComponent(typeof(FPCInput))]
    [RequireComponent(typeof(FPCRigidBodyPush))]
    [RequireComponent(typeof(CharacterController))]
    public class FpcManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, AutoGetSelf] private FPCMovement fpcMovement;
        [SerializeField, AutoGetSelf] private FPCInteraction fpcInteraction;
        [SerializeField, AutoGetSelf] private FPCCamera fpcCamera;
        [SerializeField, AutoGetSelf] private FPCInput fpcInput;
        [SerializeField, AutoGetSelf] private FPCRigidBodyPush fpcRigidBodyPush;
        [SerializeField, AutoGetSelf] private CharacterController characterController;
        [SerializeField, AutoGetSelf] private ControllerRumbleSource controllerRumbleSource;
        
        
        public FPCMovement FpcMovement => fpcMovement;
        public FPCInteraction FpcInteraction => fpcInteraction;
        public FPCCamera FpcCamera => fpcCamera;
        public FPCInput FpcInput => fpcInput;
        public FPCRigidBodyPush FpcRigidBodyPush => fpcRigidBodyPush;
        public CharacterController CharacterController => characterController;
        public ControllerRumbleSource ControllerRumbleSource => controllerRumbleSource;
    }
}