using System;
using System.Collections.Generic;
using DNExtensions.CinemachineExtesnstions;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AudioEvent;
using DNExtensions.Utilities.SerializableSelector;
using DNExtensions.Utilities.Button;
using DNExtensions.Utilities.SerializedInterface;
using DNExtensions.Utilities.CustomFields;
using DNExtensions.Utilities.InlineSO;
using DNExtensions.Utilities.PrefabSelector;
using DNExtensions.Utilities.RangedValues;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class DNExtensionsExample : MonoBehaviour
{
    
    [Header("Attributes")]
    [ReadOnly] [SerializeField] private int testReadOnly;
    [SerializeField] private bool conditionalCheck;
    [InfoBox("This is an info box showing something")]
    
    [Separator]
    [Header("Fields")]
    [SerializeField] private SceneField testScene;
    [SerializeField] private SortingLayerField testLayer;
    [SerializeField] private TagField testTag;
    [SerializeField] private OptionalField<string> customString;
    [SerializeField] private AnimatorStateField customAnimatorState;
    [SerializeField] private PositionField positionField;
    
    [Separator]
    [Header("Prefab Selector")]
    [SerializeField, PrefabSelector("Assets/2_Testing")] private GameObject prefabSelector;
    [SerializeField, PrefabSelector("Assets/2_Testing", LockDragDrop = true)] private GameObject prefabSelectorLocked;
    
    [Separator]
    [Header("Ranged Values")]
    [SerializeField] private RangedInt testRangedInt;
    [SerializeField, MinMaxRange(-5f,5)] private RangedFloat testRangedFloat;
    
    [Separator]
    [Header("Chance List")]
    [SerializeField] private ChanceList<string> testStrings;
    [SerializeField] private ChanceList<int> testInts;
    [SerializeField] private ChanceList<TestEnum> testEnums;
    
    [Separator]
    [Header("Inline So")]
    [SerializeField, InlineSO] private SOAudioEvent testAudioEvent;
    
    [Separator]
    [Header("Serialized Interface")]
    [SerializeField] private InterfaceReference<ITest> testInterface;
    [SerializeField, RequireInterface(typeof(ITest))] private MonoBehaviour interactableObject;
    
    [Separator]
    [Header("Serializable Selector")]
    [SerializeReference, SerializableSelector(SearchThreshold = -1)] private TestBehavior serializableSelector;
    [SerializeReference, SerializableSelector(SearchThreshold = 0)] private List<TestBehavior> serializableSelectorList;
    
    
    [Separator]
    [Header("Cinemachine")]
    [SerializeField] private CinemachineImpulseSource testImpulseSource;
    [SerializeField] private ImpulseSettings testImpulse;

    
    
    private enum TestEnum { Option1, Option2, Option3 }

    

    
    
    [Button(ButtonPlayMode.Both)]
    public void TestImpulse()
    {
        testImpulseSource.GenerateImpulse(testImpulse);
    }
}

internal interface ITest
{
    
}

[Serializable]
public abstract class TestBehavior
{
    public float varFloat;
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

namespace MyNamespace
{

}

[Serializable]
[SerializableSelectorTooltip("A tooltip")]
[SerializableSelectorAllowOnce]
[SerializableSelectorName("Direct Damage", "Damage")]
public class Behavior5 : TestBehavior
{
    public bool varBool;
}
