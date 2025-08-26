
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
#endif

namespace DNExtensions
{
    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SceneField))]
    public class SceneFieldPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);
            SerializedProperty sceneAsset = property.FindPropertyRelative("sceneAsset");
            SerializedProperty sceneName = property.FindPropertyRelative("sceneName");
            SerializedProperty scenePath = property.FindPropertyRelative("scenePath");
            
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            if (sceneAsset != null)
            {
                EditorGUI.BeginChangeCheck();
                sceneAsset.objectReferenceValue = EditorGUI.ObjectField(position, sceneAsset.objectReferenceValue, typeof(SceneAsset), false);
                
                if (EditorGUI.EndChangeCheck())
                {
                    if (sceneAsset.objectReferenceValue)
                    {
                        SceneAsset scene = sceneAsset.objectReferenceValue as SceneAsset;
                        if (scene)
                        {
                            sceneName.stringValue = scene.name;
                            scenePath.stringValue = AssetDatabase.GetAssetPath(scene);
                        }

                        // Validate if the scene is in build settings
                        bool sceneInBuild = false;
                        bool sceneEnabled = false;
                        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
                        
                        foreach (var buildScene in scenes)
                        {
                            if (buildScene.path == scenePath.stringValue)
                            {
                                sceneInBuild = true;
                                sceneEnabled = buildScene.enabled;
                                break;
                            }
                        }
                        
                        if (!sceneInBuild)
                        {
                            Debug.LogWarning($"Scene '{sceneName.stringValue}' is not in build settings! Please add it to your build settings.");
                        }
                        else if (!sceneEnabled)
                        {
                            Debug.LogWarning($"Scene '{sceneName.stringValue}' is in build settings but disabled! Please enable it in build settings.");
                        }
                    }
                    else
                    {
                        sceneName.stringValue = "";
                        scenePath.stringValue = "";
                    }
                }
            }
            EditorGUI.EndProperty();
        }
    }
    #endif
}