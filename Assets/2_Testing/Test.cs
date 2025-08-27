
using DNExtensions;
using DNExtensions.Button;
using DNExtensions.SerializedInterface;
using DNExtensions.VFXManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Test : MonoBehaviour
{
    
    
    
    [Header("Scene Field")]
    [SerializeField] private SceneField testScene;
    
    [Header("Sorting Layer Field")]
    [SerializeField] private SortingLayerField testLayer;
    
    [Header("Ranged Values")]
    [SerializeField] private RangedInt testRangedInt;
    [SerializeField, MinMaxRange(-5f,5)] private RangedFloat testRangedFloat;
    
    [Header("Chance List")]
    [SerializeField] private ChanceList<string> testStrings;
    [SerializeField] private ChanceList<int> testInts;
    [SerializeField] private ChanceList<TestEnum> testEnums;
    
    
    [Header("Serialized Interface")]
    [SerializeField] private InterfaceReference<ITest> testInterface;
    [SerializeField, RequireInterface(typeof(ITest))] private MonoBehaviour interactableObject;

    private enum TestEnum { Option1, Option2, Option3 }


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

internal interface ITest
{
}
