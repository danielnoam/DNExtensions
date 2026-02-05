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
        private static readonly string[] EmptyStates = { NoStateSelected };
        
        private const float IconWidth = 20f;
        private const float Spacing = 2f;
        
        private static readonly Color WarningColor = new Color(1f, 0.7f, 0.3f);
        private static readonly Color AssignedColor = new Color(0.3f, 0.8f, 0.4f);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var animatorProperty = property.FindPropertyRelative("animator");
            var stateNameProperty = property.FindPropertyRelative("stateName");
            var stateHashProperty = property.FindPropertyRelative("stateHash");
            var assignedProperty = property.FindPropertyRelative("assigned");

            EditorGUI.BeginProperty(position, GUIContent.none, property);

            var animator = FindAnimator(property, animatorProperty);
            
            var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - IconWidth, position.height);
            var iconRect = new Rect(position.x + EditorGUIUtility.labelWidth - IconWidth, position.y, IconWidth, position.height);

            EditorGUI.LabelField(labelRect, label);

            if (animator == null)
            {
                DrawWarningIcon(iconRect, "No Animator assigned");
                
                var animatorRect = new Rect(position.x + EditorGUIUtility.labelWidth + Spacing, position.y, 
                    position.width - EditorGUIUtility.labelWidth - Spacing, position.height);
                EditorGUI.ObjectField(animatorRect, animatorProperty, GUIContent.none);
            }
            else
            {
                var controller = GetAnimatorController(animator);
                if (controller == null)
                {
                    DrawWarningIcon(iconRect, "Animator has no Controller");
                    
                    var animatorRect = new Rect(position.x + EditorGUIUtility.labelWidth + Spacing, position.y, 
                        position.width - EditorGUIUtility.labelWidth - Spacing, position.height);
                    EditorGUI.ObjectField(animatorRect, animatorProperty, GUIContent.none);
                }
                else
                {
                    var fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth + Spacing, position.y, 
                        (position.width - EditorGUIUtility.labelWidth - Spacing) * 0.35f, position.height);
                    var animatorRect = new Rect(fieldRect.xMax + Spacing, position.y, 
                        (position.width - EditorGUIUtility.labelWidth - Spacing) * 0.65f - Spacing, position.height);
                    
                    var states = GetAnimatorStates(controller);
                    var currentIndex = GetCurrentStateIndex(states, stateNameProperty.stringValue);
                    var isAssigned = currentIndex > 0;

                    DrawStatusIcon(iconRect, isAssigned);

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

        private void DrawStatusIcon(Rect rect, bool isAssigned)
        {
            var icon = isAssigned ? "▶" : "○";
            var tooltip = isAssigned ? "State assigned" : "No state selected";
            var iconColor = isAssigned ? AssignedColor : WarningColor;

            var originalColor = GUI.color;
            GUI.color = iconColor;
            
            var iconContent = new GUIContent(icon, tooltip);
            var iconStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            
            EditorGUI.LabelField(rect, iconContent, iconStyle);
            GUI.color = originalColor;
        }

        private void DrawWarningIcon(Rect rect, string tooltip)
        {
            var originalColor = GUI.color;
            GUI.color = WarningColor;
            
            var iconContent = new GUIContent("⚠", tooltip);
            var iconStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            
            EditorGUI.LabelField(rect, iconContent, iconStyle);
            GUI.color = originalColor;
        }

        private Animator FindAnimator(SerializedProperty property, SerializedProperty animatorProperty)
        {
            if (animatorProperty.objectReferenceValue != null)
                return animatorProperty.objectReferenceValue as Animator;
            
            var component = property.serializedObject.targetObject as Component;
            var animator = component?.GetComponentInChildren<Animator>(true);
            
            if (animator != null)
            {
                animatorProperty.objectReferenceValue = animator;
                animatorProperty.serializedObject.ApplyModifiedProperties();
            }
            
            return animator;
        }

        private string[] GetAnimatorStates(AnimatorController controller)
        {
            var stateNames = controller.layers
                .SelectMany(layer => layer.stateMachine.states)
                .Select(state => state.state.name)
                .Distinct()
                .OrderBy(name => name)
                .ToArray();

            return EmptyStates.Concat(stateNames).ToArray();
        }

        private AnimatorController GetAnimatorController(Animator animator)
        {
            if (animator.runtimeAnimatorController is AnimatorController controller)
                return controller;

            if (animator.runtimeAnimatorController is AnimatorOverrideController overrideController)
                return overrideController.runtimeAnimatorController as AnimatorController;

            return null;
        }

        private int GetCurrentStateIndex(string[] states, string currentStateName)
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
