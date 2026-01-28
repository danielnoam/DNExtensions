using System;
using DNExtensions.Utilities.CustomFields;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNExtensions.Utilities.VFXManager
{
    public static class TransitionManager
    {
        private static Sequence _activeTransition;
        private static EffectSequence _pendingOutSequence;
        private static bool _isInitialized;
        private static bool _playingASequence;
        
        public static event Action OnTransitionStarted;
        public static event Action OnTransitionCompleted;
        
        static TransitionManager()
        {
            Initialize();
        }
        
        private static void Initialize()
        {
            if (_isInitialized) return;
            
            SceneManager.sceneLoaded += OnSceneLoaded;
            _isInitialized = true;
            _playingASequence = false;
        }
        
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!VFXManager.Instance) return;
            
            if (_pendingOutSequence && !_playingASequence)
            {
                VFXManager.Instance.PlaySequence(_pendingOutSequence);
                _pendingOutSequence = null;
            }
            else
            {
                VFXManager.Instance.ResetActiveEffects();
            }
            
            OnTransitionCompleted?.Invoke();
        }
        
        /// <summary>
        /// Transitions to a new scene with optional visual effects sequences for in and out transitions.
        /// </summary>
        public static void TransitionToScene(string sceneName, EffectSequence vfxSequenceIn = null, EffectSequence vfxSequenceOut = null)
        {
            if (!VFXManager.Instance)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
            
            if (_activeTransition.isAlive)
            {
                _activeTransition.Stop();
                VFXManager.Instance.ResetActiveEffects();
            }
            

            var transitionDuration = VFXManager.Instance.PlaySequence(vfxSequenceIn);
            _pendingOutSequence = vfxSequenceOut;
            _playingASequence = true;
            OnTransitionStarted?.Invoke();

            _activeTransition = Sequence.Create()
                .ChainDelay(transitionDuration)
                .ChainCallback(() =>
                {
                    _playingASequence = false;
                    SceneManager.LoadScene(sceneName);
                });
        }
        
        
        /// <summary>
        /// Transitions to a new scene by index with optional visual effects sequences for in and out transitions.
        /// </summary>
        public static void TransitionToScene(int sceneIndex, EffectSequence vfxSequenceIn = null, EffectSequence vfxSequenceOut = null)
        {
            if (!VFXManager.Instance)
            {
                SceneManager.LoadScene(sceneIndex);
                return;
            }
            
            if (_activeTransition.isAlive)
            {
                _activeTransition.Stop();
                VFXManager.Instance.ResetActiveEffects();
            }
            
            var transitionDuration = VFXManager.Instance.PlaySequence(vfxSequenceIn);
            _pendingOutSequence = vfxSequenceOut;
            _playingASequence = true;
            OnTransitionStarted?.Invoke();

            _activeTransition = Sequence.Create()
                .ChainDelay(transitionDuration)
                .ChainCallback(() =>
                {
                    _playingASequence = false;
                    SceneManager.LoadScene(sceneIndex);
                });
        }
        
        /// <summary>
        /// Transitions to a new scene using a SceneField with optional visual effects sequences for in and out transitions.
        /// </summary>
        public static void TransitionToScene(SceneField scene, EffectSequence vfxSequenceIn = null, EffectSequence vfxSequenceOut = null)
        {
            if (!VFXManager.Instance)
            {
                scene?.LoadScene();
                return;
            }
            
            if (_activeTransition.isAlive)
            {
                _activeTransition.Stop();
                VFXManager.Instance.ResetActiveEffects();
            }
            
            var transitionDuration = VFXManager.Instance.PlaySequence(vfxSequenceIn);
            _pendingOutSequence = vfxSequenceOut;
            _playingASequence = true;
            OnTransitionStarted?.Invoke();
            

            _activeTransition = Sequence.Create()
                .ChainDelay(transitionDuration)
                .ChainCallback(() =>
                {
                    _playingASequence = false;
                    scene?.LoadScene();
                });
        }
        
        /// <summary>
        /// Plays a transition then quits the application.
        /// </summary>
        public static void TransitionQuit(EffectSequence vfxSequenceIn = null) {
    
            if (!VFXManager.Instance)
            {
                Application.Quit();
                return;
            }

            if (_activeTransition.isAlive)
            {
                _activeTransition.Stop();
                VFXManager.Instance.ResetActiveEffects();
            }

            var transitionDuration = VFXManager.Instance.PlaySequence(vfxSequenceIn);
            _playingASequence = true;
            OnTransitionStarted?.Invoke();


            #if UNITY_EDITOR
            if (Application.isEditor && Application.isPlaying)
            {
                _activeTransition = Sequence.Create()
                    .ChainDelay(transitionDuration)
                    .ChainCallback(() =>
                    {
                        _playingASequence = false;
                        UnityEditor.EditorApplication.isPlaying = false;
                    });
                return;
            }
            #endif

            _activeTransition = Sequence.Create()
                .ChainDelay(transitionDuration)
                .ChainCallback(() =>
                {
                    _playingASequence = false;
                    Application.Quit();
                });
        }
    }

}