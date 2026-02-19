using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DNExtensions.Utilities.CustomFields
{
    [CustomPropertyDrawer(typeof(AnimatorParameterField))]
    public class AnimatorParameterFieldDrawer : PropertyDrawer
    {
        private const string NoneOption = "None";
        private const float Spacing = 2f;
        private const float ReferenceWidthRatio = 0.5f;

        private static readonly Dictionary<string, string[]> Cache = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var parameterTypeProp = property.FindPropertyRelative("parameterType");
            var sourceProp = property.FindPropertyRelative("source");
            var componentAnimatorProp = property.FindPropertyRelative("componentAnimator");
            var assetControllerProp = property.FindPropertyRelative("assetController");
            var parameterNameProp = property.FindPropertyRelative("parameterName");
            var parameterHashProp = property.FindPropertyRelative("parameterHash");

            EditorGUI.BeginProperty(position, label, property);

            HandleContextMenu(position, property, parameterTypeProp, sourceProp, parameterNameProp, parameterHashProp);

            position = EditorGUI.PrefixLabel(position, label);

            var referenceRect = new Rect(position.x, position.y, position.width * ReferenceWidthRatio, position.height);
            var dropdownRect = new Rect(referenceRect.xMax + Spacing, position.y, position.width - referenceRect.width - Spacing, position.height);

            var source = (AnimatorSource)sourceProp.enumValueIndex;
            AnimatorController controller = null;

            if (source == AnimatorSource.Component)
            {
                if (!componentAnimatorProp.objectReferenceValue)
                    TryAutoDiscover(property, componentAnimatorProp);

                EditorGUI.ObjectField(referenceRect, componentAnimatorProp, typeof(Animator), GUIContent.none);
                controller = GetControllerFromAnimator(componentAnimatorProp.objectReferenceValue as Animator);
            }
            else
            {
                EditorGUI.ObjectField(referenceRect, assetControllerProp, typeof(RuntimeAnimatorController), GUIContent.none);
                controller = GetControllerFromAsset(assetControllerProp.objectReferenceValue as RuntimeAnimatorController);
            }

            if (!controller)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.Popup(dropdownRect, 0, new[] { "No Controller" });
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndProperty();
                return;
            }

            var paramType = (AnimatorParameterType)parameterTypeProp.enumValueIndex;
            var cacheKey = $"{controller.GetInstanceID()}_{paramType}";
            var options = GetParameters(cacheKey, controller, paramType);

            var currentIndex = GetCurrentIndex(options, parameterNameProp.stringValue);
            var newIndex = EditorGUI.Popup(dropdownRect, currentIndex, options);

            if (newIndex != currentIndex)
            {
                var selected = options[newIndex];
                parameterNameProp.stringValue = selected == NoneOption ? string.Empty : selected;
                parameterHashProp.intValue = selected == NoneOption ? 0 : Animator.StringToHash(selected);
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }

        private static void HandleContextMenu(
            Rect rect,
            SerializedProperty property,
            SerializedProperty parameterTypeProp,
            SerializedProperty sourceProp,
            SerializedProperty parameterNameProp,
            SerializedProperty parameterHashProp)
        {
            if (Event.current.type != EventType.ContextClick) return;
            if (!rect.Contains(Event.current.mousePosition)) return;

            var menu = new GenericMenu();

            menu.AddDisabledItem(new GUIContent("Parameter Type"));
            menu.AddSeparator("");
            foreach (AnimatorParameterType type in System.Enum.GetValues(typeof(AnimatorParameterType)))
            {
                var capturedType = type;
                var current = (AnimatorParameterType)parameterTypeProp.enumValueIndex;
                menu.AddItem(new GUIContent(type.ToString()), current == type, () =>
                {
                    parameterTypeProp.enumValueIndex = (int)capturedType;
                    parameterNameProp.stringValue = string.Empty;
                    parameterHashProp.intValue = 0;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.AddSeparator("");
            menu.AddDisabledItem(new GUIContent("Source"));
            menu.AddSeparator("");
            foreach (AnimatorSource src in System.Enum.GetValues(typeof(AnimatorSource)))
            {
                var capturedSrc = src;
                var current = (AnimatorSource)sourceProp.enumValueIndex;
                menu.AddItem(new GUIContent(src.ToString()), current == src, () =>
                {
                    sourceProp.enumValueIndex = (int)capturedSrc;
                    parameterNameProp.stringValue = string.Empty;
                    parameterHashProp.intValue = 0;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Clear Cache"), false, () => Cache.Clear());

            menu.ShowAsContext();
            Event.current.Use();
        }

        private static void TryAutoDiscover(SerializedProperty property, SerializedProperty animatorProp)
        {
            if (property.serializedObject.targetObject is not Component component) return;
            var animator = component.GetComponentInChildren<Animator>(true);
            if (!animator) return;
            animatorProp.objectReferenceValue = animator;
            animatorProp.serializedObject.ApplyModifiedProperties();
        }

        private static AnimatorController GetControllerFromAnimator(Animator animator)
        {
            if (!animator) return null;
            if (animator.runtimeAnimatorController is AnimatorController direct) return direct;
            if (animator.runtimeAnimatorController is AnimatorOverrideController ov)
                return ov.runtimeAnimatorController as AnimatorController;
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

        private static string[] GetParameters(string cacheKey, AnimatorController controller, AnimatorParameterType paramType)
        {
            if (Cache.TryGetValue(cacheKey, out var cached)) return cached;

            var targetType = paramType switch
            {
                AnimatorParameterType.Trigger => AnimatorControllerParameterType.Trigger,
                AnimatorParameterType.Bool    => AnimatorControllerParameterType.Bool,
                AnimatorParameterType.Int     => AnimatorControllerParameterType.Int,
                AnimatorParameterType.Float   => AnimatorControllerParameterType.Float,
                _                             => AnimatorControllerParameterType.Trigger
            };

            var result = controller.parameters
                .Where(p => p.type == targetType)
                .Select(p => p.name)
                .OrderBy(n => n)
                .Prepend(NoneOption)
                .ToArray();

            Cache[cacheKey] = result;
            return result;
        }

        private static int GetCurrentIndex(string[] options, string current)
        {
            if (string.IsNullOrEmpty(current)) return 0;
            for (int i = 0; i < options.Length; i++)
                if (options[i] == current) return i;
            return 0;
        }
    }
}