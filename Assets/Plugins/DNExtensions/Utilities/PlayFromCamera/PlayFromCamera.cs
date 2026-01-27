

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;


namespace DNExtensions.Utilities.PlayFromCamera
{
    
    public enum PlayerSelectionMode
    {
        ByTag = 0,
        ByPath = 1
    }
    
    
    [InitializeOnLoad]
    public static class PlayFromCamera
    {
        static PlayFromCamera()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }
        
        
        public static void PlayFromCurrentCamera()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (!sceneView)
            {
                EditorUtility.DisplayDialog("No Scene View", 
                    "No active Scene View found! Please make sure you have a Scene View window open.", 
                    "OK");
                return;
            }

            Vector3 cameraPosition = sceneView.camera.transform.position;
            Quaternion cameraRotation = sceneView.camera.transform.rotation;

            EditorPrefs.SetString("PlayFromCamera_Position", $"{cameraPosition.x},{cameraPosition.y},{cameraPosition.z}");
            EditorPrefs.SetString("PlayFromCamera_Rotation", $"{cameraRotation.x},{cameraRotation.y},{cameraRotation.z},{cameraRotation.w}");
            EditorPrefs.SetBool("PlayFromCamera_Pending", true);
            EditorPrefs.SetBool("PlayFromCamera_SetRotation", PlayFromCameraSettings.AlsoSetRotation);
            EditorPrefs.SetString("PlayFromCamera_PlayerTag", PlayFromCameraSettings.PlayerTag);
            EditorPrefs.SetString("PlayFromCamera_PlayerPath", PlayFromCameraSettings.PlayerObjectPath);
            EditorPrefs.SetInt("PlayFromCamera_PlayerMode", (int)PlayFromCameraSettings.PlayerSelectionMode);

            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && EditorPrefs.GetBool("PlayFromCamera_Pending", false))
            {
                EditorPrefs.SetBool("PlayFromCamera_Pending", false);
                EditorApplication.delayCall += () => TeleportPlayerWithRetry(0);
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorPrefs.SetBool("PlayFromCamera_Pending", false);
            }
        }

        private static void TeleportPlayerWithRetry(int attemptCount)
        {
            if (TeleportPlayerDirect())
            {
                return;
            }
            
            if (attemptCount < 5)
            {
                int nextAttempt = attemptCount + 1;
                EditorApplication.delayCall += () => TeleportPlayerWithRetry(nextAttempt);
            }
            else
            {
                Debug.LogWarning("Could not find player object to teleport. Check your player settings in the Play from Camera settings.");
            }
        }

        private static bool TeleportPlayerDirect()
        {
            string posStr = EditorPrefs.GetString("PlayFromCamera_Position", "0,0,0");
            string rotStr = EditorPrefs.GetString("PlayFromCamera_Rotation", "0,0,0,1");
            bool setRotation = EditorPrefs.GetBool("PlayFromCamera_SetRotation", false);
            string playerTag = EditorPrefs.GetString("PlayFromCamera_PlayerTag", "Player");
            string playerPath = EditorPrefs.GetString("PlayFromCamera_PlayerPath", "");
            PlayerSelectionMode playerMode = (PlayerSelectionMode)EditorPrefs.GetInt("PlayFromCamera_PlayerMode", 0);
            
            string[] posParts = posStr.Split(',');
            Vector3 targetPos = new Vector3(float.Parse(posParts[0]), float.Parse(posParts[1]), float.Parse(posParts[2]));
            
            string[] rotParts = rotStr.Split(',');
            Quaternion targetRot = new Quaternion(float.Parse(rotParts[0]), float.Parse(rotParts[1]), float.Parse(rotParts[2]), float.Parse(rotParts[3]));
            
            GameObject player = FindPlayerObject(playerMode, playerTag, playerPath);
            
            if (player)
            {
                var characterController = player.GetComponent<CharacterController>();
                bool ccWasEnabled = false;
                if (characterController)
                {
                    ccWasEnabled = characterController.enabled;
                    characterController.enabled = false;
                }
                
                player.transform.position = targetPos;
                if (setRotation)
                {
                    player.transform.rotation = targetRot;
                }
                
                if (characterController && ccWasEnabled)
                {
                    EditorApplication.delayCall += () => 
                    {
                        if (characterController != null)
                        {
                            characterController.enabled = true;
                        }
                    };
                }
                
                var rigidbody = player.GetComponent<Rigidbody>();
                if (rigidbody)
                {
                    rigidbody.linearVelocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                }
                
                return true;
            }
            
            return false;
        }
        
        private static GameObject FindPlayerObject(PlayerSelectionMode mode, string tag, string path)
        {
            GameObject player = null;
            
            if (mode == PlayerSelectionMode.ByTag)
            {
                try
                {
                    player = GameObject.FindWithTag(tag);
                }
                catch (UnityException)
                {
                }
            }
            else if (mode == PlayerSelectionMode.ByPath)
            {
                player = GameObject.Find(path);
            }
            
            if (!player)
            {
                string[] commonNames = { "Player", "player", "FPSController", "FirstPersonController", "Character", "PlayerCharacter" };
                foreach (string name in commonNames)
                {
                    player = GameObject.Find(name);
                    if (player != null) break;
                }
                
                if (player == null)
                {
                    GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    foreach (GameObject obj in allObjects)
                    {
                        if (obj.name.ToLower().Contains("player"))
                        {
                            player = obj;
                            break;
                        }
                    }
                }
            }
            
            return player;
        }
    }
    
}

#endif