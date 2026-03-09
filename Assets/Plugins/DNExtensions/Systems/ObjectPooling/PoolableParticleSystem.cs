using System.Collections;
using UnityEngine;

namespace DNExtensions.Systems.ObjectPooling
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
        
        private Coroutine _returnRoutine;

        private void OnDisable()
        {
            if (_returnRoutine != null) StopCoroutine(_returnRoutine);
        }

        public void Play()
        {
            if (!particle) return;
            
            if (_returnRoutine != null) StopCoroutine(_returnRoutine);

            particle.Play();

            float duration = particle.main.duration + particle.main.startLifetime.constantMax;
            _returnRoutine = StartCoroutine(ReturnAfter(duration));
        }

        public void Play(Vector3 position)
        {
            transform.position = position;
            Play();
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