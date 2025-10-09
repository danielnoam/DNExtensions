
using System;
using DNExtensions;
using UnityEngine;

[SelectionBase]
[DisallowMultipleComponent]
[RequireComponent(typeof(FPCMovement))]
[RequireComponent(typeof(FPCInteraction))]
[RequireComponent(typeof(FPCCamera))]
[RequireComponent(typeof(FPCMovement))]
[RequireComponent(typeof(FPCInput))]
[RequireComponent(typeof(FPCRigidBodyPush))]
[RequireComponent(typeof(CharacterController))]
public class FPCManager : MonoBehaviour
{

    
    [Header("References")]
    [SerializeField] private FPCMovement fpcMovement;
    [SerializeField] private FPCInteraction fpcInteraction;
    [SerializeField] private FPCCamera fpcCamera;
    [SerializeField] private FPCInput fpcInput;
    [SerializeField] private FPCRigidBodyPush fpcRigidBodyPush;
    [SerializeField] private CharacterController characterController;

    
    public FPCMovement FPCMovement => fpcMovement;
    public FPCInteraction FPCInteraction => fpcInteraction;
    public FPCCamera FPCCamera => fpcCamera;
    public FPCInput FPCInput => fpcInput;
    public FPCRigidBodyPush FPCRigidBodyPush => fpcRigidBodyPush;
    public CharacterController CharacterController => characterController;

    private void OnValidate()
    {
        if (!fpcMovement) fpcMovement = gameObject.GetOrAddComponent<FPCMovement>();
        if (!fpcInteraction) fpcInteraction = gameObject.GetOrAddComponent<FPCInteraction>();
        if (!fpcCamera) fpcCamera = gameObject.GetOrAddComponent<FPCCamera>();
        if (!fpcInput) fpcInput = gameObject.GetOrAddComponent<FPCInput>();
        if (!fpcRigidBodyPush) fpcRigidBodyPush = gameObject.GetOrAddComponent<FPCRigidBodyPush>();
        if (!characterController) characterController = gameObject.GetOrAddComponent<CharacterController>();
    }
}