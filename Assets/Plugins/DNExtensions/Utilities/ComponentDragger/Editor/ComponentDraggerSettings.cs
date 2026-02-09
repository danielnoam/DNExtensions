using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditorInternal;

namespace DNExtensions.Utilities.ComponentDragger
{
    public class ComponentDraggerWindow : EditorWindow
    {

    }
    
    
    public class ComponentDraggerSettings : ScriptableObject
    {
        private const string SettingsPath = "ProjectSettings/DNExtensions_ComponentDraggerSettings.asset";

        [Header("Behavior")]
        [Tooltip("Hold ALT to toggle between copy/move. When enabled, ALT copies; when disabled, ALT moves.")]
        [SerializeField] private bool copyByDefault;
        [Tooltip("Automatically transfer dependent components (e.g., AudioFilters with AudioSource)")]
        [SerializeField] private bool transferDependencies = true;

        [Header("Warnings")]
        [Tooltip("Show warning dialogs for potentially unsafe operations")]
        [SerializeField] private bool showWarnings = true;
        [Tooltip("Confirm before moving components that might break references")]
        [SerializeField] private bool confirmRiskyMoves;

        public bool CopyByDefault => copyByDefault;
        public bool TransferDependencies => transferDependencies;
        public bool ShowWarnings => showWarnings;
        public bool ConfirmRiskyMoves => confirmRiskyMoves;

        private static ComponentDraggerSettings _instance;

        internal static ComponentDraggerSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadOrCreate();
                }
                return _instance;
            }
        }
        

        private static ComponentDraggerSettings LoadOrCreate()
        {
            var settings = CreateInstance<ComponentDraggerSettings>();
            
            if (File.Exists(SettingsPath))
            {
                InternalEditorUtility.LoadSerializedFileAndForget(SettingsPath);
            }
            
            return settings;
        }

        internal void Save()
        {
            InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { this }, SettingsPath, true);
        }

        public void ResetToDefaults()
        {
            copyByDefault = false;
            transferDependencies = true;
            showWarnings = true;
            confirmRiskyMoves = false;
            Save();
        }
    }

    static class ComponentDraggerSettingsProvider
    {
        private const string MenuPath = "Tools/DNExtensions/Component Dragger Settings";
        private const int MenuPriority = 1000;

        [MenuItem(MenuPath, false, MenuPriority)]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/DNExtensions/Component Dragger");
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/DNExtensions/Component Dragger", SettingsScope.Project)
            {
                label = "Component Dragger",
                guiHandler = (searchContext) =>
                {
                    var settings = new SerializedObject(ComponentDraggerSettings.Instance);
                    
                    EditorGUILayout.Space(10);
                    
                    EditorGUILayout.HelpBox(
                        "Usage:\n" +
                        "• Drag any component header from Inspector\n" +
                        "• Drop onto GameObject in Hierarchy\n" +
                        "• Hold ALT to toggle copy/move behavior\n" +
                        "• Undo/Redo supported (Ctrl+Z / Ctrl+Y)",
                        MessageType.Info
                    );
                    
                    EditorGUILayout.Space(10);
                    
                    EditorGUI.BeginChangeCheck();
                    
                    EditorGUILayout.PropertyField(settings.FindProperty("copyByDefault"), 
                        new GUIContent("Copy by Default", "When enabled, components are copied instead of moved (hold ALT to move). When disabled, components are moved (hold ALT to copy)."));
                    
                    EditorGUILayout.PropertyField(settings.FindProperty("transferDependencies"),
                        new GUIContent("Transfer Dependencies", "Automatically move dependent components (e.g., AudioFilters follow AudioSource)"));
                    
                    EditorGUILayout.Space(5);
                    
                    EditorGUILayout.PropertyField(settings.FindProperty("showWarnings"),
                        new GUIContent("Show Warnings", "Display warning messages for potentially unsafe operations"));
                    
                    EditorGUILayout.PropertyField(settings.FindProperty("confirmRiskyMoves"),
                        new GUIContent("Confirm Risky Moves", "Ask for confirmation before moving components that might break"));
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.ApplyModifiedProperties();
                        ComponentDraggerSettings.Instance.Save();
                    }
                    

                    
                    EditorGUILayout.Space(5);
                    
                    if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
                    {
                        if (EditorUtility.DisplayDialog(
                            "Reset Settings",
                            "Reset all Component Dragger settings to their default values?",
                            "Reset",
                            "Cancel"))
                        {
                            ComponentDraggerSettings.Instance.ResetToDefaults();
                        }
                    }
                },
                
                keywords = new[] { "Component", "Dragger", "DNExtensions", "Drag", "Drop", "Copy", "Move" }
            };

            return provider;
        }
    }
}