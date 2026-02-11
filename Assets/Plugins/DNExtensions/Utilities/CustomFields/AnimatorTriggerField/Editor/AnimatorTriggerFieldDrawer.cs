#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

namespace DNExtensions.Utilities.CustomFields.Editor
{
    [CustomPropertyDrawer(typeof(AnimatorTriggerField))]
    public class AnimatorTriggerFieldDrawer : PropertyDrawer
    {
        private readonly Dictionary<string, string[]> _cachedTriggers = new Dictionary<string, string[]>();
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var controllerProp = property.FindPropertyRelative("controllerNameOrPath");
            var triggerProp = property.FindPropertyRelative("triggerName");

            if (string.IsNullOrEmpty(controllerProp.stringValue))
            {
                EditorGUI.LabelField(position, label.text, "No controller path set");
                EditorGUI.EndProperty();
                return;
            }

            var triggers = GetAnimatorTriggers(controllerProp.stringValue);
            
            if (triggers == null || triggers.Length == 0)
            {
                EditorGUI.LabelField(position, label.text, $"Controller not found: {controllerProp.stringValue}");
                EditorGUI.EndProperty();
                return;
            }

            string displayValue = string.IsNullOrEmpty(triggerProp.stringValue) ? "None" : triggerProp.stringValue;
            int currentIndex = System.Array.IndexOf(triggers, displayValue);
            if (currentIndex == -1) currentIndex = 0;

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, triggers);
            if (EditorGUI.EndChangeCheck())
            {
                triggerProp.stringValue = triggers[newIndex] == "None" ? "" : triggers[newIndex];
            }

            EditorGUI.EndProperty();
        }

        private string[] GetAnimatorTriggers(string nameOrPath)
        {
            if (_cachedTriggers.TryGetValue(nameOrPath, out var cached))
                return cached;

            AnimatorController controller = null;
            
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(nameOrPath);
            
            if (!controller)
            {
                var guids = AssetDatabase.FindAssets($"{nameOrPath} t:AnimatorController");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                }
            }
            
            if (!controller) return null;

            var triggers = new List<string> { "None" };
            
            foreach (var param in controller.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger)
                {
                    triggers.Add(param.name);
                }
            }

            var result = triggers.ToArray();
            _cachedTriggers[nameOrPath] = result;
            return result;
        }
    }
}
#endif