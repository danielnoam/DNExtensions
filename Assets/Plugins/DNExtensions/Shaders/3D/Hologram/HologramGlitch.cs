using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HologramGlitch : MonoBehaviour
{
    
    
    [Header("References")]
    [SerializeField] private Renderer rend;
    
    private static readonly int GlitchStrength = Shader.PropertyToID("_Glitch_Strength");
    private static readonly int ScanlineOffset = Shader.PropertyToID("_Scanlines_Offset");
    private Material _material;

    private void Awake()
    {
        _material = rend.material;

        StartCoroutine(GlitchRoutine());
    }

    private IEnumerator GlitchRoutine()
    {
        while(true)
        {
            _material.SetFloat(GlitchStrength, 0.0f);
            _material.SetFloat(ScanlineOffset, 0.0f);
            yield return new WaitForSeconds(0.25f);

            _material.SetFloat(GlitchStrength, 0.15f);
            _material.SetFloat(ScanlineOffset, 0.5f);
            yield return new WaitForSeconds(0.25f);

            _material.SetFloat(GlitchStrength, 0.0f);
            _material.SetFloat(ScanlineOffset, 0.0f);
            yield return new WaitForSeconds(0.5f);

            _material.SetFloat(GlitchStrength, 0.1f);
            _material.SetFloat(ScanlineOffset, 0.5f);
            yield return new WaitForSeconds(0.1f);

            _material.SetFloat(GlitchStrength, 0.0f);
            _material.SetFloat(ScanlineOffset, 0.0f);
            yield return new WaitForSeconds(0.1f);

            _material.SetFloat(GlitchStrength, 0.05f);
            _material.SetFloat(ScanlineOffset, 0.5f);
            yield return new WaitForSeconds(0.1f);

            _material.SetFloat(GlitchStrength, 0.0f);
            _material.SetFloat(ScanlineOffset, 0.0f);
            yield return new WaitForSeconds(0.4f);

            _material.SetFloat(GlitchStrength, 0.1f);
            _material.SetFloat(ScanlineOffset, 0.5f);
            yield return new WaitForSeconds(0.3f);
        }
    }
}