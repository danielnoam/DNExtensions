using System;
using DNExtensions;
using DNExtensions.Button;
using DNExtensions.VFXManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestTransitions : MonoBehaviour
{
    private void Start()
    {
        VFXManager.Instance.PlayRandomVFX();
    }


    [Button(ButtonPlayMode.Both)]
    public void TestButton(string bla = "test")
    {
        Debug.Log(bla);
    }
    
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void TestTransitionQuit()
    {
        TransitionManager.TransitionQuit(VFXManager.Instance.GetRandomEffect());
    }
    
    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void ReloadScene()
    {
        
        TransitionManager.TransitionToScene(SceneManager.GetActiveScene().buildIndex, VFXManager.Instance.GetRandomEffect());
    }
}
