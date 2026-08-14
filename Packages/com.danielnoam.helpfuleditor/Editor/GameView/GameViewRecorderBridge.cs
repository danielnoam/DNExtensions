using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.GameView
{
    /// <summary>
    /// Hands a recording to Unity Recorder, for the takes that need audio or exact frame capture.
    ///
    /// Driving Recorder rather than reimplementing it: capturing every frame in step with the game
    /// needs a component inside the play session, a coroutine on WaitForEndOfFrame, request
    /// coalescing and per-frame audio buffers — all of which Recorder already does, and all of which
    /// the suite's own recorder deliberately does not attempt. What is added here is the part Recorder
    /// has no answer for: starting one from a button on the Game View.
    ///
    /// Reached by reflection rather than an assembly reference, so the suite compiles and runs whether
    /// or not the package is installed. Everything is a public API of Recorder's, so this is only
    /// bridging a dependency, not reaching into anything private.
    /// </summary>
    internal static class GameViewRecorderBridge
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static Type _controllerType;
        private static Type _controllerSettingsType;
        private static Type _movieSettingsType;
        private static Type _gameViewInputType;
        private static Type _coreEncoderType;

        private static bool _resolved;
        private static bool _available;
        private static bool _warned;

        private static object _controller;
        private static ScriptableObject _controllerSettings;
        private static ScriptableObject _movieSettings;

        public static bool Available
        {
            get
            {
                Resolve();
                return _available;
            }
        }

        public static bool IsRecording
        {
            get
            {
                if (_controller == null) return false;

                try
                {
                    return _controllerType.GetMethod("IsRecording", AnyInstance)?.Invoke(_controller, null) is true;
                }
                catch (Exception e)
                {
                    WarnOnce(e);
                    return false;
                }
            }
        }

        /// <summary>
        /// Resolved once per domain. Found by walking the loaded assemblies rather than by an
        /// assembly-qualified name, which would tie this to how the package happens to be named.
        /// </summary>
        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            try
            {
                _controllerType = FindType("UnityEditor.Recorder.RecorderController");
                _controllerSettingsType = FindType("UnityEditor.Recorder.RecorderControllerSettings");
                _movieSettingsType = FindType("UnityEditor.Recorder.MovieRecorderSettings");
                _gameViewInputType = FindType("UnityEditor.Recorder.Input.GameViewInputSettings");
                _coreEncoderType = FindType("UnityEditor.Recorder.Encoder.CoreEncoderSettings");

                _available = _controllerType != null && _controllerSettingsType != null &&
                             _movieSettingsType != null && _gameViewInputType != null;
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null) return type;
            }

            return null;
        }

        /// <summary>
        /// Starts a take. The path carries no extension — Recorder appends whichever the codec needs,
        /// which is also why the file it writes is not necessarily the .mp4 the built-in path produces.
        /// </summary>
        public static bool Start(int width, int height, int fps, RecordingQuality quality, string pathWithoutExtension)
        {
            if (!Available || IsRecording) return false;

            try
            {
                _controllerSettings = ScriptableObject.CreateInstance(_controllerSettingsType);
                _controllerSettings.hideFlags = HideFlags.HideAndDontSave;

                Set(_controllerSettings, "FrameRate", (float)fps);
                SetEnum(_controllerSettings, "FrameRatePlayback", "Constant");

                _controllerSettingsType.GetMethod("SetRecordModeToManual", AnyInstance)?.Invoke(_controllerSettings, null);

                _movieSettings = ScriptableObject.CreateInstance(_movieSettingsType);
                _movieSettings.hideFlags = HideFlags.HideAndDontSave;
                _movieSettings.name = "Helpful Editor Game View";

                Set(_movieSettings, "Enabled", true);
                Set(_movieSettings, "OutputFile", pathWithoutExtension);

                object input = Activator.CreateInstance(_gameViewInputType);

                Set(input, "OutputWidth", width);
                Set(input, "OutputHeight", height);
                Set(_movieSettings, "ImageInputSettings", input);

                // The whole reason this path exists.
                object audio = Get(_movieSettings, "AudioInputSettings");
                if (audio != null) Set(audio, "PreserveAudio", true);

                ApplyEncoderQuality(quality);

                _controllerSettingsType.GetMethod("AddRecorderSettings", AnyInstance)
                    ?.Invoke(_controllerSettings, new object[] { _movieSettings });

                // The constructor is looked up rather than left to Activator: passing the settings as a
                // params argument resolved to the no-argument overload, which then went looking for a
                // default constructor RecorderController does not have.
                ConstructorInfo constructor = _controllerType.GetConstructor(new[] { _controllerSettingsType });

                if (constructor == null)
                {
                    Debug.LogError("[HelpfulEditor] Unity Recorder's controller could not be constructed on this version.");
                    Release();

                    return false;
                }

                _controller = constructor.Invoke(new object[] { _controllerSettings });

                _controllerType.GetMethod("PrepareRecording", AnyInstance)?.Invoke(_controller, null);

                if (_controllerType.GetMethod("StartRecording", AnyInstance)?.Invoke(_controller, null) is true) return true;

                Debug.LogError("[HelpfulEditor] Unity Recorder refused to start the recording.");
                Release();

                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[HelpfulEditor] Could not start a Recorder capture: {e.Message}");
                Release();

                return false;
            }
        }

        /// <summary>
        /// Recorder's own quality steps rather than a bit rate. Ultra folds onto High: the encoder
        /// settings expose three named levels and a custom one, and inventing a bit rate for the
        /// fourth would be describing something Recorder is not doing.
        /// </summary>
        private static void ApplyEncoderQuality(RecordingQuality quality)
        {
            if (_coreEncoderType == null) return;

            object encoder = Get(_movieSettings, "EncoderSettings");
            if (encoder == null || !_coreEncoderType.IsInstanceOfType(encoder)) return;

            string level = quality switch
            {
                RecordingQuality.Low => "Low",
                RecordingQuality.Medium => "Medium",
                _ => "High"
            };

            SetEnum(encoder, "EncodingQuality", level);
            Set(_movieSettings, "EncoderSettings", encoder);
        }

        public static void Stop()
        {
            if (_controller == null) return;

            try
            {
                _controllerType.GetMethod("StopRecording", AnyInstance)?.Invoke(_controller, null);
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
            finally
            {
                Release();
            }
        }

        /// <summary>
        /// The settings objects are created here rather than loaded from an asset, so they are ours to
        /// destroy — left alone they would sit in memory as unowned ScriptableObjects for the session.
        /// </summary>
        private static void Release()
        {
            _controller = null;

            if (_movieSettings) UnityEngine.Object.DestroyImmediate(_movieSettings);
            if (_controllerSettings) UnityEngine.Object.DestroyImmediate(_controllerSettings);

            _movieSettings = null;
            _controllerSettings = null;
        }

        private static void Set(object target, string name, object value)
        {
            if (target == null) return;

            PropertyInfo property = target.GetType().GetProperty(name, AnyInstance);

            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value);
                return;
            }

            target.GetType().GetField(name, AnyInstance)?.SetValue(target, value);
        }

        private static object Get(object target, string name)
        {
            if (target == null) return null;

            PropertyInfo property = target.GetType().GetProperty(name, AnyInstance);
            if (property != null) return property.GetValue(target);

            return target.GetType().GetField(name, AnyInstance)?.GetValue(target);
        }

        /// <summary>Set by name rather than by value, so a reordered enum cannot quietly mean something else.</summary>
        private static void SetEnum(object target, string name, string memberName)
        {
            if (target == null) return;

            PropertyInfo property = target.GetType().GetProperty(name, AnyInstance);
            Type enumType = property?.PropertyType ?? target.GetType().GetField(name, AnyInstance)?.FieldType;

            if (enumType == null || !enumType.IsEnum) return;
            if (!Enum.IsDefined(enumType, memberName)) return;

            Set(target, name, Enum.Parse(enumType, memberName));
        }

        private static void WarnOnce(Exception e)
        {
            if (_warned) return;

            _warned = true;
            Debug.LogWarning($"[HelpfulEditor] Talking to Unity Recorder did not go cleanly. ({e.Message})");
        }
    }
}
