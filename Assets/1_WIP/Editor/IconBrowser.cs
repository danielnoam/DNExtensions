#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class IconBrowser : EditorWindow
{
    private Vector2 scrollPosition;
    private string searchFilter = "";

    [MenuItem("Tools/Icon Browser")]
    static void Init()
    {
        IconBrowser window = (IconBrowser)EditorWindow.GetWindow(typeof(IconBrowser));
        window.Show();
    }

    void OnGUI()
    {
        searchFilter = EditorGUILayout.TextField("Search:", searchFilter);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUIContent[] icons = new GUIContent[]
        {
            EditorGUIUtility.IconContent("Animation.FilterBySelection"),
            EditorGUIUtility.IconContent("SceneViewOrtho"),
            EditorGUIUtility.IconContent("IN LockButton"),
            EditorGUIUtility.IconContent("AssemblyLock"),
            EditorGUIUtility.IconContent("AvatarInspector/RightHandZoom"),
            EditorGUIUtility.IconContent("Linked"),
            EditorGUIUtility.IconContent("Unlinked"),
            EditorGUIUtility.IconContent("UnityEditor.SceneHierarchyWindow"),
            EditorGUIUtility.IconContent("PrefabOverlayAdded Icon"),
            EditorGUIUtility.IconContent("preAudioLoopOff"),
            EditorGUIUtility.IconContent("ScaleTool"),
            EditorGUIUtility.IconContent("SceneviewLighting"),
            EditorGUIUtility.IconContent("RotateTool"),
            EditorGUIUtility.IconContent("MoveTool"),
        };

        // Test some common icon names
        string[] iconNames = new string[]
        {
            "Linked",
            "Unlinked", 
            "IN LockButton",
            "AssemblyLock",
            "SceneViewOrtho",
            "Animation.FilterBySelection",
            "PrefabOverlayAdded Icon",
            "preAudioLoopOff",
            "ScaleTool",
            "SceneviewLighting",
            "RotateTool",
            "MoveTool",
            "UnityEditor.SceneHierarchyWindow",
            "AvatarInspector/RightHandZoom",
            "d_UnityEditor.SceneHierarchyWindow",
            "d_PrefabOverlayAdded Icon",
            "d_preAudioLoopOff",   
            
        };

        foreach (string iconName in iconNames)
        {
            if (string.IsNullOrEmpty(searchFilter) || iconName.ToLower().Contains(searchFilter.ToLower()))
            {
                EditorGUILayout.BeginHorizontal();
                GUIContent icon = EditorGUIUtility.IconContent(iconName);
                if (icon != null && icon.image != null)
                {
                    GUILayout.Label(icon.image, GUILayout.Width(20), GUILayout.Height(20));
                }
                else
                {
                    GUILayout.Label("N/A", GUILayout.Width(20), GUILayout.Height(20));
                }
                EditorGUILayout.LabelField(iconName);
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
    }
}
#endif