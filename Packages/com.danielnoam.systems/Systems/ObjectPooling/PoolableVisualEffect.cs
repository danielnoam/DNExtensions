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
        [Tooltip("Match the VFX Graph asset's Ignore Time Scale setting. Unity does not expose the asset's update mode at runtime, so it has to be set here.")]
        [SerializeField] private bool ignoreTimeScale;

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

        // Follows the effect's own time mode, so an unscaled effect is not held while the game is paused
        private IEnumerator ReturnAfter(float delay)
        {
            if (ignoreTimeScale) yield return new WaitForSecondsRealtime(delay);
            else yield return new WaitForSeconds(delay);

            _returnRoutine = null;
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