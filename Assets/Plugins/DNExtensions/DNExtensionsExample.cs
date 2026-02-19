using System;
using System.Collections.Generic;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities;
using DNExtensions.Utilities.SerializableSelector;
using DNExtensions.Utilities.Button;
using DNExtensions.Utilities.CinemachineExtensions;
using DNExtensions.Utilities.SerializedInterface;
using DNExtensions.Utilities.CustomFields;
using DNExtensions.Utilities.Inline;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;


namespace DNExtensions
{
    
    public class DNExtensionsExample : MonoBehaviour
    {

        [Separator("Attributes")] 
        [InfoBox("This is an info box showing something")] 
        [ReadOnly] public int readOnly;
        [Inline] public Component inline; 
        public bool conditionalCheck;
        [DisableIf("conditionalCheck")] public float conditionalFloat;
        [ShowIf("conditionalCheck")] public int conditionalInt;
        [PrefabSelector("Assets/2_Testing")] public GameObject prefabSelector;
        [SOSelector("Assets/2_Testing", LockToFilter = true)] public SOAudioEvent soSelector;

        [Separator("Fields")] 
        public SceneField sceneField;
        public SortingLayerField sortingLayerField;
        public TagField tagField;
        public OptionalField<string> optionalField;
        public AnimatorStateField animatorStateField;
        public AnimatorTriggerField animatorTriggerField = new AnimatorTriggerField("New Animator Controller");
        public PositionField positionField;
        public NoteField noteField = new("Note Field Example");
        public RangedInt rangedInt;
        [MinMaxRange(-5f, 5)] public RangedFloat rangedFloat;

        [Separator("Chance List")]
        public ChanceList<string> testStrings;
        public ChanceList<int> testInts;
        public ChanceList<TestEnum> testEnums;

        [Separator("Serialized Interface")]
        public InterfaceReference<ITest> testInterface;
        [SerializeField, RequireInterface(typeof(ITest))] public MonoBehaviour interactableObject;
        [Separator("Serializable Selector")] [SerializeReference, SerializableSelector(SearchThreshold = -1)] public TestBehavior serializableSelector;
        [SerializeReference, SerializableSelector(SearchThreshold = 0)] public List<TestBehavior> serializableSelectorList;


        [Separator("Cinemachine")]
        public CinemachineImpulseSource testImpulseSource;
        public ImpulseSettings testImpulse;


        [Separator("Better Unity Event")] 
        public UnityEvent testEvent;

        
        [Separator("Scriptable Objects")] 
        [SerializeField, Inline] private SOAudioEvent soAudioEvent;
        public SOFloat soFloat;
        public SOInt soInt;
        public SOBool soBool;
        public SOString soString;
        public SOVector3 soVector3;
        public SOColor soColor;
        public SOVector4 soVector4;
        public SOQuaternion soQuaternion;
        public SOLayerMask soLayerMask;
        public SOAnimationCurve soAnimationCurve;
        public SOEvent soEvent;
        

        
        [Button(ButtonPlayMode.Both)]
        public void TestImpulse()
        {
            testImpulseSource.GenerateImpulse(testImpulse);
        }
    }

    public interface ITest
    {

    }
    
    
    public enum TestEnum
    {
        Option1,
        Option2,
        Option3
    }
    
    [Serializable]
    public abstract class TestBehavior
    {
        public float varFloat;
    }

    [MovedFrom("")]
    [Serializable]
    public class Behavior1 : TestBehavior
    {
        public string varString;
    }

    [MovedFrom("")]
    [Serializable]
    public class Behavior2 : TestBehavior
    {
        public bool varBool;
    }

    [MovedFrom("")]
    [Serializable]
    public class Behavior3 : TestBehavior
    {
        public bool varBool;
    }

    [MovedFrom("")]
    [Serializable]
    public class Behavior4 : TestBehavior
    {
        public bool varBool;
    }

    [MovedFrom("")]
    [Serializable]
    [SerializableSelectorTooltip("A tooltip")]
    [SerializableSelectorAllowOnce]
    [SerializableSelectorName("Direct Damage", "Damage")]
    public class Behavior5 : TestBehavior
    {
        public bool varBool;
    }



}