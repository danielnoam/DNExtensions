using UnityEngine;
using UnityEditor;

namespace DNExtensions.Utilities.AudioEvent.Editor
{
    [CustomEditor(typeof(SOAudioEvent))]
    public class AudioEventEditor : UnityEditor.Editor
    {
        private AudioSource _previewer;
        private bool _isPlaying;
        
        private SerializedProperty _clips;
        private SerializedProperty _mixerGroup;
        private SerializedProperty _volume;
        private SerializedProperty _pitch;
        private SerializedProperty _stereoPan;
        private SerializedProperty _spatialBlend;
        private SerializedProperty _reverbZoneMix;
        private SerializedProperty _bypassEffects;
        private SerializedProperty _bypassListenerEffects;
        private SerializedProperty _bypassReverbZones;
        private SerializedProperty _loop;
        private SerializedProperty _set3DSettings;

        private void OnEnable()
        {
            _previewer = EditorUtility
                .CreateGameObjectWithHideFlags("Audio Preview", HideFlags.HideAndDontSave, typeof(AudioSource))
                .GetComponent<AudioSource>();
            
            _clips = serializedObject.FindProperty("clips");
            _mixerGroup = serializedObject.FindProperty("mixerGroup");
            _volume = serializedObject.FindProperty("volume");
            _pitch = serializedObject.FindProperty("pitch");
            _stereoPan = serializedObject.FindProperty("stereoPan");
            _spatialBlend = serializedObject.FindProperty("spatialBlend");
            _reverbZoneMix = serializedObject.FindProperty("reverbZoneMix");
            _bypassEffects = serializedObject.FindProperty("bypassEffects");
            _bypassListenerEffects = serializedObject.FindProperty("bypassListenerEffects");
            _bypassReverbZones = serializedObject.FindProperty("bypassReverbZones");
            _loop = serializedObject.FindProperty("loop");
            _set3DSettings = serializedObject.FindProperty("set3DSettings");
        }

        private void OnDisable()
        {
            if (_previewer)
            {
                if (_previewer.isPlaying)
                    _previewer.Stop();
                DestroyImmediate(_previewer.gameObject);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            GUI.enabled = true;

            DrawSettings();
            DrawPreviewButtons();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSettings()
        {
            EditorGUILayout.PropertyField(_clips);
            EditorGUILayout.PropertyField(_mixerGroup);
            EditorGUILayout.PropertyField(_volume);
            EditorGUILayout.PropertyField(_pitch);
            EditorGUILayout.PropertyField(_stereoPan);
            EditorGUILayout.PropertyField(_spatialBlend);
            EditorGUILayout.PropertyField(_reverbZoneMix);
            EditorGUILayout.PropertyField(_bypassEffects);
            EditorGUILayout.PropertyField(_bypassListenerEffects);
            EditorGUILayout.PropertyField(_bypassReverbZones);
            EditorGUILayout.PropertyField(_loop);
            EditorGUILayout.PropertyField(_set3DSettings);
            
            if (_set3DSettings.boolValue)
            {
                EditorGUI.indentLevel++;
                SerializedProperty prop = _set3DSettings.Copy();
                while (prop.NextVisible(false))
                {
                    if (prop.name == "set3DSettings") continue;
                    EditorGUILayout.PropertyField(prop, true);
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawPreviewButtons()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            SOAudioEvent audioEvent = (SOAudioEvent)target;
            bool hasClips = audioEvent.clips != null && audioEvent.clips.Length > 0;

            EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects || !hasClips);

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button(_isPlaying ? "■ Stop" : "▶ Play"))
            {
                if (_isPlaying)
                {
                    audioEvent.Stop(_previewer);
                    _isPlaying = false;
                }
                else
                {
                    audioEvent.Play(_previewer);
                    _isPlaying = true;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (_isPlaying && _previewer && !_previewer.isPlaying)
            {
                _isPlaying = false;
            }

            if (!hasClips)
            {
                EditorGUILayout.HelpBox("No audio clips assigned", MessageType.Warning);
            }

            EditorGUI.EndDisabledGroup();
        }
    }
}