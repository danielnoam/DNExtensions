using UnityEngine;

namespace DNExtensions.Systems.MobileHaptics
{
    /// <summary>
    /// Provides cross-platform haptic feedback for mobile devices.
    /// Android supports duration control. iOS uses system haptic patterns.
    /// No GameObject required — initializes automatically on first use.
    /// </summary>
    public static class MobileHaptics
    {
        private static bool _isInitialized;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
#endif

        private static void Initialize()
        {
            if (_isInitialized) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MobileHaptics] Failed to initialize: {e.Message}");
            }
#endif
            _isInitialized = true;
        }

        /// <summary>
        /// Triggers haptic feedback.
        /// </summary>
        /// <param name="milliseconds">Duration in milliseconds. Android only — iOS uses system default.</param>
        public static void Vibrate(long milliseconds = 50)
        {
            if (!_isInitialized) Initialize();

#if UNITY_ANDROID && !UNITY_EDITOR
            _vibrator?.Call("vibrate", milliseconds);
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        /// <summary>
        /// Returns true if the device supports haptic feedback.
        /// </summary>
        public static bool IsSupported()
        {
            if (!_isInitialized) Initialize();

#if UNITY_ANDROID && !UNITY_EDITOR
            return _vibrator?.Call<bool>("hasVibrator") ?? false;
#elif UNITY_IOS && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Cancels any ongoing haptic feedback. Android only.
        /// </summary>
        public static void Cancel()
        {
            if (!_isInitialized) Initialize();

#if UNITY_ANDROID && !UNITY_EDITOR
            _vibrator?.Call("cancel");
#endif
        }
    }
}