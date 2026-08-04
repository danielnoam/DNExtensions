using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

namespace DNExtensions.Systems.ObjectPooling
{
    /// <summary>
    /// Automatically returns Visual Effect to the object pool after a completion.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VisualEffect))]
    [AddComponentMenu("DNExtensions/Object Pooling/Poolable Visual Effect")]
    public class PoolableVisualEffect : MonoBehaviour, IPoolable
    {
        [SerializeField] private VisualEffect effect;
        [SerializeField, Min(0.1f)] private float duration = 2f;

        private Coroutine _returnRoutine;

        private void OnDisable()
        {
            if (_returnRoutine != null) StopCoroutine(_returnRoutine);
        }

        public void Play()
        {
            if (!effect) return;

            if (_returnRoutine != null) StopCoroutine(_returnRoutine);

            effect.Play();
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
            if (!effect) return;
            effect.Stop();
            effect.Reinit();
        }

        public void OnPoolRecycle()
        {
            if (!effect) return;
            effect.Stop();
            effect.Reinit();
        }
    }
}