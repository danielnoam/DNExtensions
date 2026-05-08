using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.ObjectPooling
{
    [CustomPropertyDrawer(typeof(Pool))]
    internal sealed class PoolDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label.text = $"Pool - {GetPoolLabel(property)}";

            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (!property.isExpanded) return;

            EditorGUI.indentLevel++;

            var child = property.Copy();
            var end = property.GetEndProperty();
            child.NextVisible(true);

            float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            do
            {
                if (SerializedProperty.EqualContents(child, end)) break;

                float h = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), child, true);
                y += h + EditorGUIUtility.standardVerticalSpacing;
            }
            while (child.NextVisible(false));

            EditorGUI.indentLevel--;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded) return height;

            var child = property.Copy();
            var end = property.GetEndProperty();
            child.NextVisible(true);

            do
            {
                if (SerializedProperty.EqualContents(child, end)) break;
                height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
            }
            while (child.NextVisible(false));

            return height;
        }

        private static string GetPoolLabel(SerializedProperty property)
        {
            var overrideProp = property.FindPropertyRelative("overridePoolName");
            if (overrideProp != null)
            {
                var enabledProp = overrideProp.FindPropertyRelative("isSet");
                var valueProp = overrideProp.FindPropertyRelative("value");

                if (enabledProp != null && valueProp != null
                    && enabledProp.boolValue
                    && !string.IsNullOrEmpty(valueProp.stringValue))
                {
                    return valueProp.stringValue;
                }
            }

            var prefabProp = property.FindPropertyRelative("prefab");
            if (prefabProp?.objectReferenceValue)
            {
                return prefabProp.objectReferenceValue.name;
            }

            return "New Pool";
        }
    }
}