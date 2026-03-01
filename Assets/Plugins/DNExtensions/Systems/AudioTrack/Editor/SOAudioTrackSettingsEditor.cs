using System.IO;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.AudioTrack
{
    [CustomEditor(typeof(SOAudioTrackSettings))]
    internal class SOAudioTrackSettingsEditor : Editor
    {
        private const string SettingsPath = "Assets/Resources/AudioTrackSettings.asset";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("enabled"));

            EditorGUILayout.Space(10);

            SerializedProperty tracksProp = serializedObject.FindProperty("tracks");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);

            const float idWidth = 80f;
            const float volWidth = 25f;
            const float removeWidth = 24f;
            const float padding = 45f;

            float available = EditorGUIUtility.currentViewWidth - idWidth - volWidth - removeWidth - padding;
            float clipWidth = available * 0.5f;
            float mixerWidth = available * 0.5f;

            // Column headers
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6);
            EditorGUILayout.LabelField("ID", EditorStyles.boldLabel, GUILayout.Width(idWidth));
            EditorGUILayout.LabelField("Clip", EditorStyles.boldLabel, GUILayout.Width(clipWidth));
            EditorGUILayout.LabelField("Mixer Group", EditorStyles.boldLabel, GUILayout.Width(mixerWidth));
            EditorGUILayout.LabelField("Vol", EditorStyles.boldLabel, GUILayout.Width(volWidth));
            GUILayout.Space(removeWidth);
            EditorGUILayout.EndHorizontal();

            Rect lineRect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            GUILayout.Space(4);

            if (tracksProp.arraySize == 0)
            {
                EditorGUILayout.LabelField("No tracks defined.", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < tracksProp.arraySize; i++)
                {
                    SerializedProperty track = tracksProp.GetArrayElementAtIndex(i);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(6);
                    EditorGUILayout.PropertyField(track.FindPropertyRelative("id"), GUIContent.none, GUILayout.Width(idWidth));
                    EditorGUILayout.PropertyField(track.FindPropertyRelative("clip"), GUIContent.none, GUILayout.Width(clipWidth));
                    EditorGUILayout.PropertyField(track.FindPropertyRelative("mixerGroup"), GUIContent.none, GUILayout.Width(mixerWidth));
                    EditorGUILayout.PropertyField(track.FindPropertyRelative("volume"), GUIContent.none, GUILayout.Width(volWidth));

                    if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20)))
                    {
                        tracksProp.DeleteArrayElementAtIndex(i);
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(3);
                }
            }

            GUILayout.Space(3);
            if (GUILayout.Button("+ New Track"))
            {
                tracksProp.InsertArrayElementAtIndex(tracksProp.arraySize);
                SerializedProperty newTrack = tracksProp.GetArrayElementAtIndex(tracksProp.arraySize - 1);
                newTrack.FindPropertyRelative("id").stringValue = "New_ID";
                newTrack.FindPropertyRelative("clip").objectReferenceValue = null;
                newTrack.FindPropertyRelative("mixerGroup").objectReferenceValue = null;
                newTrack.FindPropertyRelative("volume").floatValue = 1f;
            }

            GUILayout.Space(5);
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }


        [MenuItem("Tools/DNExtensions/Audio Track Settings")]
        private static void OpenSettings()
        {
            var settings = SOAudioTrackSettings.Instance;

            if (!settings)
            {
                string[] guids = AssetDatabase.FindAssets("t:SOAudioTrackSettings");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    settings = AssetDatabase.LoadAssetAtPath<SOAudioTrackSettings>(path);
                }
            }

            if (settings)
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
            else
            {
                if (EditorUtility.DisplayDialog(
                    "Create Audio Track Settings?",
                    "No Audio Track Settings found. Create one now?\n\n" +
                    "It will be created at: " + SettingsPath + "\n\n" +
                    "Note: Must be in a Resources folder for runtime access.",
                    "Create", "Cancel"))
                {
                    CreateSettingsAsset();
                }
            }
        }

        private static void CreateSettingsAsset()
        {
            var settings = CreateInstance<SOAudioTrackSettings>();

            string directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);

            Debug.Log("Created Audio Track Settings at " + SettingsPath);
        }
    }
}