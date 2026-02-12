#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities.Inline.Editor
{
    [CustomPropertyDrawer(typeof(InlineAttribute))]
    public class InlineDrawer : PropertyDrawer
    {
        private static readonly Color BoxColor = new Color(0f, 0f, 0f, 0.15f);
        private static readonly Color BoxBorderColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        private const float BoxPadding = 7f;
        private const float BoxTopMargin = 5f;
        private const float PropertySpacing = 3f;

        private static readonly Dictionary<string, SerializedObject> Cache = new Dictionary<string, SerializedObject>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (property.propertyType != SerializedPropertyType.ObjectReference)
                return height;

            if (!property.isExpanded || property.objectReferenceValue == null)
                return height;

            var so = GetOrCreateSerializedObject(property);
            if (so == null)
                return height;

            height += BoxTopMargin + BoxPadding;
            so.UpdateIfRequiredOrScript();

            var iterator = so.GetIterator();
            iterator.NextVisible(true);

            while (iterator.NextVisible(false))
                height += EditorGUI.GetPropertyHeight(iterator, true) + PropertySpacing;

            height += BoxPadding;
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.LabelField(position, label, new GUIContent("[InlineSO] only works on Object reference fields"));
                EditorGUI.EndProperty();
                return;
            }

            var fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            bool hasReference = property.objectReferenceValue != null;

            if (hasReference)
            {
                property.isExpanded = EditorGUI.Foldout(
                    new Rect(fieldRect.x, fieldRect.y, EditorGUIUtility.labelWidth, fieldRect.height),
                    property.isExpanded,
                    GUIContent.none,
                    true
                );
            }

            EditorGUI.PropertyField(fieldRect, property, label);

            if (!hasReference || !property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            var so = GetOrCreateSerializedObject(property);
            if (so == null)
            {
                EditorGUI.EndProperty();
                return;
            }

            so.UpdateIfRequiredOrScript();

            var attr = (InlineAttribute)attribute;
            float indent = EditorGUI.indentLevel * 15f;

            var boxRect = new Rect(
                position.x + indent,
                fieldRect.yMax + BoxTopMargin,
                position.width - indent,
                position.yMax - fieldRect.yMax - BoxTopMargin
            );

            if (attr.DrawBox)
                DrawBox(boxRect);

            float contentX = boxRect.x + BoxPadding;
            float contentWidth = boxRect.width - BoxPadding * 2f;
            float yOffset = boxRect.y + BoxPadding;

            EditorGUI.indentLevel++;

            var iterator = so.GetIterator();
            iterator.NextVisible(true);

            while (iterator.NextVisible(false))
            {
                float propHeight = EditorGUI.GetPropertyHeight(iterator, true);
                var propRect = new Rect(contentX, yOffset, contentWidth, propHeight);

                EditorGUI.PropertyField(propRect, iterator, true);
                yOffset += propHeight + PropertySpacing;
            }

            EditorGUI.indentLevel--;

            if (so.hasModifiedProperties)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(property.objectReferenceValue);
            }

            EditorGUI.EndProperty();
        }

        private static SerializedObject GetOrCreateSerializedObject(SerializedProperty property)
        {
            var target = property.objectReferenceValue;
            if (target == null)
                return null;

            string key = $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}";

            if (Cache.TryGetValue(key, out var cached))
            {
                try
                {
                    if (cached.targetObject == target)
                        return cached;
                }
                catch (Exception)
                {
                    Cache.Remove(key);
                }
            }

            var so = new SerializedObject(target);
            Cache[key] = so;
            return so;
        }

        private static void DrawBox(Rect rect)
        {
            EditorGUI.DrawRect(rect, BoxColor);
            
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), BoxBorderColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), BoxBorderColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), BoxBorderColor);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), BoxBorderColor);
        }
    }
}
#endif