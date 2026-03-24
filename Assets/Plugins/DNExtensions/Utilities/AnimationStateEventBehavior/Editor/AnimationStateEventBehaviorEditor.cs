using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DNExtensions.Utilities
{
    [CustomEditor(typeof(AnimationStateEventBehavior))]
    public class AnimationStateEventBehaviorEditor : Editor
    {
        private AnimationClip _previewClip;
        private float _previewTime;
        private bool _isPreviewing;
        private GameObject _previewTarget;
        

        private void OnDisable()
        {
            if (!_isPreviewing) return;
        
            AnimationMode.StopAnimationMode();

            if (Selection.activeGameObject && Selection.activeGameObject.TryGetComponent(out Animator animator) && animator.avatar && animator.avatar.isHuman)
            {
                EnforceTPose();
            }
            _isPreviewing = false;
        }
        
        public override void OnInspectorGUI()
        {
            SerializedObject so = serializedObject;
            so.Update();

            EditorGUILayout.PropertyField(so.FindProperty("eventName"));
            EditorGUILayout.PropertyField(so.FindProperty("triggerAt"));

            if ((AnimationStateEventBehavior.TriggerAt)so.FindProperty("triggerAt").enumValueIndex == AnimationStateEventBehavior.TriggerAt.During)
                EditorGUILayout.PropertyField(so.FindProperty("triggerTime"));

            so.ApplyModifiedProperties();
            
            if (_isPreviewing && Selection.activeGameObject != _previewTarget)
            {
                AnimationMode.StopAnimationMode();
                _isPreviewing = false;
                _previewTarget = null;
            }
            
            AnimationStateEventBehavior stateBehavior = (AnimationStateEventBehavior)target;
            
            if (Validate(stateBehavior, out string errorMessage))
            {
                GUILayout.Space(10);

                if (_isPreviewing)
                {
                    if (GUILayout.Button("Stop Preview"))
                    {
                        AnimationMode.StopAnimationMode();
                        EnforceTPose();
                        _isPreviewing = false;
                    }
                    else
                    {
                        PreviewAnimationClip(stateBehavior);
                    }
                    
                    GUILayout.Label($"Previewing at {_previewTime:F2}s", EditorStyles.helpBox);
                }
                else if (GUILayout.Button("Preview"))
                {
                    _isPreviewing = true;
                    _previewTarget = Selection.activeGameObject;
                }
            }
            else
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Info);
            }
        }

        private void PreviewAnimationClip(AnimationStateEventBehavior stateBehavior)
        {
            if (!_previewClip) return;

            _previewTime = stateBehavior.triggerTime * _previewClip.length;
        
            if (!AnimationMode.InAnimationMode())
                AnimationMode.StartAnimationMode();
        
            AnimationMode.SampleAnimationClip(Selection.activeGameObject, _previewClip, _previewTime);
            SceneView.RepaintAll();
        }

        private bool Validate(AnimationStateEventBehavior stateBehavior, out string errorMessage)
        {
            AnimatorController controller = GetValidAnimatorController(out errorMessage);
            if (!controller) return false;

            ChildAnimatorState matchingState = controller.layers
                .SelectMany(layer => GetAllStates(layer.stateMachine))
                .FirstOrDefault(state => state.state.behaviours.Contains(stateBehavior));

            _previewClip = matchingState.state?.motion as AnimationClip;
            if (!_previewClip)
            {
                errorMessage = "No valid AnimationClip found for the current state";
                return false;
            }

            return true;
        }
        
        private IEnumerable<ChildAnimatorState> GetAllStates(AnimatorStateMachine stateMachine)
        {
            foreach (var state in stateMachine.states)
                yield return state;

            foreach (var sub in stateMachine.stateMachines)
            foreach (var state in GetAllStates(sub.stateMachine))
                yield return state;
        }

        private AnimatorController GetValidAnimatorController(out string errorMessage)
        {
            errorMessage = string.Empty;

            GameObject targetObject = Selection.activeGameObject;
            if (!targetObject)
            {
                errorMessage = "Please select a GameObject with an Animator to preview";
                return null;
            }
            
            Animator animator = targetObject.GetComponent<Animator>();
            if (!animator)
            {
                errorMessage = "The selected GameObject does not have an Animator component";
                return null;
            }
            
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
            if (!controller)
            {
                errorMessage = "The selected GameObject's Animator does not have a valid AnimatorController";
                return null;
            }
            
            return controller;
        }
        
        private static void EnforceTPose()
        {
            GameObject selected = Selection.activeGameObject;
            
            if (!selected || !selected.TryGetComponent(out Animator animator) || !animator.avatar) return;

            SkeletonBone[] skeletonBones = animator.avatar.humanDescription.skeleton;

            foreach (HumanBodyBones hbb in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (hbb == HumanBodyBones.LastBone) continue;
                
                Transform boneTransform = animator.GetBoneTransform(hbb);
                if (!boneTransform) continue;
                
                SkeletonBone skeletonBone = skeletonBones.FirstOrDefault(sb => sb.name == boneTransform.name);
                if (skeletonBone.name == null) continue;

                if (hbb == HumanBodyBones.Hips) boneTransform.localPosition = skeletonBone.position;
                boneTransform.localRotation = skeletonBone.rotation;
            }

        }
    }
}
