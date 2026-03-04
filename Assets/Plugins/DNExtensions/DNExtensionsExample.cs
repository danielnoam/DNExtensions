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
    /// <summary>
    /// Example MonoBehaviour demonstrating various DNExtensions attributes, fields, and systems.
    /// </summary>
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
        [LinkedVector3] public Vector3 linkedVector3Field;

        [Separator("Fields")]
        public SceneField sceneField;
        public SortingLayerField sortingLayerField;
        public TagField tagField;
        public OptionalField<string> optionalField;
        public AnimatorParameterField animatorParameterField = new AnimatorParameterField(AnimatorParameterType.Bool, AnimatorSource.Component);
        public AnimatorStateField animatorStateField = new AnimatorStateField();
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


        /// <summary>
        /// Generates an impulse using the configured Cinemachine impulse source.
        /// </summary>
        [Button(ButtonPlayMode.Both)]
        [ComponentHeaderButton("Test")]
        public void TestImpulse()
        {
            testImpulseSource.GenerateImpulse(testImpulse);
        }
    }

    /// <summary>
    /// Test interface for demonstrating interface serialization.
    /// </summary>
    public interface ITest
    {

    }

    /// <summary>
    /// Test enum for demonstrating enum serialization in ChanceList.
    /// </summary>
    public enum TestEnum
    {
        /// <summary>
        /// First test option.
        /// </summary>
        Option1,
        /// <summary>
        /// Second test option.
        /// </summary>
        Option2,
        /// <summary>
        /// Third test option.
        /// </summary>
        Option3
    }

    /// <summary>
    /// Base class for testing serializable selector behavior.
    /// </summary>
    [Serializable]
    public abstract class TestBehavior
    {
        /// <summary>
        /// Example float variable.
        /// </summary>
        public float varFloat;
    }

    /// <summary>
    /// Test behavior implementation with string variable.
    /// </summary>
    [MovedFrom("")]
    [Serializable]
    public class Behavior1 : TestBehavior
    {
        /// <summary>
        /// Example string variable.
        /// </summary>
        public string varString;
    }

    /// <summary>
    /// Test behavior implementation with boolean variable.
    /// </summary>
    [MovedFrom("")]
    [Serializable]
    public class Behavior2 : TestBehavior
    {
        /// <summary>
        /// Example boolean variable.
        /// </summary>
        public bool varBool;
    }

    /// <summary>
    /// Test behavior implementation with boolean variable.
    /// </summary>
    [MovedFrom("")]
    [Serializable]
    public class Behavior3 : TestBehavior
    {
        /// <summary>
        /// Example boolean variable.
        /// </summary>
        public bool varBool;
    }

    /// <summary>
    /// Test behavior implementation with boolean variable.
    /// </summary>
    [MovedFrom("")]
    [Serializable]
    public class Behavior4 : TestBehavior
    {
        /// <summary>
        /// Example boolean variable.
        /// </summary>
        public bool varBool;
    }

    /// <summary>
    /// Test behavior implementation with tooltip, allow-once, and custom name attributes.
    /// </summary>
    [MovedFrom("")]
    [Serializable]
    [SerializableSelectorTooltip("A tooltip")]
    [SerializableSelectorAllowOnce]
    [SerializableSelectorName("Direct Damage", "Damage")]
    public class Behavior5 : TestBehavior
    {
        /// <summary>
        /// Example boolean variable.
        /// </summary>
        public bool varBool;
    }



}