using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.AudioLibrary
{
    [CustomPropertyDrawer(typeof(AudioMapping))]
    public class AudioMappingDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            Rect idRect = new Rect(position.x, position.y, position.width * 0.4f, position.height);
            Rect objRect = new Rect(position.x + position.width * 0.45f, position.y, position.width * 0.55f, position.height);

            SerializedProperty idProp = property.FindPropertyRelative("id");
            SerializedProperty objProp = property.FindPropertyRelative("audioObject");
            
            EditorGUI.PropertyField(idRect, idProp, GUIContent.none);

  
            EditorGUI.BeginChangeCheck();
            Object newObj = EditorGUI.ObjectField(objRect, GUIContent.none, objProp.objectReferenceValue, typeof(Object), false);
            
            if (EditorGUI.EndChangeCheck())
            {
                if (!newObj || newObj is AudioClip || newObj is SOAudioProfile)
                {
                    objProp.objectReferenceValue = newObj;
                }
                else
                {
                    Debug.LogWarning("AudioMapping: Only AudioClips or SOAudioProfiles allowed!");
                }
            }

            EditorGUI.EndProperty();
        }
    }
}