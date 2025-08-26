using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class AudioSettingsUI : MonoBehaviour
{

    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeSliderAmount;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxVolumeSliderAmount;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeSliderAmount;
    
    
    [Header("Buttons")]
    [SerializeField] private Button buttonOptionsBack;
    [SerializeField] private Button buttonOptionsReset;
    
    
    protected void Start()
    {
        SetupButtons();
        SetupVolumeSliders();
    }
    

    private void SetupButtons()
    {
        if (buttonOptionsReset)
        {
            buttonOptionsReset.onClick.AddListener(() => {
                SettingsManager.ResetSettingsToDefault();
                SetupVolumeSliders();
            });
        }
    }

   
    private void SetupVolumeSliders()
    {
        if (masterVolumeSlider)
        {
            if (masterVolumeSliderAmount) masterVolumeSliderAmount.text = (SettingsManager.MasterGameVolume * 100).ToString("F0") + "%";
            masterVolumeSlider.value = SettingsManager.MasterGameVolume;
            masterVolumeSlider.onValueChanged.AddListener(value =>
            {
                SettingsManager.SetMasterVolume(value);
                if (masterVolumeSliderAmount) masterVolumeSliderAmount.text = (value * 100).ToString("F0") + "%";
            });
        }
        
        if (sfxVolumeSlider)
        {
            if (sfxVolumeSliderAmount) sfxVolumeSliderAmount.text = (SettingsManager.SoundFXVolume * 100).ToString("F0") + "%";
            sfxVolumeSlider.value = SettingsManager.SoundFXVolume;
            sfxVolumeSlider.onValueChanged.AddListener(value =>
            {
                SettingsManager.SetSoundFXVolume(value);
                if (sfxVolumeSliderAmount) sfxVolumeSliderAmount.text = (value * 100).ToString("F0") + "%";
            });
        }

        if (musicVolumeSlider)
        {
            if (musicVolumeSliderAmount) musicVolumeSliderAmount.text = (SettingsManager.MusicVolume * 100).ToString("F0") + "%";
            musicVolumeSlider.value = SettingsManager.MusicVolume;
            musicVolumeSlider.onValueChanged.AddListener(value =>
            {
                SettingsManager.SetMusicVolume(value);
                if (musicVolumeSliderAmount) musicVolumeSliderAmount.text = (value * 100).ToString("F0") + "%";
            });
            
        }
        
    }
}