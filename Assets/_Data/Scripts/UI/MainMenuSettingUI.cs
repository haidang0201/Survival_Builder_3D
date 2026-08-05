using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuSettingUI : MonoBehaviour
{
    [Header("UI Components")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public TMP_Dropdown screenDropdown;
    public Slider mouseSpeedSlider;

    private void OnEnable()
    {
        if (SettingManager.HasInstance && SettingManager.Ins != null)
        {
            masterVolumeSlider.value = SettingManager.Ins.masterVolume;
            musicVolumeSlider.value = SettingManager.Ins.musicVolume;
            screenDropdown.value = SettingManager.Ins.screenModeIndex;
            mouseSpeedSlider.value = SettingManager.Ins.mouseSpeed;

            masterVolumeSlider.onValueChanged.AddListener(SettingManager.Ins.SetMasterVolume);
            musicVolumeSlider.onValueChanged.AddListener(SettingManager.Ins.SetMusicVolume);
            screenDropdown.onValueChanged.AddListener(SettingManager.Ins.SetScreenMode);
            mouseSpeedSlider.onValueChanged.AddListener(SettingManager.Ins.SetMouseSpeed);
        }
    }

    private void OnDisable()
    {
        masterVolumeSlider.onValueChanged.RemoveAllListeners();
        musicVolumeSlider.onValueChanged.RemoveAllListeners();
        screenDropdown.onValueChanged.RemoveAllListeners();
        mouseSpeedSlider.onValueChanged.RemoveAllListeners();
        
        PlayerPrefs.Save();
    }
}