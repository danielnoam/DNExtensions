using System;
using DNExtensions.Utilities.CustomFields;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNExtensions.Systems.VFXManager
{
    public static class TransitionManager
    {
        private static Sequence _activeTransition;
        private static EffectSequence _pendingOutSequence;
        private static bool _isInitialized;
        private static bool _playingASequence;
        

        static TransitionManager()
        {
            Initialize();
        }

        private static void Initialize()
        {
            if (_isInitialized) return;
            SceneManager.sceneLoaded += OnSceneLoaded;
            _isInitialized = true;
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
        }

        private static void StartTransition(EffectSequence sequenceIn, EffectSequence sequenceOut, Action loadAction)
        {
            if (_activeTransition.isAlive)
            {
                _activeTransition.Stop();
                VFXManager.Instance.ResetActiveEffects();
            }

            var duration = VFXManager.Instance.PlaySequence(sequenceIn);
            _pendingOutSequence = sequenceOut;
            _playingASequence = true;

            _activeTransition = Sequence.Create()
                .ChainDelay(duration)
                .ChainCallback(() =>
                {
                    _playingASequence = false;
                    loadAction();
                });
        }

        /// <summary>
        /// Transitions to a new scene with optional visual effects sequences for in and out transitions.
        /// </summary>
        public static void TransitionToScene(string sceneName, EffectSequence sequenceIn = null, EffectSequence sequenceOut = null)
        {
            if (!VFXManager.Instance)
            {
                SceneManager.LoadScene(sceneName); return; 
            }
            StartTransition(sequenceIn, sequenceOut, () => SceneManager.LoadScene(sceneName));
        }

        /// <summary>
        /// Transitions to a new scene by index with optional visual effects sequences for in and out transitions.
        /// </summary>
        public static void TransitionToScene(int sceneIndex, EffectSequence sequenceIn = null, EffectSequence sequenceOut = null)
        {
            if (!VFXManager.Instance)
            {
                SceneManager.LoadScene(sceneIndex); return;
            }
            StartTransition(sequenceIn, sequenceOut, () => SceneManager.LoadScene(sceneIndex));
        }

        /// <summary>
        /// Transitions to a new scene using a SceneField with optional visual effects sequences for in and out transitions.
        /// </summary>
        public static void TransitionToScene(SceneField scene, EffectSequence sequenceIn = null, EffectSequence sequenceOut = null)
        {
            if (!VFXManager.Instance)
            {
                scene?.LoadScene(); return; 
            }
            StartTransition(sequenceIn, sequenceOut, () => scene?.LoadScene());
        }

        /// <summary>
        /// Plays a transition then quits the application.
        /// </summary>
        public static void TransitionQuit(EffectSequence sequenceIn = null)
        {
            if (!VFXManager.Instance)
            {
                Application.Quit(); return; 
            }

            #if UNITY_EDITOR
            StartTransition(sequenceIn, null, () => UnityEditor.EditorApplication.isPlaying = false);
            #else
            StartTransition(sequenceIn, null, () => Application.Quit());
            #endif
        }
    }
}