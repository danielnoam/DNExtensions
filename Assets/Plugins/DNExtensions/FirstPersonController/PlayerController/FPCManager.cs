using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.ControllerRumbleSystem;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[SelectionBase]
[DisallowMultipleComponent]
[RequireComponent(typeof(FPCMovement))]
[RequireComponent(typeof(FPCInteraction))]
[RequireComponent(typeof(FPCInput))]
[RequireComponent(typeof(FPCRigidBodyPush))]
[RequireComponent(typeof(CharacterController))]
public class FPCManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private FPCCameraMode cameraMode = FPCCameraMode.CinemachineCamera;
    
    [Header("References")]
    [SerializeField] private FPCMovement fpcMovement;
    [SerializeField] private FPCInteraction fpcInteraction;
    [SerializeField] private FPCCameraBase fpcCamera;
    [SerializeField] private FPCInput fpcInput;
    [SerializeField] private FPCRigidBodyPush fpcRigidBodyPush;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private ControllerRumbleSource controllerRumbleSource;

    private enum FPCCameraMode
    {
        NormalCamera,
        CinemachineCamera
    }
    
    public FPCMovement FPCMovement => fpcMovement;
    public FPCInteraction FPCInteraction => fpcInteraction;
    public FPCCameraBase FPCCamera => fpcCamera;
    public FPCInput FPCInput => fpcInput;
    public FPCRigidBodyPush FPCRigidBodyPush => fpcRigidBodyPush;
    public CharacterController CharacterController => characterController;
    public ControllerRumbleSource ControllerRumbleSource => controllerRumbleSource;

    private void OnValidate()
    {
        if (!fpcMovement) fpcMovement = gameObject.GetOrAddComponent<FPCMovement>();
        if (!fpcInteraction) fpcInteraction = gameObject.GetOrAddComponent<FPCInteraction>();
        if (!fpcInput) fpcInput = gameObject.GetOrAddComponent<FPCInput>();
        if (!fpcRigidBodyPush) fpcRigidBodyPush = gameObject.GetOrAddComponent<FPCRigidBodyPush>();
        if (!characterController) characterController = gameObject.GetOrAddComponent<CharacterController>();
        HandleCameraComponentSwitch();
    }

    private void HandleCameraComponentSwitch()
    {
        Type requiredCameraType = cameraMode switch
        {
            FPCCameraMode.NormalCamera => typeof(FPCCameraNormal),
            FPCCameraMode.CinemachineCamera => typeof(FPCCameraCinemachine),
            _ => null
        };

        if (requiredCameraType == null) return;
        
        FPCCameraBase existingCamera = GetComponent<FPCCameraBase>();

        if (existingCamera && existingCamera.GetType() != requiredCameraType)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += () =>
                {
                    if (this != null && existingCamera != null)
                    {
                        DestroyImmediate(existingCamera);
                        if (cameraMode == FPCCameraMode.NormalCamera)
                        {
                            fpcCamera = gameObject.GetOrAddComponent<FPCCameraNormal>();
                        }
                        else if (cameraMode == FPCCameraMode.CinemachineCamera)
                        {
                            fpcCamera = gameObject.GetOrAddComponent<FPCCameraCinemachine>();
                        }
                    }
                };
            }
            else
            #endif
            {
                Destroy(existingCamera);
                if (cameraMode == FPCCameraMode.NormalCamera)
                {
                    fpcCamera = gameObject.GetOrAddComponent<FPCCameraNormal>();
                }
                else if (cameraMode == FPCCameraMode.CinemachineCamera)
                {
                    fpcCamera = gameObject.GetOrAddComponent<FPCCameraCinemachine>();
                }
            }
        }
        else if (!existingCamera)
        {
            if (cameraMode == FPCCameraMode.NormalCamera)
            {
                fpcCamera = gameObject.GetOrAddComponent<FPCCameraNormal>();
            }
            else if (cameraMode == FPCCameraMode.CinemachineCamera)
            {
                fpcCamera = gameObject.GetOrAddComponent<FPCCameraCinemachine>();
            }
        }
        else
        {
            fpcCamera = existingCamera;
        }
    }
}