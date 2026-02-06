
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

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty transformProp = property.FindPropertyRelative("positionTransform");
            SerializedProperty vectorProp = property.FindPropertyRelative("positionVector");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect line1 = new Rect(position.x, position.y, position.width, lineHeight);
            Rect line2 = new Rect(position.x, position.y + lineHeight + Spacing, position.width, lineHeight);

            // Line 1: Vector3 field with main label
            Rect vectorFieldRect = EditorGUI.PrefixLabel(line1, label);
            
            bool hasTransform = transformProp.objectReferenceValue;

            EditorGUI.BeginDisabledGroup(hasTransform);
            EditorGUI.BeginChangeCheck();
            Vector3 newVector = EditorGUI.Vector3Field(vectorFieldRect, GUIContent.none, vectorProp.vector3Value);
            if (EditorGUI.EndChangeCheck() && !hasTransform)
            {
                vectorProp.vector3Value = newVector;
            }
            EditorGUI.EndDisabledGroup();

            // Line 2: Transform field with indented label
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;
            
            EditorGUI.BeginChangeCheck();
            Transform newTransform = EditorGUI.ObjectField(line2, "Transform", transformProp.objectReferenceValue, 
                typeof(Transform), true) as Transform;
            if (EditorGUI.EndChangeCheck())
            {
                transformProp.objectReferenceValue = newTransform;
                if (newTransform)
                {
                    vectorProp.vector3Value = newTransform.position;
                }
            }
            
            EditorGUI.indentLevel = oldIndent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2 + Spacing;
        }
    }
}
