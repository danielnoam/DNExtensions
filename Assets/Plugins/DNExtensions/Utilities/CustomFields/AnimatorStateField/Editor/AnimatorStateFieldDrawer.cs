using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DNExtensions.Utilities.CustomFields.Editor
{
    [CustomPropertyDrawer(typeof(AnimatorStateField))]
    public class AnimatorStateFieldDrawer : PropertyDrawer
    {
        private const string NoneOption = "None";
        private const string NoAnimator = "No Animator";
        private const string NoController = "No Controller";
        private static readonly string[] EmptyStates = { NoneOption };
        private static readonly string[] NoAnimatorStates = { NoAnimator };
        private static readonly string[] NoControllerStates = { NoController };

        private const float Spacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var animatorProperty = property.FindPropertyRelative("animator");
            var assetControllerProperty = property.FindPropertyRelative("assetController");
            var sourceProperty = property.FindPropertyRelative("source");
            var stateNameProperty = property.FindPropertyRelative("stateName");
            var stateHashProperty = property.FindPropertyRelative("stateHash");
            var assignedProperty = property.FindPropertyRelative("assigned");

            EditorGUI.BeginProperty(position, label, property);

            HandleContextMenu(position, property, sourceProperty, stateNameProperty, stateHashProperty, assignedProperty);

            position = EditorGUI.PrefixLabel(position, label);

            var referenceRect = new Rect(position.x, position.y, position.width * 0.5f, position.height);
            var dropdownRect = new Rect(referenceRect.xMax + Spacing, position.y, position.width * 0.5f - Spacing, position.height);

            var source = (AnimatorSource)sourceProperty.enumValueIndex;
            AnimatorController controller = null;

            if (source == AnimatorSource.Component)
            {
                EditorGUI.ObjectField(referenceRect, animatorProperty, typeof(Animator), GUIContent.none);
                controller = GetAnimatorController(animatorProperty.objectReferenceValue as Animator);
            }
            else
            {
                EditorGUI.ObjectField(referenceRect, assetControllerProperty, typeof(RuntimeAnimatorController), GUIContent.none);
                controller = GetControllerFromAsset(assetControllerProperty.objectReferenceValue as RuntimeAnimatorController);
            }

            if (!controller)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.Popup(dropdownRect, 0, source == AnimatorSource.Component ? NoAnimatorStates : NoControllerStates);
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndProperty();
                return;
            }

            var states = GetAnimatorStates(controller);
            var currentIndex = GetCurrentStateIndex(states, stateNameProperty.stringValue);
            var newIndex = EditorGUI.Popup(dropdownRect, currentIndex, states);

            if (newIndex != currentIndex)
            {
                var selectedState = states[newIndex];
                stateNameProperty.stringValue = selectedState == NoneOption ? string.Empty : selectedState;
                stateHashProperty.intValue = selectedState == NoneOption ? 0 : Animator.StringToHash(selectedState);
                assignedProperty.boolValue = newIndex > 0;
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }

        private static void HandleContextMenu(
            Rect rect,
            SerializedProperty property,
            SerializedProperty sourceProperty,
            SerializedProperty stateNameProperty,
            SerializedProperty stateHashProperty,
            SerializedProperty assignedProperty)
        {
            if (Event.current.type != EventType.ContextClick) return;
            if (!rect.Contains(Event.current.mousePosition)) return;

            var menu = new GenericMenu();

            menu.AddDisabledItem(new GUIContent("Source"));
            menu.AddSeparator("");
            foreach (AnimatorSource src in System.Enum.GetValues(typeof(AnimatorSource)))
            {
                var capturedSrc = src;
                var current = (AnimatorSource)sourceProperty.enumValueIndex;
                menu.AddItem(new GUIContent(src.ToString()), current == src, () =>
                {
                    sourceProperty.enumValueIndex = (int)capturedSrc;
                    stateNameProperty.stringValue = string.Empty;
                    stateHashProperty.intValue = 0;
                    assignedProperty.boolValue = false;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Clear Selection"), false, () =>
            {
                stateNameProperty.stringValue = string.Empty;
                stateHashProperty.intValue = 0;
                assignedProperty.boolValue = false;
                property.serializedObject.ApplyModifiedProperties();
            });

            menu.ShowAsContext();
            Event.current.Use();
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
            if (!animator) return null;
            if (animator.runtimeAnimatorController is AnimatorController controller)
                return controller;

            if (animator.runtimeAnimatorController is AnimatorOverrideController overrideController)
                return overrideController.runtimeAnimatorController as AnimatorController;

            return null;
        }

        private static AnimatorController GetControllerFromAsset(RuntimeAnimatorController asset)
        {
            if (!asset) return null;
            if (asset is AnimatorController direct) return direct;
            if (asset is AnimatorOverrideController ov)
                return ov.runtimeAnimatorController as AnimatorController;
            return null;
        }

        private static int GetCurrentStateIndex(string[] states, string currentStateName)
        {
            if (string.IsNullOrEmpty(currentStateName)) return 0;

            for (int i = 0; i < states.Length; i++)
                if (states[i] == currentStateName) return i;

            return 0;
        }
    }
}