using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities.CustomFields
{
    [CustomPropertyDrawer(typeof(SimpleAnimatorClipField))]
    internal class SimpleAnimatorClipFieldDrawer : PropertyDrawer
    {
        private const string NoneOption = "None";
        private const string NoAnimator = "No SimpleAnimator";
        private const string NoClips = "No Clips";
        private static readonly string[] EmptyStates = { NoneOption };
        private static readonly string[] NoAnimatorStates = { NoAnimator };
        private static readonly string[] NoClipStates = { NoClips };

        private const float Spacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var simpleAnimatorProperty = property.FindPropertyRelative("simpleAnimator");
            var clipNameProperty = property.FindPropertyRelative("clipName");
            var clipIndexProperty = property.FindPropertyRelative("clipIndex");
            var assignedProperty = property.FindPropertyRelative("assigned");

            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, label);

            var referenceRect = new Rect(position.x, position.y, position.width * 0.5f, position.height);
            var dropdownRect = new Rect(referenceRect.xMax + Spacing, position.y, position.width * 0.5f - Spacing, position.height);

            EditorGUI.ObjectField(referenceRect, simpleAnimatorProperty, typeof(SimpleAnimator), GUIContent.none);

            var simpleAnimator = simpleAnimatorProperty.objectReferenceValue as SimpleAnimator;

            if (!simpleAnimator)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.Popup(dropdownRect, 0, NoAnimatorStates);
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndProperty();
                return;
            }

            var clipNames = GetClipNames(simpleAnimator);

            if (clipNames.Length <= 1)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.Popup(dropdownRect, 0, NoClipStates);
                EditorGUI.EndDisabledGroup();

                ClearSelection(clipNameProperty, clipIndexProperty, assignedProperty);
                EditorGUI.EndProperty();
                return;
            }

            int currentIndex = GetCurrentIndex(clipNames, clipNameProperty.stringValue);
            int newIndex = EditorGUI.Popup(dropdownRect, currentIndex, clipNames);

            if (newIndex != currentIndex)
            {
                if (newIndex == 0)
                {
                    ClearSelection(clipNameProperty, clipIndexProperty, assignedProperty);
                }
                else
                {
                    clipNameProperty.stringValue = clipNames[newIndex];
                    clipIndexProperty.intValue = newIndex - 1;
                    assignedProperty.boolValue = true;
                }

                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }

        private static string[] GetClipNames(SimpleAnimator simpleAnimator)
        {
            int count = simpleAnimator.ClipCount;
            var names = new string[count + 1];
            names[0] = NoneOption;

            for (int i = 0; i < count; i++)
            {
                var clip = simpleAnimator.GetClip(i);
                names[i + 1] = clip ? clip.name : $"(null) [{i}]";
            }

            return names;
        }

        private static int GetCurrentIndex(string[] clipNames, string currentName)
        {
            if (string.IsNullOrEmpty(currentName)) return 0;

            for (int i = 1; i < clipNames.Length; i++)
            {
                if (clipNames[i] == currentName) return i;
            }

            return 0;
        }

        private static void ClearSelection(
            SerializedProperty clipNameProperty,
            SerializedProperty clipIndexProperty,
            SerializedProperty assignedProperty)
        {
            clipNameProperty.stringValue = string.Empty;
            clipIndexProperty.intValue = -1;
            assignedProperty.boolValue = false;
        }
    }
}