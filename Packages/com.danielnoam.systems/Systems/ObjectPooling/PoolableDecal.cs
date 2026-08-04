using System.Collections;
using DNExtensions.Utilities;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DNExtensions.Systems.ObjectPooling
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DecalProjector))]
    [AddComponentMenu("DNExtensions/Object Pooling/Poolable Decal")]
    public class PoolableDecal : MonoBehaviour, IPoolable
    {
        [SerializeField] private DecalProjector decalProjector;
        [SerializeField, Min(0.1f)] private float duration = 5f;
        [SerializeField] private bool fadeOut = true;
        [SerializeField, ShowIf("fadeOut")] private AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        private Coroutine _returnRoutine;

        private void OnDisable()
        {
            if (_returnRoutine != null) StopCoroutine(_returnRoutine);
        }

        public void Show(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            Show();
        }

        public void Show()
        {
            if (!decalProjector) return;

            if (_returnRoutine != null) StopCoroutine(_returnRoutine);

            decalProjector.fadeFactor = 1f;
            decalProjector.enabled = true;
            _returnRoutine = StartCoroutine(fadeOut ? FadeAndReturn(duration) : ReturnAfter(duration));
        }

        private IEnumerator ReturnAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            ObjectPooler.ReturnObjectToPool(gameObject);
        }

        private IEnumerator FadeAndReturn(float delay)
        {
            float elapsed = 0f;
            while (elapsed < delay)
            {
                elapsed += Time.deltaTime;
                decalProjector.fadeFactor = fadeCurve.Evaluate(elapsed / delay);
                yield return null;
            }

            ObjectPooler.ReturnObjectToPool(gameObject);
        }

        public void OnPoolGet() { }

        public void OnPoolReturn()
        {
            if (!decalProjector) return;
            decalProjector.enabled = false;
        }

        public void OnPoolRecycle()
        {
            if (!decalProjector) return;
            decalProjector.enabled = false;
        }
    }
}