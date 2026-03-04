using DNExtensions.Systems.ControllerRumble;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace DNExtensions.Systems.FirstPersonController
{
    /// <summary>
    /// Handles first-person camera effects including FOV changes, headbob, tilt, fall kick, and landing rumble.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FpcManager))]
    [AddComponentMenu("DNExtensions/First Person Controller/FPC Effects")]
    public class FPCEffects : MonoBehaviour
    {
        [Header("FOV")]
        [Tooltip("Enables camera FOV change when running")]
        [SerializeField] private bool enableFov = true;
        [SerializeField] private float runFovMultiplier = 1.3f;
        [SerializeField] private float fovSmoothing = 5f;

        [Header("Headbob")]
        [Tooltip("Enables camera headbob when moving")]
        [SerializeField] private bool enableHeadbob = true;
        [SerializeField] private float bobFrequency = 5f;
        [SerializeField] private float bobAmplitudeWalk = 0.1f;
        [SerializeField] private float bobAmplitudeRun = 0.2f;
        [SerializeField] private float bobSmoothing = 10f;

        [Header("Tilt")]
        [Tooltip("Enables camera tilt when moving")]
        [SerializeField] private bool enableTilt = true;
        [SerializeField] private float rollTilt = 0.1f;
        [SerializeField] private float pitchTilt = 0.05f;
        [SerializeField] private float tiltSmoothing = 10f;
        
        [Header("Fall Kick")]
        [Tooltip("Enables camera fall kick when falling")]
        [SerializeField] private bool enableFallKick = true;
        [SerializeField] private float fallKickDuration = 0.3f;
        [SerializeField, MinMaxRange(0,10)] private RangedFloat fallKickVelocityRange = new RangedFloat(0, 2);
        [SerializeField, MinMaxRange(0,10)] private RangedFloat fallKickStrengthRange = new RangedFloat(0.1f, 0.3f);
        
        [Header("Land Rumble")]
        [Tooltip("Enables controller rumble on landing")]
        [SerializeField] private bool enableLandingRumble = true;
        [SerializeField] private ControllerRumbleEffectSettings landingRumble = new ControllerRumbleEffectSettings(0.5f, 0.7f, 0.2f);
        
        [SerializeField, AutoGetSelf, HideInInspector] private FpcManager manager;

        private float _baseFov;
        private Vector3 _cameraBasePosition;
        private Vector3 _cameraBaseRotation;

        private float _kickTime;
        private Vector3 _kickPositionOffset;
        private Vector3 _kickRotationOffset;
        
        private float _bobTimer;
        private float _bobAmplitude;
        private float _bobAmplitudeVelocity;
        private Vector3 _bobOffset;
        
        private Vector3 _tiltOffset;

        private void Awake()
        {
            if (manager.FpcCamera.Cam)
            {
                _baseFov = manager.FpcCamera.Cam.Lens.FieldOfView;
                _cameraBasePosition = manager.FpcCamera.Cam.transform.localPosition;
                _cameraBaseRotation = manager.FpcCamera.Cam.transform.localEulerAngles;
            }
            else
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            manager.FpcLocomotion.OnLanded += OnLanded;
        }

        private void OnDisable()
        {
            manager.FpcLocomotion.OnLanded -= OnLanded;
        }

        private void OnLanded(float landVelocity)
        {
            if (enableLandingRumble) manager.ControllerRumbleSource?.Rumble(landingRumble);

            if (enableFallKick && landVelocity >= fallKickVelocityRange.minValue)
            {
                float impact = Mathf.Lerp(
                    fallKickStrengthRange.minValue,
                    fallKickStrengthRange.maxValue,
                    Mathf.Clamp01((landVelocity - fallKickVelocityRange.minValue) / (fallKickVelocityRange.maxValue - fallKickVelocityRange.minValue))
                );

                KickCamera(impact, impact, Vector3.down, Vector3.right, fallKickDuration);
            }
        }

        private void Update()
        {
            UpdateFov();
            UpdateHeadbob();
            UpdateKick();
            UpdateTilt();

            manager.FpcCamera.Cam.transform.localPosition = _cameraBasePosition + _bobOffset + _kickPositionOffset;
            manager.FpcCamera.Cam.transform.localEulerAngles = _cameraBaseRotation + _tiltOffset + _kickRotationOffset;
        }

        private void UpdateKick()
        {
            float t = Time.deltaTime / _kickTime;
            _kickPositionOffset = Vector3.Lerp(_kickPositionOffset, Vector3.zero, t);
            _kickRotationOffset = Vector3.Lerp(_kickRotationOffset, Vector3.zero, t);
        }

        private void UpdateFov()
        {
            if (!enableFov) return;

            float targetFov = _baseFov * (manager.FpcLocomotion.IsRunning ? runFovMultiplier : 1f);

            manager.FpcCamera.Cam.Lens.FieldOfView = Mathf.Lerp(
                manager.FpcCamera.Cam.Lens.FieldOfView,
                targetFov,
                Time.deltaTime * fovSmoothing
            );
        }

        private void UpdateHeadbob()
        {
            if (!enableHeadbob) return;

            bool isMoving = manager.FpcLocomotion.IsGrounded && manager.FpcInput.MoveInput.sqrMagnitude > 0.01f;

            float targetAmplitude = 0f;
            float frequency = bobFrequency;

            if (isMoving)
            {
                targetAmplitude = manager.FpcLocomotion.IsRunning ? bobAmplitudeRun : bobAmplitudeWalk;
                frequency = bobFrequency * (manager.FpcLocomotion.IsRunning ? 1.5f : 1f);
            }

            _bobAmplitude = Mathf.SmoothDamp(_bobAmplitude, targetAmplitude, ref _bobAmplitudeVelocity, 1f / bobSmoothing);
            _bobTimer += Time.deltaTime * frequency;

            _bobOffset = new Vector3(
                Mathf.Sin(_bobTimer) * _bobAmplitude * 0.5f,
                Mathf.Abs(Mathf.Sin(_bobTimer)) * _bobAmplitude,
                0f
            );
        }

        private void UpdateTilt()
        {
            if (!enableTilt)
            {
                _tiltOffset = Vector3.zero;
                return;
            }

            Vector3 velocity = manager.FpcLocomotion.Velocity;

            float targetRoll = Vector3.Dot(velocity, transform.right) * -rollTilt;
            float targetPitch = Vector3.Dot(velocity, transform.forward) * pitchTilt;

            _tiltOffset.x = Mathf.LerpAngle(_tiltOffset.x, targetPitch, Time.deltaTime * tiltSmoothing);
            _tiltOffset.z = Mathf.LerpAngle(_tiltOffset.z, targetRoll, Time.deltaTime * tiltSmoothing);
        }

        /// <summary>
        /// Applies a camera kick effect with position and rotation offsets.
        /// </summary>
        public void KickCamera(float positionStrength, float rotationStrength, Vector3 positionDirection, Vector3 rotationDirection, float kickTime)
        {
            _kickPositionOffset += positionDirection * positionStrength;
            _kickRotationOffset += rotationDirection * rotationStrength;
            _kickTime += kickTime;
        }
    }
}