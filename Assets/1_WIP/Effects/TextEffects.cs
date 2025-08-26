using System.Collections;
using DNExtensions;
using DNExtensions.Button;
using UnityEngine;
using TMPro;


public class TextEffects : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private float pumpEffectTime = 1f;
    [SerializeField] private float enlargeEffectTime = 0.05f;
    [SerializeField] private float endFontSizeMultiplier = 1.3f;
    [SerializeField] private TextMeshProUGUI text;

    private float _startFontSize;
    private float BigFontSize => _startFontSize * endFontSizeMultiplier; 

    private Coroutine _pumpEffectCoroutine;
    private Coroutine _enlargeEffectCoroutine;


    private void Awake()
    {
        _startFontSize = text.fontSize;

    }

    private IEnumerator EnlargeEffectAndFade() {
        
        while (true)
        {
            text.fontSize += 1.1f;
            text.alpha -= 0.1f;
            yield return new WaitForSeconds(enlargeEffectTime);
        } 
    }


    private IEnumerator PumpEffect() {
        
        while (true)
        {
            if (Mathf.Approximately(text.fontSize, _startFontSize)) {
                
                text.fontSize = BigFontSize;
                
            }
            else if (Mathf.Approximately(text.fontSize, BigFontSize)) {

                text.fontSize = _startFontSize;
            }

            yield return new WaitForSeconds(pumpEffectTime);
        } 
    }

    [Button]
    public void TogglePumpEffect()
    {

        if (_pumpEffectCoroutine != null)
        {
            _pumpEffectCoroutine = null;
            return;
        }
        
        _pumpEffectCoroutine = StartCoroutine(PumpEffect());
    }
    
    [Button]
    public void ToggleEnlargeEffect()
    {

        if (_enlargeEffectCoroutine != null)
        {
            _enlargeEffectCoroutine = null;
            return;
        }

        _enlargeEffectCoroutine = StartCoroutine(EnlargeEffectAndFade());
    }
    
}
