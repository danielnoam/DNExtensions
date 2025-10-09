
using DNExtensions;
using DNExtensions.Button;
using DNExtensions.SerializedInterface;
using DNExtensions.VFXManager;
using DNExtensions.CinemachineImpulseSystem;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class DNExtensionsExample : MonoBehaviour
{

    [Header("DNExtensions")]
    
    [InfoBox("This is an info box showing something")]
    [Separator("Custom Fields")]
    [SerializeField] private SceneField testScene;
    [SerializeField] private SortingLayerField testLayer;
    [SerializeField] private TagField testTag;
    
    
    [Separator("Ranged Values")]
    [SerializeField] private RangedInt testRangedInt;
    [SerializeField, MinMaxRange(-5f,5)] private RangedFloat testRangedFloat;
    
    [Separator("Chance List")]
    [SerializeField] private ChanceList<string> testStrings;
    [SerializeField] private ChanceList<int> testInts;
    [SerializeField] private ChanceList<TestEnum> testEnums;
    
    [Separator("Serialized Interface")]
    [SerializeField] private InterfaceReference<ITest> testInterface;
    [SerializeField, RequireInterface(typeof(ITest))] private MonoBehaviour interactableObject;

    [Separator("Cinemachine")]
    [SerializeField] private ImpulseSettings testImpulse;
    [SerializeField] private CinemachineImpulseSource testImpulseSource;
    
    
    private enum TestEnum { Option1, Option2, Option3 }


    private void Start()
    {
        VFXManager.Instance.PlayVFX(VFXManager.Instance.GetRandomEffect());
    }

    
    
    [Button("Test Group", "")]
    public void TestImpulse()
    {
        testImpulseSource.GenerateImpulse(testImpulse);
    }
    
    
    
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
