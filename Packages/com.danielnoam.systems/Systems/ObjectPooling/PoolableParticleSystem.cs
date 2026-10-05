using System.Collections;
using UnityEngine;

namespace DNExtensions.Systems.ObjectPooling
{
    

    /// <summary>
    /// Automatically returns a particle system to the object pool after completion.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    [AddComponentMenu("DNExtensions/Object Pooling/Poolable Particle System")]
    public class PoolableParticleSystem : MonoBehaviour, IPoolable
    {
        public ParticleSystem particle;
        
        private Coroutine _returnRoutine;

        private void Awake()
        {
            if (!particle) particle = GetComponent<ParticleSystem>();
        }

        private void OnDisable()
        {
            if (_returnRoutine != null) StopCoroutine(_returnRoutine);
        }

        public void Play()
        {
            if (!particle) return;
            
            if (_returnRoutine != null) StopCoroutine(_returnRoutine);

            particle.Play();

            _returnRoutine = StartCoroutine(ReturnAfter(GetPlayDuration()));
        }

        public void Play(Vector3 position)
        {
            transform.position = position;
            Play();
        }

        private float GetPlayDuration()
        {
            var main = particle.main;
            float duration = main.duration + GetMax(main.startDelay) + GetMax(main.startLifetime);
            return duration / Mathf.Max(0.01f, main.simulationSpeed);
        }

        // constantMax is only valid in the constant modes, curve modes scale by curveMultiplier instead
        private static float GetMax(ParticleSystem.MinMaxCurve curve)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant: return curve.constant;
                case ParticleSystemCurveMode.TwoConstants: return curve.constantMax;
                default: return curve.curveMultiplier;
            }
        }

        // Follows the system's own time mode, so an unscaled effect is not held while the game is paused
        private IEnumerator ReturnAfter(float delay)
        {
            if (particle.main.useUnscaledTime) yield return new WaitForSecondsRealtime(delay);
            else yield return new WaitForSeconds(delay);

            _returnRoutine = null;
            ObjectPooler.ReturnObjectToPool(gameObject);
        }

        public void OnPoolGet()
        {

        }

        public void OnPoolReturn()
        {
            if (particle)
            {
                particle.Stop(true);
                particle.Clear(true);
            }
        }

        public void OnPoolRecycle()
        {
            if (particle)
            {
                particle.Stop(true);
                particle.Clear(true);
            }
        }
    }
}