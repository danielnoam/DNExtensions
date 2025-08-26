using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

namespace DNExtensions
{
    [System.Serializable]
    public class SortingLayerField
    {
       
        [SerializeField, HideInInspector]
        private string mLayerName = "Default";

        
        [SerializeField]
        private int mLayerID;

        public string LayerName => mLayerName;

        public int LayerID => mLayerID;

        public static implicit operator int(SortingLayerField sortingField)
        {
            return sortingField.mLayerID;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SortingLayerField))]
    public class SortingLayerFieldPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);
            
            SerializedProperty layerName = property.FindPropertyRelative("m_LayerName");
            SerializedProperty layerID = property.FindPropertyRelative("m_LayerID");
            
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Calculate rects for popup and button
            Rect popupRect = new Rect(position.x, position.y, position.width - 30, position.height);
            Rect buttonRect = new Rect(position.x + position.width - 25, position.y, 25, position.height);

            string[] sortingLayerNames = SortingLayer.layers.Select(l => l.name).ToArray();
            int[] layerIDs = SortingLayer.layers.Select(l => l.id).ToArray();
            
            int currentIndex = System.Array.IndexOf(sortingLayerNames, layerName.stringValue);
            if (currentIndex == -1) currentIndex = 0;
            
            int newIndex = EditorGUI.Popup(popupRect, currentIndex, sortingLayerNames);
            if (newIndex != currentIndex)
            {
                layerName.stringValue = sortingLayerNames[newIndex];
                layerID.intValue = layerIDs[newIndex];
            }

            // Add button to open Tags and Layers window
            if (GUI.Button(buttonRect, "..."))
            {
                EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
                SettingsService.OpenProjectSettings("Project/Tags and Layers");
            }
            
            EditorGUI.EndProperty();
        }
    }
#endif
}