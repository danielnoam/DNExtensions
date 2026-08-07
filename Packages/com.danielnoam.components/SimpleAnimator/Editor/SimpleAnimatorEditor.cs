using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DNExtensions.Utilities
{
    [CustomEditor(typeof(SimpleAnimator))]
    internal class SimpleAnimatorEditor : Editor
    {
        private const string ControllerFolder = "Assets/SimpleAnimator";
        private const string ControllerPath = ControllerFolder + "/Dummy_AutoSync.controller";

        private SerializedProperty _clips;

        private void OnEnable()
        {
            _clips = serializedObject.FindProperty("clips");
            SyncController();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();

            if (EditorGUI.EndChangeCheck())
            {
                SyncController();
            }
            
            if (GUILayout.Button("Add Clip"))
            {
                AddClip();
            }

            if (GUILayout.Button("Create Clip"))
            {
                CreateClip();
            }
        }

        private void AddClip()
        {
            string path = EditorUtility.OpenFilePanel("Select Animation Clip", "Assets", "anim");

            if (string.IsNullOrEmpty(path)) return;

            string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(relativePath);

            if (!clip)
            {
                Debug.LogError("Selected file is not a valid AnimationClip.");
                return;
            }

            serializedObject.Update();
            int index = _clips.arraySize;
            _clips.InsertArrayElementAtIndex(index);
            _clips.GetArrayElementAtIndex(index).objectReferenceValue = clip;
            serializedObject.ApplyModifiedProperties();
            SyncController();
        }

        private void CreateClip()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Animation Clip", "NewClip", "anim", "Choose where to save the new clip");

            if (string.IsNullOrEmpty(path)) return;

            var clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
            AssetDatabase.SaveAssets();

            serializedObject.Update();
            int index = _clips.arraySize;
            _clips.InsertArrayElementAtIndex(index);
            _clips.GetArrayElementAtIndex(index).objectReferenceValue = clip;
            serializedObject.ApplyModifiedProperties();

            SyncController();
            EditorGUIUtility.PingObject(clip);
        }

        private void SyncController()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var animator = ((SimpleAnimator)target).GetComponent<Animator>();
            if (!animator) return;

            serializedObject.Update();
            var clips = new AnimationClip[_clips.arraySize];
            for (int i = 0; i < _clips.arraySize; i++)
            {
                clips[i] = _clips.GetArrayElementAtIndex(i).objectReferenceValue as AnimationClip;
            }

            var controller = EnsureController();

            if (animator.runtimeAnimatorController != controller)
            {
                Undo.RecordObject(animator, "Auto-assign SimpleAnimator Controller");
                animator.runtimeAnimatorController = controller;
                EditorUtility.SetDirty(animator);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var existingClips = stateMachine.states
                .Select(s => s.state.motion as AnimationClip)
                .ToArray();

            if (clips.SequenceEqual(existingClips)) return;

            foreach (var state in stateMachine.states)
            {
                stateMachine.RemoveState(state.state);
            }

            foreach (var clip in clips)
            {
                if (!clip) continue;
                var state = stateMachine.AddState(clip.name);
                state.motion = clip;
            }

            stateMachine.defaultState = null;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        private static AnimatorController EnsureController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller) return controller;

            if (!AssetDatabase.IsValidFolder(ControllerFolder))
            {
                Directory.CreateDirectory(ControllerFolder);
                AssetDatabase.Refresh();
            }

            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AssetDatabase.SaveAssets();
            return controller;
        }
    }
}