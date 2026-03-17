using System;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Forwards all collision and trigger callbacks as subscribable C# events.
    /// Attach to any GameObject with a Collider to relay physics events to external scripts.
    /// </summary>
    [AddComponentMenu("DNExtensions/Collision Relay")]
    [RequireComponent(typeof(Collider))]
    public class CollisionRelay : MonoBehaviour
    {
        public event Action<Collision> CollisionEntered;
        public event Action<Collision> CollisionStayed;
        public event Action<Collision> CollisionExited;

        public event Action<Collider> TriggerEntered;
        public event Action<Collider> TriggerStayed;
        public event Action<Collider> TriggerExited;

        private void OnCollisionEnter(Collision collision) => CollisionEntered?.Invoke(collision);
        private void OnCollisionStay(Collision collision) => CollisionStayed?.Invoke(collision);
        private void OnCollisionExit(Collision collision) => CollisionExited?.Invoke(collision);

        private void OnTriggerEnter(Collider other) => TriggerEntered?.Invoke(other);
        private void OnTriggerStay(Collider other) => TriggerStayed?.Invoke(other);
        private void OnTriggerExit(Collider other) => TriggerExited?.Invoke(other);
    }
}