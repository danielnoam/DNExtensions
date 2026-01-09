using TMPro;
using UnityEngine;

public class TitleAnimator : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI screenTitle;

    [Header("Scale")]
    [SerializeField] private Spring scaleSpring = new Spring();
    [SerializeField] private float scaleStartValue = 2f;

    [Header("Position")]
    [SerializeField] private Spring positionSpring = new Spring();
    [SerializeField] private float positionStartValue = 2f;

    
    private Vector3 _baseScale;
    private Vector3 _basePosition;

    private void Awake()
    {
        if (screenTitle)
        {
            _baseScale = screenTitle.rectTransform.localScale;
            _basePosition = screenTitle.rectTransform.anchoredPosition3D;
            
        }
        
        scaleSpring.SetValue(scaleStartValue);
        positionSpring.SetValue(positionStartValue);
    }

    private void Update()
    {
        scaleSpring.Update(Time.deltaTime);
        positionSpring.Update(Time.deltaTime);
        
        if (screenTitle)
        {
            screenTitle.rectTransform.localScale = _baseScale * scaleSpring.Value;
            
            
            Vector3 pos = new Vector3(_basePosition.x, _basePosition.y - positionSpring.Value, _basePosition.z);
            screenTitle.rectTransform.anchoredPosition3D = pos;
        }
    }
}
