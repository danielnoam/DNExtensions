using System;
using System.Collections;
using UnityEngine;

namespace DNExtensions.Systems.ObjectPooling
{
    
    /// <summary>
    /// Automatically returns audio sources to the object pool after a specified lifetime.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("DNExtensions/Poolable/Audio Source")]
    public class PoolableAudioSource : MonoBehaviour, IPoolable
    {
        public AudioSource audioSource;


        private void Awake()
        {
            if (!audioSource) audioSource = GetComponent<AudioSource>();
        }

        public void Play(AudioClip clip)
        {
            if (!audioSource) return;

            audioSource.clip = clip;
            audioSource.Play();

            float duration = audioSource.clip.length;
            StartCoroutine(ReturnAfter(duration));
        }

        public void Play(AudioClip clip, Vector3 position)
        {
            if (!audioSource) return;

            audioSource.clip = clip;
            transform.position = position;
            audioSource.Play();

            float duration = audioSource.clip.length;
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
            if (audioSource) audioSource.Stop();
        }

        public void OnPoolRecycle()
        {
            if (audioSource) audioSource.Stop();
        }
    }
}