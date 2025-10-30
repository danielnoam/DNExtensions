
using UnityEngine;
using Unity.Cinemachine;


[DisallowMultipleComponent]
[RequireComponent(typeof(FPCManager))]
public class FPCCameraCinemachine : FPCCameraBase
{
    [Header("Camera Reference")]
    [SerializeField] private CinemachineCamera cinemachineCamera;

    protected override void UpdateFovInEditor()
    {
        if (cinemachineCamera) 
        {
            cinemachineCamera.Lens.FieldOfView = baseFov;
        }
    }

    protected override void SetFieldOfView(float targetFov)
    {
        if (cinemachineCamera)
        {
            cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(
                cinemachineCamera.Lens.FieldOfView, 
                targetFov, 
                Time.deltaTime * fovChangeSmoothing
            );
        }
    }
}