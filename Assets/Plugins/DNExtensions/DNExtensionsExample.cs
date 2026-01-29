
using System;
using System.Collections.Generic;
using DNExtensions.CinemachineExtesnstions;
using DNExtensions.Utilities;
using DNExtensions.Utilities.SerializableSelector;
using DNExtensions.Utilities.Button;
using DNExtensions.Utilities.SerializedInterface;
using DNExtensions.Utilities.CustomFields;
using DNExtensions.Utilities.PrefabSelector;
using DNExtensions.Utilities.RangedValues;
using DNExtensions.Utilities.VFXManager;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class DNExtensionsExample : MonoBehaviour
{

    [Header("DNExtensions")]
    [InfoBox("This is an info box showing something")]
    [Separator("Attributes")]
    [ReadOnly] [SerializeField] private int testReadOnly;
    
    [Separator("Fields")]
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
    
    [Separator("Serializable Selector")]
    [SerializeReference, SerializableSelector(SearchThreshold = -1)] private TestBehavior serializableSelector;
    [SerializeReference, SerializableSelector(SearchThreshold = 0)] private List<TestBehavior> serializableSelectorList;
    
    [Separator("Prefab Selector")]
    [SerializeField, PrefabSelector("Assets/2_Testing")] private GameObject prefabSelector;
    [SerializeField, PrefabSelector("Assets/2_Testing", LockDragDrop = true)] private GameObject prefabSelectorLocked;
    
    [Separator("Cinemachine")]
    [SerializeField] private ImpulseSettings testImpulse;
    [SerializeField] private CinemachineImpulseSource testImpulseSource;
    
    
    private enum TestEnum { Option1, Option2, Option3 }

    

    
    
    [Button("Test Group", ButtonPlayMode.Both)]
    public void TestImpulse()
    {
        testImpulseSource.GenerateImpulse(testImpulse);
    }
    
    
    
    [Button("Transitions", ButtonPlayMode.OnlyWhenPlaying)]
    public void TransitionQuit()
    {
        TransitionManager.TransitionQuit(VFXManager.Instance.GetRandomSequence());
    }
    
    [Button("Transitions",ButtonPlayMode.OnlyWhenPlaying)]
    public void TransitionReloadScene()
    {
        
        TransitionManager.TransitionToScene(SceneManager.GetActiveScene().buildIndex, VFXManager.Instance.GetRandomSequence());
    }
}

internal interface ITest
{
    
}

[Serializable]
public abstract class TestBehavior
{
    public float VarFloat;
}

[Serializable]
public class Behavior1 : TestBehavior
{
    public string varString;
}

[Serializable]
public class Behavior2 : TestBehavior
{
    public bool varBool;
}

[Serializable]
public class Behavior3 : TestBehavior
{
    public bool varBool;
}

[Serializable]
public class Behavior4 : TestBehavior
{
    public bool varBool;
}

[Serializable]
[SerializableSelectorTooltip("A tooltip")]
[SerializableSelectorAllowOnce]
[SerializableSelectorName("Direct Damage", "Damage")]
public class Behavior5 : TestBehavior
{
    public bool varBool;
}