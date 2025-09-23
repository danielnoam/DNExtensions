using DNExtensions.Button;
using UnityEngine;
using TMPro;

namespace DNExtensions.InputSystem
{
    

    public class ReplaceTextWithBinding : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textComponent;
        [SerializeField] private bool autoSetOnStart = true;
        [SerializeField] private bool useSpriteMode = true;

        private void Start()
        {
            if (autoSetOnStart)
            {
                UpdateBindingText();
            }
        }

        [Button(ButtonPlayMode.OnlyWhenPlaying)]
        public void UpdateBindingText()
        {
            if (!textComponent) return;

            string processedText = useSpriteMode
                ? InputManager.ReplaceActionBindingsWithSprites(textComponent.text)
                : InputManager.ReplaceActionBindingsWithText(textComponent.text);

            textComponent.text = processedText;
        }
    }
}