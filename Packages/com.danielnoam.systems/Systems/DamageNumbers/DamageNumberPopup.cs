using DNExtensions.Systems.ObjectPooling;
using TMPro;
using UnityEngine;

namespace DNExtensions.Systems.DamageNumbers
{
    [AddComponentMenu("DNExtensions/Damage Number Popup")]
    [DisallowMultipleComponent]
    public class DamageNumberPopup : MonoBehaviour, IPoolable
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField] private float lifetime = 1f;
        [SerializeField] private float jumpSpeed = 3f;
        [SerializeField] private float gravity = 4f;
        [SerializeField] private float spreadRadius = 0.5f;
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Header("Billboard")]
        [SerializeField] private bool rotateToCamera = true;
        [SerializeField] private float rotationSpeed = 25f;

        private float _timer;
        private float _height;
        private float _verticalVelocity;
        private Vector3 _startPosition;
        private Vector3 _horizontalOffset;
        private Camera _cam;

        public void Show(DamageNumberInfo info)
        {
            text.text = string.IsNullOrEmpty(info.Text) ? Mathf.RoundToInt(info.Amount).ToString() : info.Text;
            text.color = info.IsCritical ? criticalColor : normalColor;

            Vector2 randomOffset = Random.insideUnitCircle * spreadRadius;
            _horizontalOffset = new Vector3(randomOffset.x, 0f, randomOffset.y);
            _startPosition = transform.position;

            _timer = 0f;
            _height = 0f;
            _verticalVelocity = jumpSpeed;
            transform.localScale = Vector3.one * scaleCurve.Evaluate(0f);
            text.alpha = alphaCurve.Evaluate(0f);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float t = Mathf.Clamp01(_timer / lifetime);

            _verticalVelocity -= gravity * Time.deltaTime;
            _height = Mathf.Max(0f, _height + _verticalVelocity * Time.deltaTime);

            transform.position = _startPosition + _horizontalOffset * t + Vector3.up * _height;

            text.alpha = alphaCurve.Evaluate(t);
            transform.localScale = Vector3.one * scaleCurve.Evaluate(t);

            if (rotateToCamera) RotateToCamera();

            if (_timer >= lifetime)
            {
                ObjectPooler.ReturnObjectToPool(gameObject);
            }
        }

        private void RotateToCamera()
        {
            if (!_cam) _cam = Camera.main;
            if (!_cam) return;

            Vector3 directionToCamera = transform.position - _cam.transform.position;
            directionToCamera.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        public void OnPoolGet()
        {
            if (!_cam) _cam = Camera.main;
        }

        public void OnPoolReturn()
        {
        }

        public void OnPoolRecycle()
        {
        }
    }
}
