using System;
using System.Collections;
using UnityEngine;

namespace DNExtensions.Systems.ObjectPooling
{
    /// <summary>
    /// Automatically returns audio sources to the object pool after a completion.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("DNExtensions/Object Pooling/Poolable Audio Source")]
    public class PoolableAudioSource : MonoBehaviour, IPoolable
    {
        public AudioSource audioSource;

        private void Awake()
        {
            if (!audioSource) audioSource = GetComponent<AudioSource>();
        }

        private Coroutine _returnRoutine;

        private void OnDisable()
        {
            if (_returnRoutine != null) StopCoroutine(_returnRoutine);
        }

        public void Play(AudioClip clip)
        {
            if (!audioSource || !clip) return;

            audioSource.clip = clip;
            audioSource.Play();

            if (_returnRoutine != null) StopCoroutine(_returnRoutine);
            _returnRoutine = StartCoroutine(ReturnAfter(clip.length / Mathf.Max(0.01f, Mathf.Abs(audioSource.pitch))));
        }

        public void Play(AudioClip clip, Vector3 position)
        {
            transform.position = position;
            Play(clip);
        }

        // Audio plays in real time whatever the time scale, so the wait must too, or a paused game holds the source forever
        private IEnumerator ReturnAfter(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _returnRoutine = null;
            ObjectPooler.ReturnObjectToPool(gameObject);
        }

        public void OnPoolGet()
        {
            
        }

        public void OnPoolReturn()
        {
            if (audioSource) audioSource.Stop();
        }

        public void OnPoolRecycle()
        {
            if (audioSource) audioSource.Stop();
        }
    }
}