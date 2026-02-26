
using DNExtensions.Utilities.AutoGet;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.Utilities
{
    
    public enum CameraRotationMode
    { 
        Disabled = 0,
        RightMouseButton = 1,
        Always = 2,
    }

    /// <summary>
    /// Free camera controller with movement, rotation and zoom.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("DNExtensions/Free Form Camera Controller", -1000)]
    public class FreeFormCameraController : MonoBehaviour
    {

        [InfoBox("Camera Controls:\n" +
                 "- Move: WASD or Arrow Keys\n" +
                 "- Move Up/Down: Space / Left Ctrl\n" +
                 "- Fast Movement: Left Shift\n" +
                 "- Rotate: Right Mouse Button + Mouse Move\n" +
                 "- Zoom: Mouse Scroll Wheel", 
        InfoBoxType.Info)]
        [Space(20)]
        
        [Header("Settings")]
        [SerializeField] private bool lockCursor = true;
        
        [Header("Movement")]
        [SerializeField] private bool allowMovement = true;
        [SerializeField, Min(1f)] private float moveSpeed = 10;
        [SerializeField, Range(1, 10)] private float fastMultiplier = 2f;

        [Header("Rotation")] 
        [SerializeField] private CameraRotationMode rotationMode = CameraRotationMode.Always;
        [SerializeField, Min(1f)] private float rotationSpeed = 100f;
        [SerializeField, Min(0.1f)] private float rotationSmoothing = 1f;
        [SerializeField] private bool invertY;
        [SerializeField] private bool invertX;

        [Header("Zoom")] 
        [SerializeField] private bool allowZoom = true;
        [SerializeField, Min(1f)] private float zoomSpeed = 10f;
        [SerializeField, MinMaxRange(0, 50)] private RangedFloat zoomLimits = new RangedFloat(5f, 50f);


        [SerializeField, AutoGetSelf, HideInInspector] private Camera cam;
        

        private void Awake()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            if (!cam) return;
            
            HandleMovement();
            HandleRotation();
            HandleZoom();
        }

        private void HandleMovement()
        {
            if (Keyboard.current == null || !allowMovement) return;
            
            Vector2 moveInput = Keyboard.current != null ? new Vector2(
                (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0),
                (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0)
            ) : Vector2.zero;

            float speedMultiplier = Keyboard.current.leftShiftKey.isPressed ? fastMultiplier : 1f;

            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

            if (Keyboard.current.spaceKey.isPressed)
                move += Vector3.up;
            else if (Keyboard.current.leftCtrlKey.isPressed)
                move += Vector3.down;

            transform.position += move * (moveSpeed * speedMultiplier * Time.deltaTime);
        }

        private void HandleRotation()
        {
            if (Mouse.current == null || rotationMode == CameraRotationMode.Disabled) return;
            
            if (rotationMode == CameraRotationMode.RightMouseButton)
            {
                bool rightMouseHeld = Mouse.current.rightButton.isPressed;
                Cursor.lockState = rightMouseHeld ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !rightMouseHeld;

                if (!rightMouseHeld) return;
            }

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            float mouseX = mouseDelta.x * rotationSpeed * Time.deltaTime * (invertX ? -1 : 1);
            float mouseY = mouseDelta.y * rotationSpeed * Time.deltaTime * (invertY ? -1 : 1);

            Quaternion targetRotation = Quaternion.Euler(transform.eulerAngles.x - mouseY, transform.eulerAngles.y + mouseX, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing);
        }

        private void HandleZoom()
        {
            if (Mouse.current == null || !allowZoom) return;
            
            float scroll = Mouse.current.scroll.y.value;
            if (scroll != 0f)
            {
                float newFOV = cam.fieldOfView - scroll * zoomSpeed * 100f * Time.deltaTime;
                cam.fieldOfView = Mathf.Clamp(newFOV, zoomLimits.minValue, zoomLimits.maxValue);
            }
        }
        
        public void EnableControl(bool move = true, CameraRotationMode cameraRotationMode = CameraRotationMode.Always, bool zoom = true)
        {
            allowMovement = move;
            rotationMode = cameraRotationMode;
            allowZoom = zoom;
        }
    }
}