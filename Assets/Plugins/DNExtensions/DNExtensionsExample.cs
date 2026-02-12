using System;
using System.Collections.Generic;
using DNExtensions.CinemachineExtesnstions;
using DNExtensions.Utilities;
using DNExtensions.Utilities.SerializableSelector;
using DNExtensions.Utilities.Button;
using DNExtensions.Utilities.SerializedInterface;
using DNExtensions.Utilities.CustomFields;
using DNExtensions.Utilities.Inline;
using DNExtensions.Utilities.PrefabSelector;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class DNExtensionsExample : MonoBehaviour
{
    
    [Separator("Attributes")]
    [InfoBox("This is an info box showing something")]
    [SerializeField, ReadOnly] private int readOnly;
    [SerializeField, Inline] private Component inline;
    [SerializeField, Foldout("Conditional")] private bool conditionalCheck;
    [SerializeField, DisableIf("conditionalCheck"), Foldout("Conditional")] private float conditionalFloat;
    [SerializeField, ShowIf("conditionalCheck"), Foldout("Conditional")] private int conditionalInt;
    [SerializeField, Foldout("Prefab Selector"), PrefabSelector("Assets/2_Testing")] private GameObject prefabSelector;
    [SerializeField, Foldout("Prefab Selector"), PrefabSelector("Assets/2_Testing", LockToFilter = true)] private GameObject prefabSelectorLocked;
    
    [Separator("Fields")]
    [SerializeField] private SceneField sceneField;
    [SerializeField] private SortingLayerField sortingLayerField;
    [SerializeField] private TagField tagField;
    [SerializeField] private OptionalField<string> optionalField;
    [SerializeField] private AnimatorStateField animatorStateField;
    [SerializeField] private AnimatorTriggerField animatorTriggerField = new AnimatorTriggerField("New Animator Controller");
    [SerializeField] private PositionField positionField;
    [SerializeField] private NoteField noteField = new("Note Field Example");
    [SerializeField] private RangedInt rangedInt;
    [SerializeField, MinMaxRange(-5f,5)] private RangedFloat rangedFloat;
    
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
    
    
    [Separator("Cinemachine")]
    [SerializeField] private CinemachineImpulseSource testImpulseSource;
    [SerializeField] private ImpulseSettings testImpulse;

    
    [Separator("Better Unity Event")]
    [SerializeField] private UnityEvent testEvent;
    
    
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

[Serializable]
[SerializableSelectorTooltip("A tooltip")]
[SerializableSelectorAllowOnce]
[SerializableSelectorName("Direct Damage", "Damage")]
public class Behavior5 : TestBehavior
{
    public bool varBool;
}


