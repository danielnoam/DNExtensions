

using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.MobileHeptics
{
    public class MobileHapticsSetupWindow : EditorWindow
    {
        private const string ManifestPath = "Assets/Plugins/Android/AndroidManifest.xml";
        private const string VibratePermission = "android.permission.VIBRATE";
        
        private Vector2 _scrollPosition;
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.Info;

        [MenuItem("Tools/DNExtensions/Mobile Haptics Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<MobileHapticsSetupWindow>("Haptics Setup");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            CheckManifestStatus();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            DrawHeader();
            EditorGUILayout.Space(10);
            
            DrawManifestStatus();
            EditorGUILayout.Space(10);
            
            DrawActions();
            EditorGUILayout.Space(10);
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Mobile Haptics Setup & Validation", EditorStyles.largeLabel);
            EditorGUILayout.LabelField("Android vibration requires VIBRATE permission", EditorStyles.miniLabel);
        }

        private void DrawManifestStatus()
        {
            EditorGUILayout.LabelField("Manifest Status", EditorStyles.boldLabel);
            
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }

            EditorGUILayout.Space(5);
            
            // Show manifest path
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Location:", GUILayout.Width(70));
            EditorGUILayout.SelectableLabel(ManifestPath, EditorStyles.textField, GUILayout.Height(18));
            EditorGUILayout.EndHorizontal();

            // File exists indicator
            bool manifestExists = File.Exists(ManifestPath);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("File Exists:", GUILayout.Width(70));
            EditorGUILayout.LabelField(manifestExists ? "✓ Yes" : "✕ No", 
                manifestExists ? EditorStyles.boldLabel : EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            // Permission status
            if (manifestExists)
            {
                bool hasPermission = CheckForPermission();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Permission:", GUILayout.Width(70));
                EditorGUILayout.LabelField(hasPermission ? "✓ Configured" : "⚠ Missing", 
                    hasPermission ? EditorStyles.boldLabel : EditorStyles.label);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Refresh Status", GUILayout.Height(30)))
            {
                CheckManifestStatus();
            }
            
            if (GUILayout.Button("Setup Manifest", GUILayout.Height(30)))
            {
                SetupManifest();
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (File.Exists(ManifestPath))
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Open Manifest"))
                {
                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(
                        Path.GetFullPath(ManifestPath), 1);
                }
                
                if (GUILayout.Button("Ping in Project"))
                {
                    var manifest = AssetDatabase.LoadAssetAtPath<Object>(ManifestPath);
                    EditorGUIUtility.PingObject(manifest);
                    Selection.activeObject = manifest;
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private void CheckManifestStatus()
        {
            if (!File.Exists(ManifestPath))
            {
                _statusMessage = "✕ AndroidManifest.xml not found. Click 'Setup Manifest' to create it.";
                _statusType = MessageType.Warning;
                return;
            }

            if (!IsValidXml(ManifestPath))
            {
                _statusMessage = "✕ AndroidManifest.xml exists but is not valid XML. Please fix or recreate it.";
                _statusType = MessageType.Error;
                return;
            }

            bool hasPermission = CheckForPermission();
            
            if (hasPermission)
            {
                _statusMessage = "✓ AndroidManifest.xml is properly configured with VIBRATE permission.";
                _statusType = MessageType.Info;
            }
            else
            {
                _statusMessage = "⚠ AndroidManifest.xml exists but is missing VIBRATE permission. Click 'Setup Manifest' to add it.";
                _statusType = MessageType.Warning;
            }
        }

        private void SetupManifest()
        {
            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(ManifestPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(ManifestPath))
                {
                    // Create new manifest
                    CreateNewManifest();
                    _statusMessage = "✓ Created new AndroidManifest.xml with VIBRATE permission.";
                    _statusType = MessageType.Info;
                }
                else
                {
                    // Add permission to existing manifest
                    if (CheckForPermission())
                    {
                        _statusMessage = "✓ VIBRATE permission already exists in manifest.";
                        _statusType = MessageType.Info;
                    }
                    else
                    {
                        InjectPermission();
                        _statusMessage = "✓ Added VIBRATE permission to existing AndroidManifest.xml.";
                        _statusType = MessageType.Info;
                    }
                }

                AssetDatabase.Refresh();
                Repaint();
            }
            catch (System.Exception e)
            {
                _statusMessage = $"✕ Failed to setup manifest: {e.Message}";
                _statusType = MessageType.Error;
                Debug.LogError($"[MobileHaptics] Setup failed: {e}");
            }
        }

        private void CreateNewManifest()
        {
            string manifestContent = 
@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"">
    <!-- Required for haptic feedback on Android -->
    <uses-permission android:name=""android.permission.VIBRATE"" />
</manifest>";

            File.WriteAllText(ManifestPath, manifestContent);
        }

        private void InjectPermission()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(ManifestPath);

            // Setup namespace manager for Android namespace
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("android", "http://schemas.android.com/apk/res/android");

            XmlNode manifest = doc.SelectSingleNode("/manifest");
            if (manifest == null)
            {
                throw new System.Exception("Invalid AndroidManifest.xml - no <manifest> root element found");
            }

            // Check if permission already exists using namespace manager
            XmlNode existingPermission = doc.SelectSingleNode($"//uses-permission[@android:name='{VibratePermission}']", nsmgr);
            if (existingPermission != null)
            {
                return; // Already exists
            }

            // Create permission element
            XmlElement permission = doc.CreateElement("uses-permission");
            XmlAttribute nameAttr = doc.CreateAttribute("android", "name", "http://schemas.android.com/apk/res/android");
            nameAttr.Value = VibratePermission;
            permission.Attributes.Append(nameAttr);

            // Add comment
            XmlComment comment = doc.CreateComment(" Required for haptic feedback on Android ");
            
            // Insert at the beginning of manifest
            if (manifest.FirstChild != null)
            {
                manifest.InsertBefore(comment, manifest.FirstChild);
                manifest.InsertBefore(permission, manifest.FirstChild);
            }
            else
            {
                manifest.AppendChild(comment);
                manifest.AppendChild(permission);
            }

            doc.Save(ManifestPath);
        }

        private bool CheckForPermission()
        {
            if (!File.Exists(ManifestPath)) return false;

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(ManifestPath);
                
                // Setup namespace manager for Android namespace
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("android", "http://schemas.android.com/apk/res/android");
                
                XmlNode permission = doc.SelectSingleNode($"//uses-permission[@android:name='{VibratePermission}']", nsmgr);
                return permission != null;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MobileHaptics] Failed to parse manifest: {e.Message}");
                return false;
            }
        }

        private bool IsValidXml(string path)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
