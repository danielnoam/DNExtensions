using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HologramGlitch : MonoBehaviour
{
    private Material _material;

    private void Awake()
    {
        _material = GetComponent<Renderer>().material;

        StartCoroutine(GlitchRoutine());
    }

    private IEnumerator GlitchRoutine()
    {
        while(true)
        {
            _material.SetFloat("_Glitch_Strength", 0.0f);
            _material.SetFloat("_Scanline_Offset", 0.0f);
            yield return new WaitForSeconds(0.25f);

            _material.SetFloat("_Glitch_Strength", 0.15f);
            _material.SetFloat("_Scanline_Offset", 0.5f);
            yield return new WaitForSeconds(0.25f);

            _material.SetFloat("_Glitch_Strength", 0.0f);
            _material.SetFloat("_Scanline_Offset", 0.0f);
            yield return new WaitForSeconds(0.5f);

            _material.SetFloat("_Glitch_Strength", 0.1f);
            _material.SetFloat("_Scanline_Offset", 0.5f);
            yield return new WaitForSeconds(0.1f);

            _material.SetFloat("_Glitch_Strength", 0.0f);
            _material.SetFloat("_Scanline_Offset", 0.0f);
            yield return new WaitForSeconds(0.1f);

            _material.SetFloat("_Glitch_Strength", 0.05f);
            _material.SetFloat("_Scanline_Offset", 0.5f);
            yield return new WaitForSeconds(0.1f);

            _material.SetFloat("_Glitch_Strength", 0.0f);
            _material.SetFloat("_Scanline_Offset", 0.0f);
            yield return new WaitForSeconds(0.4f);

            _material.SetFloat("_Glitch_Strength", 0.1f);
            _material.SetFloat("_Scanline_Offset", 0.5f);
            yield return new WaitForSeconds(0.3f);
        }
    }
}