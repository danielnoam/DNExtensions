using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DNExtensions;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // For URP Motion blur
// using UnityEngine.Rendering.HighDefinition; // For HDRP Motion blur



public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    
    public static Resolution[] AvailableResolutions;
    private static Volume _postProcessingVolume;
    private static AudioMixer _audioMixer;
    private const float AspectRatioTolerance = 0.01f;
    private static readonly Dictionary<AspectRatioMode, float> AspectRatios = new()
    {
        { AspectRatioMode.AspectRatioAny, 0f },
        { AspectRatioMode.AspectRatio16By9, 16f / 9f },
        { AspectRatioMode.AspectRatio16By10, 16f / 10f },
        { AspectRatioMode.AspectRatio4By3, 4f / 3f },
        { AspectRatioMode.AspectRatio21By9, 21f / 9f }
    };


    [Header("Setup")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Volume postProcessingVolume;
    public const float MinMotionBlurIntensity = 0f;
    public const float MaxMotionBlurIntensity = 1f;
    public const float MinGammaLevel = -1f;
    public const float MaxGammaLevel = 1f;
    public const int MinFPS = 30;
    public const int MaxFPS = 999;
    public enum ScreenModes
    {
        FullScreen = 0,
        Borderless = 1,
        Windowed = 2
    }
    public enum AspectRatioMode
    {
        AspectRatioAny,
        AspectRatio16By9,
        AspectRatio16By10,
        AspectRatio4By3,
        AspectRatio21By9
    }
    
    
    [Header("Current Settings")] 
    public static int TargetFPS;
    public static int VSync;
    public static ScreenModes ScreenMode;
    public static AspectRatioMode CurrentAspectRatio;
    public static int ResolutionIndex;
    public static bool MotionBlurEnabled;
    public static float MotionBlurIntensity;
    public static float GammaLevel;
    public static float MasterGameVolume;
    public static float SoundFXVolume;
    public static float MusicVolume;
    
    
    [Header("Default Settings")]
    private const int DefaultTargetFPS = 999;
    private const int DefaultVSync = 0;
    private const ScreenModes DefaultScreenMode = ScreenModes.Borderless;
    private static int DefaultResolutionIndex => AvailableResolutions.Length - 1;
    private const float DefaultMasterGameVolume = 1f;
    private const float DefaultSoundFXVolume = 0.7f;
    private const float DefaultMusicVolume = 0.5f;
    private const AspectRatioMode DefaultAspectRatio = AspectRatioMode.AspectRatioAny;
    private const bool DefaultMotionBlurEnabled = false;
    private const float DefaultMotionBlurIntensity = 0.5f;
    private const float DefaultGammaLevel = 0f;
    

    public static event Action<int> OnResolutionChange;
    public static event Action<ScreenModes> OnScreenModeChange;
    public static event Action<AspectRatioMode> OnAspectRatioChange;
    public static event Action<int> OnVSyncChange;
    public static event Action<int> OnTargetFPSChange;
    public static event Action<float> OnSoundFXVolumeChange;
    public static event Action<float> OnMusicVolumeChange;
    public static event Action<float> OnMasterVolumeChange;
    public static event Action<bool> OnMotionBlurEnabledChange;
    public static event Action<float> OnMotionBlurIntensityChange;
    public static event Action<float> OnGammaLevelChange;
    



    
    private void Awake()
    {
        if (Instance && Instance != this) 
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _postProcessingVolume = postProcessingVolume;
        if (!_postProcessingVolume)
        {
            _postProcessingVolume = FindAnyObjectByType<Volume>();
            if (!_postProcessingVolume)
            {
                Debug.LogWarning("No Post Processing Volume found in the scene!");
            }
        }

        _audioMixer = audioMixer;
        if (!_audioMixer)
        {
            Debug.LogWarning("No Audio Mixer was assigned!");
        }
        
        UpdateAvailableResolutions();
        LoadAllSettings();
        LoadAudioSettings();
    }
    


#region Setup // ----------------------------------------------------------------------------------------------------

    public static void LoadAllSettings()
    {
        LoadAudioSettings();
        LoadVideoSettings();
    }
    
    private static void LoadVideoSettings()
    {
        SetAspectRatio((AspectRatioMode)PlayerPerfsManager.LoadInt("AspectRatio", (int)DefaultAspectRatio));
        SetResolution(PlayerPerfsManager.LoadInt("ResolutionIndex", DefaultResolutionIndex));
        SetScreenMode((ScreenModes)PlayerPerfsManager.LoadInt("screenMode", (int)DefaultScreenMode));
        SetVSync(PlayerPerfsManager.LoadInt("VSync", DefaultVSync));
        SetFPS(PlayerPerfsManager.LoadInt("FPS", DefaultTargetFPS));
        SetMotionBlurEnabled(PlayerPerfsManager.LoadBool("MotionBlurEnabled", DefaultMotionBlurEnabled));
        SetMotionBlurIntensity(PlayerPerfsManager.LoadFloat("MotionBlurIntensity", DefaultMotionBlurIntensity));
        SetGammaLevel(PlayerPerfsManager.LoadFloat("GammaLevel", DefaultGammaLevel));
    }
    
    private static void LoadAudioSettings()
    {
        SetMasterVolume(PlayerPerfsManager.LoadFloat("MasterVolume", DefaultMasterGameVolume));
        SetSoundFXVolume(PlayerPerfsManager.LoadFloat("GameVolume", DefaultSoundFXVolume));
        SetMusicVolume(PlayerPerfsManager.LoadFloat("MusicVolume", DefaultMusicVolume));
    }

    public static void ResetSettingsToDefault()
    {
        SetAspectRatio(DefaultAspectRatio);
        SetResolution(DefaultResolutionIndex);
        SetScreenMode(DefaultScreenMode);
        SetVSync(DefaultVSync);
        SetFPS(DefaultTargetFPS);
        SetMasterVolume(DefaultMasterGameVolume);
        SetSoundFXVolume(DefaultSoundFXVolume);
        SetMusicVolume(DefaultMusicVolume);
        SetMotionBlurEnabled(DefaultMotionBlurEnabled);
        SetMotionBlurIntensity(DefaultMotionBlurIntensity);
        SetGammaLevel(DefaultGammaLevel);
    }
    

#endregion Setup // ----------------------------------------------------------------------------------------------------


#region Video Settings // ----------------------------------------------------------------------------------------------------

      public static void SetScreenMode(ScreenModes modes)
    {
        FullScreenMode unityScreenMode;
        Resolution highestResolution = AvailableResolutions[^1];
    
        switch (modes)
        {
            case ScreenModes.FullScreen:
                unityScreenMode = FullScreenMode.ExclusiveFullScreen;
                UnityEngine.Screen.SetResolution(highestResolution.width, highestResolution.height, unityScreenMode);
                break;
            case ScreenModes.Borderless:
                unityScreenMode = FullScreenMode.FullScreenWindow;
                UnityEngine.Screen.SetResolution(highestResolution.width, highestResolution.height, unityScreenMode);
                break;
            case ScreenModes.Windowed:
                unityScreenMode = FullScreenMode.Windowed;
                Resolution currentResolution = UnityEngine.Screen.currentResolution;
                UnityEngine.Screen.SetResolution(currentResolution.width, currentResolution.height, unityScreenMode);
                break;
        }

        ScreenMode = modes;
        OnScreenModeChange?.Invoke(modes);
        PlayerPerfsManager.SaveInt("ScreenModes", (int)modes);
    }

    public static void SetResolution(int index)
    {
        if (index >= 0 && index < AvailableResolutions.Length)
        {
            Resolution resolution = AvailableResolutions[index];
            ResolutionIndex = index;
            UnityEngine.Screen.SetResolution(resolution.width, resolution.height, UnityEngine.Screen.fullScreenMode);
            OnResolutionChange?.Invoke(index);
            PlayerPerfsManager.SaveInt("ResolutionIndex", index);
        }
    }
    
    
    private static void UpdateAvailableResolutions()
    {
        // Find all available resolutions
        Resolution[] allResolutions = UnityEngine.Screen.resolutions;
    
        // Filter by a chosen aspect ratio
        IEnumerable<Resolution> filteredResolutions;
        if (CurrentAspectRatio == AspectRatioMode.AspectRatioAny)
        {
            filteredResolutions = allResolutions;
        }
        else 
        {
            float targetAspect = AspectRatios[CurrentAspectRatio];
            filteredResolutions = allResolutions.Where(r => 
            {
                float aspect = (float)r.width / r.height;
                return Mathf.Abs(aspect - targetAspect) < AspectRatioTolerance;
            });
        }

        // Organize resolutions
        AvailableResolutions = filteredResolutions
            .GroupBy(r => new { r.width, r.height })
            .Select(g => g.First())
            .OrderBy(r => r.width)
            .ThenBy(r => r.height)
            .ToArray();
    }
    
    public static bool IsResolutionValidForCurrentAspectRatio(Resolution resolution)
    {
        if (CurrentAspectRatio == AspectRatioMode.AspectRatioAny)
            return true;

        float aspect = (float)resolution.width / resolution.height;
        float targetAspect = AspectRatios[CurrentAspectRatio];
        return Mathf.Abs(aspect - targetAspect) < AspectRatioTolerance;
    }
    
    public static void SetAspectRatio(AspectRatioMode mode)
    {
        if (CurrentAspectRatio != mode)
        {
            CurrentAspectRatio = mode;
            UpdateAvailableResolutions();
            
            // Reset resolution to the highest available if the current resolution is not valid
            if (ResolutionIndex >= AvailableResolutions.Length)
            {
                SetResolution(AvailableResolutions.Length - 1);
            }
            
            OnAspectRatioChange?.Invoke(mode);
            PlayerPerfsManager.SaveInt("AspectRatio", (int)mode);
        }
    }
    

    public static void SetVSync(int vSyncCount)
    {
        VSync = vSyncCount;
        QualitySettings.vSyncCount = VSync;
        OnVSyncChange?.Invoke(vSyncCount);
        PlayerPerfsManager.SaveInt("VSync", VSync);
    }

    public static void SetFPS(int fps)
    {
        TargetFPS = fps;
        Application.targetFrameRate = TargetFPS;
        OnTargetFPSChange?.Invoke(fps);
        PlayerPerfsManager.SaveInt("FPS", fps);
    }
    

    public static void SetMotionBlurEnabled(bool enabled)
    {
        MotionBlurEnabled = enabled;
        
        if (_postProcessingVolume != null && 
            _postProcessingVolume.profile.TryGet(out MotionBlur motionBlur))
        {
            motionBlur.active = enabled;
        }
        
        OnMotionBlurEnabledChange?.Invoke(enabled);
        PlayerPerfsManager.SaveBool("MotionBlurEnabled", enabled);
    }

    public static void SetMotionBlurIntensity(float intensity)
    {
        // Clamp the intensity value
        intensity = Mathf.Clamp(intensity, MinMotionBlurIntensity, MaxMotionBlurIntensity);
        MotionBlurIntensity = intensity;
        
        if (_postProcessingVolume != null && 
            _postProcessingVolume.profile.TryGet(out MotionBlur motionBlur))
        {
            motionBlur.intensity.value = intensity;
        }
        
        OnMotionBlurIntensityChange?.Invoke(intensity);
        PlayerPerfsManager.SaveFloat("MotionBlurIntensity", intensity);
    }
    
    public static bool IsMotionBlurSupported()
    {
        return _postProcessingVolume != null && 
               _postProcessingVolume.profile.TryGet(out MotionBlur _);
    }
    
    public static void SetGammaLevel(float level)
    {
        // Clamp the level value
        level = Mathf.Clamp(level, MinGammaLevel, MaxGammaLevel);
        GammaLevel = level;

        if (_postProcessingVolume != null &&
            _postProcessingVolume.profile.TryGet(out LiftGammaGain liftGammaGain))
        {
            liftGammaGain.gamma.value = new Vector4(1, 1, 1, level);
        }

        OnGammaLevelChange?.Invoke(level);
        PlayerPerfsManager.SaveFloat("GammaLevel", level);
    }
    
    public static bool IsGammaLevelSupported()
    {
        return _postProcessingVolume != null &&
               _postProcessingVolume.profile.TryGet(out LiftGammaGain _);
    }
    
    
    
#endregion Video Settings // ----------------------------------------------------------------------------------------------------
  

#region Audio Settings // ----------------------------------------------------------------------------------------------------

    public static void SetMasterVolume(float volume)
    {
        // Convert slider value (usually 0 to 1) to decibels
        float dB = Mathf.Log10(volume) * 20;
        // Protect against - infinity when volume is 0
        if (volume == 0) { dB = -80f; }
        
        
        MasterGameVolume = volume;
        if (_audioMixer) _audioMixer.SetFloat("masterVolume", dB);
        PlayerPerfsManager.SaveFloat("MasterVolume", MasterGameVolume);
        OnMasterVolumeChange?.Invoke(volume);
    }
        
    public static void SetSoundFXVolume(float volume)
    {
        // Convert slider value (usually 0 to 1) to decibels
        float dB = Mathf.Log10(volume) * 20;
        // Protect against - infinity when volume is 0
        if (volume == 0) { dB = -80f; }
        
        
        SoundFXVolume = volume;
        if (_audioMixer) _audioMixer.SetFloat("soundFXVolume", dB);
        PlayerPerfsManager.SaveFloat("GameVolume", SoundFXVolume);
        OnSoundFXVolumeChange?.Invoke(volume);
    }

    public static void SetMusicVolume(float volume)
    {
        // Convert slider value (usually 0 to 1) to decibels
        float dB = Mathf.Log10(volume) * 20;
        // Protect against - infinity when volume is 0
        if (volume == 0) { dB = -80f; }
        
        
        MusicVolume = volume;
        if (_audioMixer) _audioMixer.SetFloat("musicVolume", dB);
        PlayerPerfsManager.SaveFloat("MusicVolume", MusicVolume);
        OnMusicVolumeChange?.Invoke(volume);
    }
    

#endregion Audio Settings // ----------------------------------------------------------------------------------------------------


}