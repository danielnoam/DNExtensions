#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace DNExtensions.Utilities.CustomFields
{
    /// <summary>
    /// Custom property drawer for PositionField that displays a Vector3 field and Transform field.
    /// When a Transform is assigned, the Vector3 field becomes read-only and syncs with the transform's position.
    /// </summary>
    [CustomPropertyDrawer(typeof(PositionField))]
    public class PositionFieldPropertyDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;
        private const float TransformLabelWidth = 60f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty transformProp = property.FindPropertyRelative("positionTransform");
            SerializedProperty vectorProp = property.FindPropertyRelative("positionVector");

            // Calculate rects for two-line layout
            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect line1 = new Rect(position.x, position.y, position.width, lineHeight);
            Rect line2 = new Rect(position.x, position.y + lineHeight + Spacing, position.width, lineHeight);

            // Line 1: Label and Vector3 field
            Rect labelRect = new Rect(line1.x, line1.y, EditorGUIUtility.labelWidth, line1.height);
            Rect vectorRect = new Rect(line1.x + EditorGUIUtility.labelWidth, line1.y, 
                                       line1.width - EditorGUIUtility.labelWidth, line1.height);

            EditorGUI.LabelField(labelRect, label);

            bool hasTransform = transformProp.objectReferenceValue;

            // Draw Vector3 field (read-only if transform is set)
            EditorGUI.BeginDisabledGroup(hasTransform);
            EditorGUI.BeginChangeCheck();
            Vector3 newVector = EditorGUI.Vector3Field(vectorRect, GUIContent.none, vectorProp.vector3Value);
            if (EditorGUI.EndChangeCheck() && !hasTransform)
            {
                vectorProp.vector3Value = newVector;
            }
            EditorGUI.EndDisabledGroup();

            // Line 2: Transform field with label
            float indent = EditorGUIUtility.labelWidth /2;
            Rect transformFieldRect = new Rect(line2.x + indent + TransformLabelWidth, line2.y, 
                                               line2.width - indent - TransformLabelWidth, line2.height);
            
            
            EditorGUI.BeginChangeCheck();
            Transform newTransform = EditorGUI.ObjectField(transformFieldRect, transformProp.objectReferenceValue, 
                                                          typeof(Transform), true) as Transform;
            if (EditorGUI.EndChangeCheck())
            {
                transformProp.objectReferenceValue = newTransform;
                
                // Sync vector3 when transform is set or cleared
                if (newTransform)
                {
                    vectorProp.vector3Value = newTransform.position;
                }
            }
            

            EditorGUI.EndProperty();

        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2 + Spacing;
        }
    }
}
#endif