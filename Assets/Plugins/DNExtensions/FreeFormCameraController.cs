
using DNExtensions.Utilities;
using DNExtensions.Utilities.RangedValues;
using UnityEngine;



[RequireComponent(typeof(Camera))]
public class FreeFormCameraController : MonoBehaviour
{

    [InfoBox("Camera Controls:\n" +
             "- Move: WASD or Arrow Keys\n" +
             "- Move Up/Down: Space / Left Ctrl\n" +
             "- Fast Movement: Left Shift\n" +
             "- Rotate: Right Mouse Button + Mouse Move\n" +
             "- Zoom: Mouse Scroll Wheel", InfoBoxType.Info)]
    
    [Header("Movement Settings")]
    [SerializeField, Min(1f)] private float moveSpeed = 10;
    [SerializeField, Range(1, 10)] private float sprintMultiplier = 2f;
    
    [Header("Rotation Settings")]
    [SerializeField] private CameraRotationMode rotationMode = CameraRotationMode.Always;
    [SerializeField, Min(1f)] private float rotationSpeed = 100f;
    [SerializeField, Min(0.1f)] private float rotationSmoothing = 1f;
    [SerializeField] private bool invertY;
    [SerializeField] private bool invertX; 
    
    [Header("Zoom Settings")]
    [SerializeField, Min(1f)] private float zoomSpeed = 10f;
    [SerializeField, MinMaxRange(0, 50)] private RangedFloat zoomLimits = new RangedFloat(5f, 50f);
    
    [Header("References")]
    [SerializeField] private new Camera camera;
    

    

    
    private enum CameraRotationMode
    {
        RightMouseButton,
        Always
    }



    private void OnValidate()
    {
        if (!camera) camera = GetComponent<Camera>();
    }


    private void Awake()
    {
        if (rotationMode == CameraRotationMode.Always)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();
    }
    
    private void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        float speedMultiplier = Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f;
        
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        
        if (Input.GetKey(KeyCode.Space))
        {
            move += Vector3.up;
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            move += Vector3.down;
        }
        
        transform.position += move * (moveSpeed * speedMultiplier * Time.deltaTime);
    }
    private void HandleRotation()
    {

        if (rotationMode == CameraRotationMode.RightMouseButton)
        {
            Cursor.lockState = Input.GetMouseButton(1) ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !Input.GetMouseButton(1);
        }
        
        if (rotationMode == CameraRotationMode.RightMouseButton && !Input.GetMouseButton(1)) return;

        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime * (invertX ? -1 : 1);
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime * (invertY ? -1 : 1);

        Quaternion targetRotation = Quaternion.Euler(transform.eulerAngles.x - mouseY, transform.eulerAngles.y + mouseX, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing);
        
    }
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            float newFOV = camera.fieldOfView - scroll * zoomSpeed * 100f * Time.deltaTime;
            camera.fieldOfView = Mathf.Clamp(newFOV, zoomLimits.minValue, zoomLimits.maxValue);
        }
    }
}



