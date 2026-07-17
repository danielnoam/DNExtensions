using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace DNExtensions.Utilities
{
    [AddComponentMenu("DNExtensions/Flying Agent")]
    [RequireComponent(typeof(Rigidbody))]
    public class FlyingAgent : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float moveSpeed = 17f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 15f;

        [Header("Height")]
        [SerializeField] private float flightHeight = 14f;
        [SerializeField] private float heightAdjustSpeed = 10f;
        [SerializeField] private float heightCheckDistance = 30f;
        [SerializeField] private float lookAheadDistance = 12f;
        [SerializeField] private LayerMask groundMask;

        [Header("Hover")]
        [SerializeField] private float hoverAmplitude = 0.2f;
        [SerializeField] private float hoverFrequency = 1f;

        [Header("Obstacle Avoidance")]
        [SerializeField] private float avoidanceRadius = 4f;
        [SerializeField] private float avoidanceForce = 25f;
        [SerializeField] private LayerMask separationMask;

        [SerializeField, AutoGetSelf, HideInInspector] private Rigidbody rigidBody;

        private const float ArrivalThreshold = 3f;
        private const int SeparationCheckInterval = 5;

        private int _separationFrameOffset;
        private static int _frameOffsetCounter;

        public Vector3 Destination { get; private set; }
        public bool HasPath { get; private set; }
        public bool UpdateRotation { get; set; } = true;
        public float FlightHeight => flightHeight;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void Awake()
        {
            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
            _separationFrameOffset = _frameOffsetCounter % SeparationCheckInterval;
            _frameOffsetCounter++;
        }

        public void SetDestination(Vector3 dest)
        {
            Destination = new Vector3(dest.x, 0, dest.z);
            HasPath = true;
        }

        public void ResetPath()
        {
            HasPath = false;
        }

        private void FixedUpdate()
        {
            var velocity = rigidBody.linearVelocity;

            if (!HasPath)
            {
                ApplyHover(ref velocity);
                rigidBody.linearVelocity = velocity;
                return;
            }

            if ((Time.frameCount + _separationFrameOffset) % SeparationCheckInterval == 0) ApplySeparation();

            var posXZ = new Vector3(transform.position.x, 0, transform.position.z);
            if (Vector3.Distance(posXZ, Destination) > ArrivalThreshold)
            {
                MaintainAltitude(ref velocity);
                FlyToward(ref velocity);
            }
            else
            {
                ApplyHover(ref velocity);
            }

            rigidBody.linearVelocity = velocity;
        }

        private void MaintainAltitude(ref Vector3 velocity)
        {
            if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, heightCheckDistance, groundMask))
            {
                velocity.y = -heightAdjustSpeed;
                return;
            }

            float groundY = hit.point.y;

            Vector3 horizontalDir = Destination - transform.position;
            horizontalDir.y = 0;
            if (horizontalDir.sqrMagnitude > 0.01f && TrySampleGroundAhead(horizontalDir.normalized, out float aheadY))
            {
                groundY = Mathf.Max(groundY, aheadY);
            }

            float desiredY = groundY + flightHeight;
            float correction = (desiredY - transform.position.y) * heightAdjustSpeed;
            velocity.y = Mathf.MoveTowards(velocity.y, correction, heightAdjustSpeed * Time.fixedDeltaTime);
        }

        private bool TrySampleGroundAhead(Vector3 direction, out float groundY)
        {
            Vector3 aheadPos = transform.position + direction * lookAheadDistance;
            Vector3 origin = new Vector3(aheadPos.x, transform.position.y + heightCheckDistance, aheadPos.z);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, heightCheckDistance * 2f, groundMask))
            {
                groundY = hit.point.y;
                return true;
            }

            groundY = 0f;
            return false;
        }

        private void ApplySeparation()
        {
            Collider[] nearby = Physics.OverlapSphere(transform.position, avoidanceRadius, separationMask);
            foreach (var col in nearby)
            {
                Vector3 away = transform.position - col.transform.position;
                rigidBody.AddForce(away.normalized * avoidanceForce, ForceMode.Force);
            }
        }

        private void FlyToward(ref Vector3 velocity)
        {
            Vector3 direction = Destination - transform.position;
            direction.y = 0;

            float distance = direction.magnitude;
            if (distance <= ArrivalThreshold) return;

            Vector3 dir = direction / distance;

            float currentSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            float brakingDistance = (currentSpeed * currentSpeed) / (2f * deceleration);

            float targetSpeed = distance <= brakingDistance
                ? Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.fixedDeltaTime)
                : Mathf.MoveTowards(currentSpeed, moveSpeed, acceleration * Time.fixedDeltaTime);

            velocity.x = dir.x * targetSpeed;
            velocity.z = dir.z * targetSpeed;

            if (UpdateRotation) FacePosition(transform.position + direction);
        }

        private void ApplyHover(ref Vector3 velocity)
        {
            float targetY = transform.position.y + Mathf.Sin(Time.time * hoverFrequency + _separationFrameOffset) * hoverAmplitude;
            velocity.y = Mathf.MoveTowards(velocity.y, (targetY - transform.position.y) * heightAdjustSpeed, heightAdjustSpeed * Time.fixedDeltaTime);
            velocity.x = Mathf.MoveTowards(velocity.x, 0, moveSpeed * Time.fixedDeltaTime);
            velocity.z = Mathf.MoveTowards(velocity.z, 0, moveSpeed * Time.fixedDeltaTime);
        }

        private void FacePosition(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - rigidBody.position;
            direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) return;
            rigidBody.rotation = Quaternion.Slerp(rigidBody.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.fixedDeltaTime);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, heightCheckDistance, groundMask))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, hit.point);
                Gizmos.DrawWireSphere(hit.point, 0.2f);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, transform.position + Vector3.down * heightCheckDistance);
            }

            if (HasPath)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(new Vector3(Destination.x, transform.position.y, Destination.z), 1f);

                Vector3 dir = Destination - transform.position;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.01f)
                {
                    Vector3 aheadPos = transform.position + dir.normalized * lookAheadDistance;
                    Vector3 origin = new Vector3(aheadPos.x, transform.position.y + heightCheckDistance, aheadPos.z);
                    Gizmos.color = Color.yellow;
                    if (Physics.Raycast(origin, Vector3.down, out RaycastHit aheadHit, heightCheckDistance * 2f, groundMask))
                        Gizmos.DrawLine(origin, aheadHit.point);
                }
            }
        }
#endif
    }
}
