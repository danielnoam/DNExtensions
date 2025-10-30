using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(FPCManager))]
public class FPCCameraNormal : FPCCameraBase
{
    [Header("Camera Reference")]
    [SerializeField] private Camera normalCamera;

    protected override void UpdateFovInEditor()
    {
        if (normalCamera) 
        {
            normalCamera.fieldOfView = baseFov;
        }
    }

    protected override void SetFieldOfView(float targetFov)
    {
        if (normalCamera)
        {
            normalCamera.fieldOfView = Mathf.Lerp(
                normalCamera.fieldOfView, 
                targetFov, 
                Time.deltaTime * fovChangeSmoothing
            );
        }
    }
}