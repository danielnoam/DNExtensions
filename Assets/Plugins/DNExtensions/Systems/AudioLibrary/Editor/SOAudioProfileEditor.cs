using UnityEngine;
using UnityEditor;

namespace DNExtensions.Systems.AudioLibrary
{
    [CustomEditor(typeof(SOAudioProfile))]
    public class SOAudioProfileEditor : Editor
    {
        private AudioSource _previewer;
        private bool _isPlaying;
        
        // Settings are grouped by the Editor, not the script
        private void OnEnable()
        {
            _previewer = EditorUtility
                .CreateGameObjectWithHideFlags("Audio Preview", HideFlags.HideAndDontSave, typeof(AudioSource))
                .GetComponent<AudioSource>();
        }

        private void OnDisable()
        {
            if (_previewer)
            {
                if (_previewer.isPlaying) _previewer.Stop();
                DestroyImmediate(_previewer.gameObject);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            GUI.enabled = true;

            // Draw everything except m_Script and the 3D details
            DrawPropertiesExcluding(serializedObject, "m_Script", "dopplerLevel", "spread", "rolloffMode", "minDistance", "maxDistance");
            
            // Conditional 3D Draw
            if (serializedObject.FindProperty("set3DSettings").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dopplerLevel"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spread"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rolloffMode"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxDistance"));
                EditorGUI.indentLevel--;
            }

            DrawPreviewButtons();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPreviewButtons()
        {
            EditorGUILayout.Space();
            SOAudioProfile profile = (SOAudioProfile)target;
            bool hasClips = profile.clips != null && profile.clips.Length > 0;

            EditorGUI.BeginDisabledGroup(!hasClips);
            if (GUILayout.Button(_isPlaying ? "■ Stop" : "▶ Play Preview"))
            {
                if (_isPlaying)
                {
                    _previewer.Stop();
                    _isPlaying = false;
                }
                else
                {
                    ApplySettingsToSource(_previewer, profile.GetSettings());
                    _previewer.Play();
                    _isPlaying = true;
                }
            }
            EditorGUI.EndDisabledGroup();

            if (_isPlaying && !_previewer.isPlaying) _isPlaying = false;
        }

        private void ApplySettingsToSource(AudioSource source, AudioSettings settings)
        {
            source.clip = settings.clip;
            source.volume = settings.volume;
            source.pitch = settings.pitch;
            source.panStereo = settings.stereoPan;
            source.spatialBlend = settings.spatialBlend;
            source.loop = settings.loop;
            // Apply 3D settings if enabled
            if (settings.set3DSettings)
            {
                source.minDistance = settings.minDistance;
                source.maxDistance = settings.maxDistance;
                source.rolloffMode = settings.rolloffMode;
            }
        }
    }
}