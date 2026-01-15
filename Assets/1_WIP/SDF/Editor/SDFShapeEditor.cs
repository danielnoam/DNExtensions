#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CustomEditor(typeof(SDFShapeBase), true)]
[CanEditMultipleObjects]
public class SDFShapeBaseEditor : GraphicEditor
{
    private SerializedProperty m_BaseColor;
    private SerializedProperty m_Rotation;
    private SerializedProperty m_OutlineThickness;
    private SerializedProperty m_OutlineColor;
    private SerializedProperty m_InlineThickness;
    private SerializedProperty m_InlineColor;
    
    // Base Graphic properties
    private new SerializedProperty m_Color;
    private new SerializedProperty m_RaycastTarget;
    private new SerializedProperty m_RaycastPadding;
    private new SerializedProperty m_Maskable;
    
    // Export settings
    private int exportWidth = 512;
    private int exportHeight = 512;
    
    private bool m_ShowOutline;
    private bool m_ShowInline;
    private bool m_ShowExport;

    protected override void OnEnable()
    {
        base.OnEnable();
        
        // Common properties
        m_BaseColor = serializedObject.FindProperty("m_BaseColor");
        m_Rotation = serializedObject.FindProperty("m_Rotation");
        m_OutlineThickness = serializedObject.FindProperty("m_OutlineThickness");
        m_OutlineColor = serializedObject.FindProperty("m_OutlineColor");
        m_InlineThickness = serializedObject.FindProperty("m_InlineThickness");
        m_InlineColor = serializedObject.FindProperty("m_InlineColor");
        
        // Base Graphic properties
        m_Color = serializedObject.FindProperty("m_Color");
        m_RaycastTarget = serializedObject.FindProperty("m_RaycastTarget");
        m_RaycastPadding = serializedObject.FindProperty("m_RaycastPadding");
        m_Maskable = serializedObject.FindProperty("m_Maskable");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space();
        
        // Base Graphic properties
        EditorGUILayout.LabelField("Graphic", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_Color);
        EditorGUILayout.PropertyField(m_RaycastTarget);
        
        if (m_RaycastTarget.boolValue)
        {
            EditorGUILayout.PropertyField(m_RaycastPadding);
        }
        
        EditorGUILayout.PropertyField(m_Maskable);
        
        EditorGUILayout.Space();
        
        // Shape properties 
        EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_BaseColor);
        EditorGUILayout.PropertyField(m_Rotation);
        DrawShapeSpecificProperties();
        
        EditorGUILayout.Space();
        
        m_ShowOutline = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowOutline, "Outline");
        if (m_ShowOutline)
        {
            EditorGUILayout.PropertyField(m_OutlineThickness);
            EditorGUILayout.PropertyField(m_OutlineColor);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        m_ShowInline = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowInline, "Inline");
        if (m_ShowInline)
        {
            EditorGUILayout.PropertyField(m_InlineThickness);
            EditorGUILayout.PropertyField(m_InlineColor);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();


        
        // Export section
        m_ShowExport = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowExport, "Export");
        if (m_ShowExport)
        {
            exportWidth = EditorGUILayout.IntField("Width", exportWidth);
            exportHeight = EditorGUILayout.IntField("Height", exportHeight);
        
            if (GUILayout.Button("Export to PNG"))
            {
                SDFShapeBase shape = (SDFShapeBase)target;
                string path = EditorUtility.SaveFilePanel(
                    "Save Shape as PNG",
                    "Assets",
                    "SDFShape.png",
                    "png"
                );
        
                if (!string.IsNullOrEmpty(path))
                {
                    shape.ExportToPNG(exportWidth, exportHeight, path);
                    AssetDatabase.Refresh();
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        

        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawShapeSpecificProperties()
    {
        // This automatically draws all serialized fields that aren't in the base class
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            
            // Skip base class properties and Unity internals
            if (prop.name == "m_Script" || 
                prop.name == "m_BaseColor" || 
                prop.name == "m_Rotation" ||
                prop.name == "m_OutlineThickness" ||
                prop.name == "m_OutlineColor" ||
                prop.name == "m_InlineThickness" ||
                prop.name == "m_InlineColor" ||
                prop.name == "m_Color" ||
                prop.name == "m_Material" ||
                prop.name == "m_RaycastTarget" ||
                prop.name == "m_RaycastPadding" ||
                prop.name == "m_Maskable" ||
                prop.name == "m_OnCullStateChanged")
            {
                continue;
            }
            
            EditorGUILayout.PropertyField(prop, true);
        }
    }
}
#endif