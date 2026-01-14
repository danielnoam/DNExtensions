using System;
using DNExtensions;
using DNExtensions.Button;
using UnityEngine;


public class NPC : MonoBehaviour
{
    
    [Header("Info")]
    [SerializeField] private new string name = "NPC";
    
    [Header("Dialogue")]
    [SerializeField] private float lineCooldown = 2f;
    [SerializeField] private ChanceList<string> greetingDialogueLines;
    [SerializeField] private ChanceList<string> farewellDialogueLines;
    [SerializeField, ReadOnly] private float lineCooldownTimer;

    
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
    private void Greet()
    {
        var line = greetingDialogueLines.GetRandomItem();
        var lineIndex = greetingDialogueLines.IndexOf(line);
        greetingDialogueLines.SetChance(lineIndex, 0);
        lineCooldownTimer = lineCooldown;
        
        Debug.Log($"{name}: {line}");
    }
    
    
    [Button]
    private void Farewell()
    {
        var line = farewellDialogueLines.GetRandomItem();
        var lineIndex = farewellDialogueLines.IndexOf(line);
        farewellDialogueLines.SetChance(lineIndex, 0);
        lineCooldownTimer = lineCooldown;
        
        Debug.Log($"{name}: {line}");
    }
}
