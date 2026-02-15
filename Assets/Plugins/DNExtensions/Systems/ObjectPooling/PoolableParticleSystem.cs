
using System.Collections;
using UnityEngine;

namespace DNExtensions.ObjectPooling
{
    

    /// <summary>
    /// Automatically returns particle system  to the object pool after a specified lifetime.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    [AddComponentMenu("DNExtensions/Poolable/Particle System")]
    public class PoolableParticleSystem : MonoBehaviour, IPoolable
    {
        public ParticleSystem particle;

        private void Awake()
        {
            if (!particle) particle = GetComponent<ParticleSystem>();
        }

        public void Play()
        {
            if (!particle) return;

            particle.Play();

            float duration = particle.main.duration + particle.main.startLifetime.constantMax;
            StartCoroutine(ReturnAfter(duration));
        }

        public void Play(Vector3 position)
        {
            if (!particle) return;

            transform.position = position;
            particle.Play();

            float duration = particle.main.duration + particle.main.startLifetime.constantMax;
            StartCoroutine(ReturnAfter(duration));
        }

        private IEnumerator ReturnAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
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