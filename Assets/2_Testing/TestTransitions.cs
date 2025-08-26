
using DNExtensions;
using DNExtensions.Button;
using DNExtensions.VFXManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestTransitions : MonoBehaviour
{
    
    
    
    [Header("Scene Field")]
    [SerializeField] private SceneField testScene;
    
    [Header("Sorting Layer Field")]
    [SerializeField] private SortingLayerField testLayer;
    
    [Header("Chance List")]
    [SerializeField] private ChanceList<string> testStrings;
    [SerializeField] private ChanceList<int> testInts;
    
    

    private void Start()
    {
        VFXManager.Instance.PlayRandomVFX();
    }

    
    
    
    [Button("Test Group", "")]
    public void TestButton1() { }
    
    [Button("Test Group", "")]
    public void TestButton2() { }
    
    
    
    [Button("Transitions", ButtonPlayMode.OnlyWhenPlaying)]
    public void TransitionQuit()
    {
        TransitionManager.TransitionQuit(VFXManager.Instance.GetRandomEffect());
    }
    
    [Button("Transitions",ButtonPlayMode.OnlyWhenPlaying)]
    public void TransitionReloadScene()
    {
        
        TransitionManager.TransitionToScene(SceneManager.GetActiveScene().buildIndex, VFXManager.Instance.GetRandomEffect());
    }
}
