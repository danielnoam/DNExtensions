#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace DNExtensions.Utilities.CustomFields
{
    [CustomPropertyDrawer(typeof(NoteField))]
    public class NoteFieldDrawer : PropertyDrawer
    {
        private const float LineHeight = 18f;
        private const float Padding = 4f;
        private const double DoubleClickTime = 0.3;
        
        private static string sEditingPropertyPath;
        private static Object sEditingTarget;
        private static double sLastClickTime;
        private static string sLastClickedPropertyPath;
        private static Object sLastClickedTarget;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var noteProp = property.FindPropertyRelative("note");
            if (noteProp == null)
                return EditorGUIUtility.singleLineHeight;

            string text = noteProp.stringValue ?? "";
            int lineCount = Mathf.Max(1, text.Split('\n').Length);

            return lineCount * LineHeight + Padding * 2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var noteProp = property.FindPropertyRelative("note");
            var isEditableProp = property.FindPropertyRelative("isEditable");

            if (noteProp == null || isEditableProp == null)
            {
                EditorGUI.LabelField(position, label.text, "Missing properties");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Object target = property.serializedObject.targetObject;
            bool isEditing = sEditingPropertyPath == property.propertyPath && sEditingTarget == target;
            bool isEditable = isEditableProp.boolValue;

            Event evt = Event.current;

            EditorGUI.DrawRect(position, new Color(0, 0, 0, 0.1f));
            
            Handles.BeginGUI();
            Handles.color = new Color(0, 0, 0, 0.3f);
            Handles.DrawPolyLine(
                new Vector3(position.x, position.y),
                new Vector3(position.xMax, position.y),
                new Vector3(position.xMax, position.yMax),
                new Vector3(position.x, position.yMax),
                new Vector3(position.x, position.y)
            );
            Handles.EndGUI();

            Rect contentRect = new Rect(
                position.x + Padding,
                position.y + Padding,
                position.width - Padding * 2,
                position.height - Padding * 2
            );

            if (isEditing)
            {
                string controlName = "NoteFieldEditor_" + property.propertyPath;
                GUI.SetNextControlName(controlName);
                
                EditorGUI.BeginChangeCheck();
                
                GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
                textAreaStyle.padding = new RectOffset(2, 2, 2, 2);
                string newText = EditorGUI.TextArea(contentRect, noteProp.stringValue, textAreaStyle);
                
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Edit Note");
                    noteProp.stringValue = newText;
                    property.serializedObject.ApplyModifiedProperties();
                }

                if (GUI.GetNameOfFocusedControl() != controlName)
                {
                    EditorGUI.FocusTextInControl(controlName);
                }

                if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
                {
                    sEditingPropertyPath = null;
                    sEditingTarget = null;
                    GUI.FocusControl(null);
                    evt.Use();
                }

                if (evt.type == EventType.MouseDown && !position.Contains(evt.mousePosition))
                {
                    sEditingPropertyPath = null;
                    sEditingTarget = null;
                    GUI.FocusControl(null);
                }
            }
            else
            {
                GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
                labelStyle.wordWrap = true;
                labelStyle.padding = new RectOffset(2, 2, 2, 2);
                
                if (isEditable && position.Contains(evt.mousePosition))
                {
                    labelStyle.normal.textColor = new Color(0.3f, 0.6f, 1f);
                }
                
                EditorGUI.LabelField(contentRect, noteProp.stringValue ?? "", labelStyle);

                if (isEditable && evt.type == EventType.MouseDown && position.Contains(evt.mousePosition))
                {
                    double currentTime = EditorApplication.timeSinceStartup;
                    bool isSameProperty = sLastClickedPropertyPath == property.propertyPath && sLastClickedTarget == target;
                    
                    if (isSameProperty && (currentTime - sLastClickTime) < DoubleClickTime)
                    {
                        sEditingPropertyPath = property.propertyPath;
                        sEditingTarget = target;
                        evt.Use();
                        GUIUtility.ExitGUI();
                    }
                    
                    sLastClickedPropertyPath = property.propertyPath;
                    sLastClickedTarget = target;
                    sLastClickTime = currentTime;
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif