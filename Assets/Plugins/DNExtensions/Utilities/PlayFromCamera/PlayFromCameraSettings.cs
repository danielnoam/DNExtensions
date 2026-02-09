using System.IO;
using UnityEditorInternal;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DNExtensions.Utilities.PlayFromCamera
{
    public class PlayFromCameraSettings : ScriptableObject
    {
        private const string SettingsPath = "ProjectSettings/DNExtensions_PlayFromCameraSettings.asset";
        
        [Tooltip("If enabled, the player's rotation will also be set to match the camera rotation")]
        [SerializeField] private bool alsoSetRotation;
        [Tooltip("How to find the player object in the scene")]
        [SerializeField] private PlayerSelectionMode playerSelectionMode = PlayerSelectionMode.ByTag;
        [Tooltip("Tag to search for when finding the player object")]
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Hierarchy path to the player object (e.g., 'Player', 'Managers/Player')")]
        [SerializeField] private string playerObjectPath = "";

        public bool AlsoSetRotation => alsoSetRotation;
        public PlayerSelectionMode PlayerSelectionMode => playerSelectionMode;
        public string PlayerTag => playerTag;
        public string PlayerObjectPath => playerObjectPath;

        private static PlayFromCameraSettings _instance;

        public static PlayFromCameraSettings Instance
        {
            get
            {
#if UNITY_EDITOR
                if (_instance == null)
                {
                    _instance = LoadOrCreate();
                }
                return _instance;
#else
                return null;
#endif
            }
        }

#if UNITY_EDITOR
        private static PlayFromCameraSettings LoadOrCreate()
        {
            var settings = CreateInstance<PlayFromCameraSettings>();
            
            if (File.Exists(SettingsPath))
            {
                InternalEditorUtility.LoadSerializedFileAndForget(SettingsPath);
            }
            
            return settings;
        }

        internal void Save()
        {
            InternalEditorUtility.SaveToSerializedFileAndForget(new[] { this }, SettingsPath, true);
        }
        
#endif
    }

    public enum PlayerSelectionMode
    {
        ByTag = 0,
        ByPath = 1
    }

#if UNITY_EDITOR
    static class PlayFromCameraSettingsProvider
    {
        private const string MenuPath = "Tools/DNExtensions/Play from Camera Settings";

        [MenuItem(MenuPath, false)]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/DNExtensions/Play from Camera");
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/DNExtensions/Play from Camera", SettingsScope.Project)
            {
                label = "Play from Camera",
                guiHandler = (searchContext) =>
                {
                    var settings = new SerializedObject(PlayFromCameraSettings.Instance);
                    
                    EditorGUILayout.Space(5);
                    
                    EditorGUI.BeginChangeCheck();
                    
                    EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                    
                    EditorGUILayout.PropertyField(settings.FindProperty("alsoSetRotation"),
                        new GUIContent("Also Set Rotation", "Also match the player's rotation to the camera"));
                    
                    
                    var playerSelectionMode = settings.FindProperty("playerSelectionMode");
                    EditorGUILayout.PropertyField(playerSelectionMode,
                        new GUIContent("Selection Mode", "How to find the player object"));
                    
                    if (playerSelectionMode.enumValueIndex == (int)PlayerSelectionMode.ByTag)
                    {
                        EditorGUILayout.PropertyField(settings.FindProperty("playerTag"),
                            new GUIContent("Player Tag", "Tag to search for"));
                    }
                    else if (playerSelectionMode.enumValueIndex == (int)PlayerSelectionMode.ByPath)
                    {
                        EditorGUILayout.PropertyField(settings.FindProperty("playerObjectPath"),
                            new GUIContent("Player Path", "Hierarchy path (e.g., 'Player' or 'GameManager/Player')"));
                        
                        EditorGUILayout.HelpBox(
                            "Examples:\n" +
                            "• 'Player' - object at root level\n" +
                            "• 'GameManager/Player' - nested object",
                            MessageType.Info
                        );
                    }
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.ApplyModifiedProperties();
                        PlayFromCameraSettings.Instance.Save();
                    }
                    
                    EditorGUILayout.Space(10);
                    
                    if (GUILayout.Button("Find Player Object"))
                    {
                        TestFindPlayer();
                    }
                    
                    EditorGUILayout.Space(5);
                    
                    EditorGUILayout.HelpBox(
                        "How to use:\n" +
                        "1. Position Scene View camera where you want player to spawn\n" +
                        "2. Use menu: Tools > Play from Camera Position (Ctrl+Alt+Shift+P)\n" +
                        "3. Or click the toolbar button",
                        MessageType.Info
                    );
                    
                },
                
                keywords = new[] { "Play", "Camera", "Position", "Player", "Spawn", "DNExtensions" }
            };

            return provider;
        }

        private static void TestFindPlayer()
        {
            var settings = PlayFromCameraSettings.Instance;
            GameObject playerObj = null;

            switch (settings.PlayerSelectionMode)
            {
                case PlayerSelectionMode.ByTag:
                    try
                    {
                        playerObj = GameObject.FindWithTag(settings.PlayerTag);
                    }
                    catch (UnityException e)
                    {
                        Debug.LogError($"Tag '{settings.PlayerTag}' is not defined: {e.Message}");
                        return;
                    }
                    break;
                    
                case PlayerSelectionMode.ByPath:
                    playerObj = GameObject.Find(settings.PlayerObjectPath);
                    break;
            }

            if (playerObj != null)
            {
                EditorGUIUtility.PingObject(playerObj);
                Selection.activeGameObject = playerObj;
                Debug.Log($"✓ Found player: {playerObj.name} at {playerObj.transform.position}");
            }
            else
            {
                Debug.LogWarning($"✗ Could not find player with current settings!");
            }
        }
    }
#endif
}