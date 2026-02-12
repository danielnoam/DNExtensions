using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DNExtensions.Utilities.CustomFields.Editor
{
    [CustomPropertyDrawer(typeof(AnimatorStateField))]
    public class AnimatorStateFieldDrawer : PropertyDrawer
    {
        private const string NoStateSelected = "None";
        private const string NoAnimator = "No Animator";
        private const string NoController = "No Controller";
        private static readonly string[] EmptyStates = { NoStateSelected };
        private static readonly string[] NoAnimatorStates = { NoAnimator };
        private static readonly string[] NoControllerStates = { NoController };
        
        private const float Spacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var animatorProperty = property.FindPropertyRelative("animator");
            var stateNameProperty = property.FindPropertyRelative("stateName");
            var stateHashProperty = property.FindPropertyRelative("stateHash");
            var assignedProperty = property.FindPropertyRelative("assigned");

            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, label);

            var animator = FindAnimator(property, animatorProperty);
            
            var fieldRect = new Rect(position.x, position.y, 
                position.width * 0.35f, position.height);
            var animatorRect = new Rect(fieldRect.xMax + Spacing, position.y, 
                position.width * 0.65f - Spacing, position.height);

            if (!animator)
            {
                EditorGUI.Popup(fieldRect, 0, NoAnimatorStates);
                EditorGUI.ObjectField(animatorRect, animatorProperty, GUIContent.none);
            }
            else
            {
                var controller = GetAnimatorController(animator);
                if (!controller)
                {
                    EditorGUI.Popup(fieldRect, 0, NoControllerStates);
                    EditorGUI.ObjectField(animatorRect, animatorProperty, GUIContent.none);
                }
                else
                {
                    var states = GetAnimatorStates(controller);
                    var currentIndex = GetCurrentStateIndex(states, stateNameProperty.stringValue);

                    var newIndex = EditorGUI.Popup(fieldRect, currentIndex, states);
                    EditorGUI.ObjectField(animatorRect, animatorProperty, GUIContent.none);

                    if (newIndex != currentIndex)
                    {
                        var selectedState = states[newIndex];
                        stateNameProperty.stringValue = selectedState;
                        stateHashProperty.intValue = Animator.StringToHash(selectedState);
                        assignedProperty.boolValue = newIndex > 0;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        private static Animator FindAnimator(SerializedProperty property, SerializedProperty animatorProperty)
        {
            if (animatorProperty.objectReferenceValue) return animatorProperty.objectReferenceValue as Animator;
            
            var component = property.serializedObject.targetObject as Component;
            var animator = component?.GetComponentInChildren<Animator>(true);
            
            if (animator)
            {
                animatorProperty.objectReferenceValue = animator;
                animatorProperty.serializedObject.ApplyModifiedProperties();
            }
            
            return animator;
        }

        private static string[] GetAnimatorStates(AnimatorController controller)
        {
            var stateNames = controller.layers
                .SelectMany(layer => layer.stateMachine.states)
                .Select(state => state.state.name)
                .Distinct()
                .OrderBy(name => name)
                .ToArray();

            return EmptyStates.Concat(stateNames).ToArray();
        }

        private static AnimatorController GetAnimatorController(Animator animator)
        {
            if (animator.runtimeAnimatorController is AnimatorController controller)
                return controller;

            if (animator.runtimeAnimatorController is AnimatorOverrideController overrideController)
                return overrideController.runtimeAnimatorController as AnimatorController;

            return null;
        }

        private static int GetCurrentStateIndex(string[] states, string currentStateName)
        {
            if (string.IsNullOrEmpty(currentStateName)) return 0;

            for (int i = 0; i < states.Length; i++)
            {
                if (states[i] == currentStateName)
                    return i;
            }

            return 0;
        }
    }
}