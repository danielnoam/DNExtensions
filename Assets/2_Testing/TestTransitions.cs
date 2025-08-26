using System;
using System.Collections.Generic;
using DNExtensions;
using DNExtensions.Button;
using DNExtensions.VFXManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestTransitions : MonoBehaviour
{
    
    
    
    [Header("Scene Field")]
    [SerializeField] private SceneField testScene;
    
    [Header("Chance List")]
    [SerializeField] private ChanceList<string> testStrings;
    [SerializeField] private ChanceList<int> testInts;
    [SerializeField] private List<GameObject> gmeObjects;
    
    private void Start()
    {
        VFXManager.Instance.PlayRandomVFX();
    }

    
    
    
    [Button("Group1", "")]
    public void TestButton1() { }
    
    [Button("Group1", "")]
    public void TestButton2() { }
    
    
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void TransitionQuit()
    {
        TransitionManager.TransitionQuit(VFXManager.Instance.GetRandomEffect());
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void TransitionReloadScene()
    {
        
        TransitionManager.TransitionToScene(SceneManager.GetActiveScene().buildIndex, VFXManager.Instance.GetRandomEffect());
    }
}
