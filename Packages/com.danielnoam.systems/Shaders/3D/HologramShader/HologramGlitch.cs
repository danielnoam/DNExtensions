using System.Collections;
using DNExtensions.Utilities.Button;
using UnityEngine;

namespace ProjectWallE
{
    public class HologramGlitch : MonoBehaviour
    {
        [System.Serializable]
        private struct GlitchFrame
        {
            public float strength;
            public float scanlineOffset;
            public float duration;
        }
        

        [Header("Settings")]
        [SerializeField] private bool unscaledTime;
        [SerializeField] private GlitchFrame[] sequence = new GlitchFrame[]
        {
            new GlitchFrame { strength = 0.00f, scanlineOffset = 0.0f, duration = 0.25f },
            new GlitchFrame { strength = 0.15f, scanlineOffset = 0.5f, duration = 0.25f },
            new GlitchFrame { strength = 0.00f, scanlineOffset = 0.0f, duration = 0.50f },
            new GlitchFrame { strength = 0.10f, scanlineOffset = 0.5f, duration = 0.10f },
            new GlitchFrame { strength = 0.00f, scanlineOffset = 0.0f, duration = 0.10f },
            new GlitchFrame { strength = 0.05f, scanlineOffset = 0.5f, duration = 0.10f },
            new GlitchFrame { strength = 0.00f, scanlineOffset = 0.0f, duration = 0.40f },
            new GlitchFrame { strength = 0.10f, scanlineOffset = 0.5f, duration = 0.30f },
        };

        private static readonly int GlitchStrength = Shader.PropertyToID("_Glitch_Strength");
        private static readonly int ScanlineOffset = Shader.PropertyToID("_Scanlines_Offset");
        private Renderer[] _renderers;
        private Material[] _materials;
        
        private void Awake()
        {
            CacheRenderers();
        }

        private void OnEnable()
        {
            StartCoroutine(GlitchRoutine());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }
        
        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _materials = new Material[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
                _materials[i] = Application.isPlaying ? _renderers[i].material : _renderers[i].sharedMaterial;
        }

        private void SetGlitch(float strength, float scanline)
        {
            foreach (var mat in _materials)
            {
                mat.SetFloat(GlitchStrength, strength);
                mat.SetFloat(ScanlineOffset, scanline);
            }
        }

        private IEnumerator GlitchRoutine()
        {
            while (gameObject.activeInHierarchy)
            {
                foreach (var frame in sequence)
                {
                    SetGlitch(frame.strength, frame.scanlineOffset);
                    yield return unscaledTime
                        ? new WaitForSecondsRealtime(frame.duration)
                        : new WaitForSeconds(frame.duration);
                }
            }
        }
        
        [Button(ButtonPlayMode.OnlyWhenNotPlaying)]
        private void RandomizeSequence(int frameCount = 8)
        {
            sequence = new GlitchFrame[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                bool isGlitch = Random.value > 0.5f;
                sequence[i] = new GlitchFrame
                {
                    strength      = isGlitch ? Random.Range(0.05f, 0.2f) : 0f,
                    scanlineOffset = isGlitch ? Random.Range(0.2f, 0.8f) : 0f,
                    duration      = isGlitch ? Random.Range(0.1f, 0.3f) : Random.Range(0.2f, 0.6f),
                };
            }
        }
    }
}