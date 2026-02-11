using UnityEngine;
using UnityEditor;

namespace DNExtensions.Utilities.CustomFields
{
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

            bool hasTransform = transformProp.objectReferenceValue;

            Rect transformFieldRect = EditorGUI.PrefixLabel(line1, label);
            EditorGUI.BeginChangeCheck();
            Transform newTransform = EditorGUI.ObjectField(transformFieldRect, GUIContent.none, transformProp.objectReferenceValue, typeof(Transform), true) as Transform;
            if (EditorGUI.EndChangeCheck())
            {
                transformProp.objectReferenceValue = newTransform;
                if (newTransform)
                {
                    vectorProp.vector3Value = newTransform.position;
                }
            }

            Rect vectorFieldRect = new Rect(transformFieldRect.x, line2.y, transformFieldRect.width, lineHeight);
            
            EditorGUI.BeginDisabledGroup(hasTransform);
            EditorGUI.BeginChangeCheck();
            Vector3 newVector = EditorGUI.Vector3Field(vectorFieldRect, GUIContent.none, vectorProp.vector3Value);
            if (EditorGUI.EndChangeCheck() && !hasTransform)
            {
                vectorProp.vector3Value = newVector;
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2 + Spacing;
        }
    }
}