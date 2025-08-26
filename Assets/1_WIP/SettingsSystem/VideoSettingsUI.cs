using TMPro;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System.Collections.Generic;

public class VideoSettingsUI : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private TMP_Dropdown vSyncDropdown;
    [SerializeField] private TMP_Dropdown screenModeDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown aspectRatioDropdown;
    [SerializeField] private Slider fpsSlider;
    [SerializeField] private TextMeshProUGUI fpsSliderAmount;
    
    
    [Header("Post Processing")]
    [SerializeField] private Toggle motionBlurToggle;
    [SerializeField] private Slider motionBlurSlider;
    [SerializeField] private TextMeshProUGUI motionBlurSliderAmount;
    [SerializeField] private Slider gammaSlider;
    [SerializeField] private TextMeshProUGUI gammaSliderAmount;
    
    [Header("Buttons")]
    [SerializeField] private Button buttonOptionsBack;
    [SerializeField] private Button buttonOptionsReset;
    
    protected void Awake()
    {
        SetupButtons();
        SetupResolutionDropdown();
        SetupScreenModeDropdown();
        SetupVSyncDropDown();
        SetupFPSSliderLogarithmic();
        SetupAspectRatioDropdown();
        SetupMotionBlurOptions();
        SetupGammaSlider();
    }
    

    private void SetupButtons()
    {

        if (buttonOptionsReset)
        {
            buttonOptionsReset.onClick.AddListener(() => {
                SettingsManager.ResetSettingsToDefault();
                SetupAspectRatioDropdown();
                SetupResolutionDropdown();
                SetupScreenModeDropdown();
                SetupVSyncDropDown();
                SetupFPSSliderLogarithmic();
                SetupMotionBlurOptions();
                SetupGammaSlider();
            });
        }
    }

    private void SetupVSyncDropDown()
    {
        if (!vSyncDropdown) return;
    
        vSyncDropdown.ClearOptions();
        vSyncDropdown.AddOptions(new List<string> { 
            "Disabled", 
            "Every V-Blank", 
            "Every Second V-Blank"
        });
    
        vSyncDropdown.value = SettingsManager.VSync;
        vSyncDropdown.onValueChanged.AddListener(SettingsManager.SetVSync);
    }
    
    private void SetupScreenModeDropdown()
    {
        if (!screenModeDropdown) return;
        
        screenModeDropdown.ClearOptions();
        List<string> options = System.Enum.GetNames(typeof(SettingsManager.ScreenModes)).ToList();
        screenModeDropdown.AddOptions(options);
        screenModeDropdown.value = (int)SettingsManager.ScreenMode;
        screenModeDropdown.onValueChanged.AddListener((value) => { SettingsManager.SetScreenMode((SettingsManager.ScreenModes)value); });
    }

    private void SetupResolutionDropdown()
    {
        if (!resolutionDropdown) return;
        UpdateResolutionDropdown();
    }

    private void UpdateResolutionDropdown()
    {
        if (!resolutionDropdown) return;

        // Get the filtered resolutions from SettingsManager
        Resolution[] availableResolutions = SettingsManager.AvailableResolutions;

        resolutionDropdown.ClearOptions();
        List<string> options = availableResolutions
            .Select(res => $"{res.width} x {res.height}")
            .ToList();
        
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = SettingsManager.ResolutionIndex;
        resolutionDropdown.onValueChanged.AddListener(SettingsManager.SetResolution);
    }

    private void SetupFPSSlider()
    {
        if (!fpsSlider) return;
        if (fpsSliderAmount) fpsSliderAmount.text = SettingsManager.TargetFPS.ToString();
        
        fpsSlider.value = (SettingsManager.TargetFPS - SettingsManager.MinFPS) / (SettingsManager.MaxFPS - SettingsManager.MinFPS); // Convert FPS to 0-1 range
        fpsSlider.onValueChanged.AddListener((value) =>
        {
            float fps = 30f + (value * (999f - 30f));
            SettingsManager.SetFPS((int)fps);
            if (fpsSliderAmount) fpsSliderAmount.text = SettingsManager.TargetFPS.ToString();
        });
    }
    
    private void SetupFPSSliderPresets()
    {
        if (!fpsSlider) return;
        if (fpsSliderAmount) fpsSliderAmount.text = SettingsManager.TargetFPS.ToString();
    
        // Common FPS presets
        int[] fpsPresets = { 30, 60, 75, 90, 120, 144, 165, 240, 360 };
    
        // Find the closest preset to current FPS
        int currentFps = SettingsManager.TargetFPS;
        int presetIndex = 0;
        float minDiff = Mathf.Abs(fpsPresets[0] - currentFps);
    
        for (int i = 1; i < fpsPresets.Length; i++)
        {
            float diff = Mathf.Abs(fpsPresets[i] - currentFps);
            if (diff < minDiff)
            {
                minDiff = diff;
                presetIndex = i;
            }
        }
    
        fpsSlider.value = (float)presetIndex / (fpsPresets.Length - 1);
        
        fpsSlider.onValueChanged.AddListener((value) =>
        {
            int index = Mathf.RoundToInt(value * (fpsPresets.Length - 1));
            SettingsManager.SetFPS(fpsPresets[index]);
            if (fpsSliderAmount) fpsSliderAmount.text = SettingsManager.TargetFPS.ToString();
        });
    }
    
    private void SetupFPSSliderLogarithmic ()
    {
        if (!fpsSlider) return;
        if (fpsSliderAmount) fpsSliderAmount.text = SettingsManager.TargetFPS.ToString();
    
        // Convert current FPS to logarithmic scale for slider position
        float currentFps = SettingsManager.TargetFPS;
        float minFps = SettingsManager.MinFPS;
        float maxFps = SettingsManager.MaxFPS;
    
        float logMin = Mathf.Log10(minFps);
        float logMax = Mathf.Log10(maxFps);
        float logCurrent = Mathf.Log10(currentFps);
    
        fpsSlider.value = (logCurrent - logMin) / (logMax - logMin);
        
        fpsSlider.onValueChanged.AddListener((value) =>
        {
            // Convert slider value back from logarithmic scale to FPS
            float logValue = logMin + (value * (logMax - logMin));
            int fps = Mathf.RoundToInt(Mathf.Pow(10, logValue));
            SettingsManager.SetFPS(fps);
            if (fpsSliderAmount) fpsSliderAmount.text = SettingsManager.TargetFPS.ToString();
        });
    }
    
    
    private void SetupAspectRatioDropdown()
    {
        if (!aspectRatioDropdown) return;

        aspectRatioDropdown.ClearOptions();
        
        // Create a dictionary to map enum values to friendly names
        Dictionary<SettingsManager.AspectRatioMode, string> aspectRatioNames = new()
        {
            { SettingsManager.AspectRatioMode.AspectRatioAny, "Any" },
            { SettingsManager.AspectRatioMode.AspectRatio16By9, "16:9" },
            { SettingsManager.AspectRatioMode.AspectRatio16By10, "16:10" },
            { SettingsManager.AspectRatioMode.AspectRatio4By3, "4:3" },
            { SettingsManager.AspectRatioMode.AspectRatio21By9, "21:9" }
        };

        // Add the options in the order of the enum
        var options = System.Enum.GetValues(typeof(SettingsManager.AspectRatioMode))
            .Cast<SettingsManager.AspectRatioMode>()
            .Select(mode => aspectRatioNames[mode])
            .ToList();

        aspectRatioDropdown.AddOptions(options);
        aspectRatioDropdown.value = (int)SettingsManager.CurrentAspectRatio;
        
        aspectRatioDropdown.onValueChanged.AddListener((value) => 
        {
            SettingsManager.SetAspectRatio((SettingsManager.AspectRatioMode)value);
            UpdateResolutionDropdown();
        });
    }
    
    private void SetupMotionBlurOptions()
    {
        if (motionBlurToggle)
        {
            motionBlurToggle.gameObject.SetActive(SettingsManager.IsMotionBlurSupported());
            motionBlurToggle.isOn = SettingsManager.MotionBlurEnabled;
            
            motionBlurToggle.onValueChanged.AddListener(SettingsManager.SetMotionBlurEnabled);
            motionBlurToggle.onValueChanged.AddListener((value) => motionBlurSlider.interactable = value);
        }

        if (motionBlurSlider)
        {
            motionBlurToggle.gameObject.SetActive(SettingsManager.IsMotionBlurSupported());
            motionBlurSlider.interactable = motionBlurToggle.isOn;
            if (motionBlurSliderAmount) motionBlurSliderAmount.text = SettingsManager.MotionBlurIntensity.ToString();
            motionBlurSlider.value = (SettingsManager.MaxMotionBlurIntensity - SettingsManager.MinMotionBlurIntensity) / (SettingsManager.MaxMotionBlurIntensity - SettingsManager.MinMotionBlurIntensity);
            
            motionBlurSlider.onValueChanged.AddListener((value) =>
            {
                SettingsManager.SetMotionBlurIntensity(value);
                if (motionBlurSliderAmount) motionBlurSliderAmount.text = $"{SettingsManager.MotionBlurIntensity:F}";
            });
        }
    }
    
    private void SetupGammaSlider()
    {
        if (!gammaSlider) return;
        if (gammaSliderAmount) gammaSliderAmount.text = SettingsManager.GammaLevel.ToString();

        gammaSlider.value = (SettingsManager.GammaLevel - SettingsManager.MinGammaLevel) / (SettingsManager.MaxGammaLevel - SettingsManager.MinGammaLevel); // Convert FPS to 0-1 range
        gammaSlider.onValueChanged.AddListener((value) =>
        {
            float gamma = SettingsManager.MinGammaLevel + (value * (SettingsManager.MaxGammaLevel - SettingsManager.MinGammaLevel));
            SettingsManager.SetGammaLevel(gamma);
            if (gammaSliderAmount) gammaSliderAmount.text = $"{SettingsManager.GammaLevel:F}";
        });
    }
    
    
    
}