using System;
using DNExtensions;
using DNExtensions.Button;
using UnityEngine;


[SelectionBase]
public class NPC : MonoBehaviour
{
    
    [Header("Info")]
    [SerializeField] private new string name = "NPC";
    
    [Header("Dialogue")]
    [SerializeField] private float lineCooldown = 2f;
    [SerializeField] private ChanceList<string> greetingDialogueLines;
    [SerializeField] private ChanceList<string> farewellDialogueLines;
    [SerializeField, ReadOnly] private float lineCooldownTimer;

    
    [Header("References")]
    [SerializeField] private SpeechBubble speechBubble;
    
    private void Update()
    {
        if (lineCooldownTimer > 0f)
        {
            lineCooldownTimer -= Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (greetingDialogueLines.Count <= 0 || lineCooldownTimer > 0) return;
        
        if (other.TryGetComponent(out PlayerController player))
        {
            Greet();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (farewellDialogueLines.Count <= 0 || lineCooldownTimer > 0) return;
        
        if (other.TryGetComponent(out PlayerController player))
        {
            Farewell();
        }
        
    }

    [Button]
    public void Greet()
    {
        var line = greetingDialogueLines.GetRandomItem();
        var lineIndex = greetingDialogueLines.IndexOf(line);
        greetingDialogueLines.SetChance(lineIndex, 0);
        lineCooldownTimer = lineCooldown;
        
        speechBubble?.Show(line);
    }
    
    
    [Button]
    public void Farewell()
    {
        var line = farewellDialogueLines.GetRandomItem();
        var lineIndex = farewellDialogueLines.IndexOf(line);
        farewellDialogueLines.SetChance(lineIndex, 0);
        lineCooldownTimer = lineCooldown;
        
        speechBubble?.Show(line);
    }
}
