using UnityEngine;
using UnityEngine.UI;
using TMPro; // Đảm bảo có thư viện này để dùng TMP_Dropdown

public class MainMenuSettingUI : MonoBehaviour
{
    [Header("UI Components")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public TMP_Dropdown screenDropdown; // ĐỔI THÀNH DROPDOWN (Yêu cầu thiết lập 3 Option trong Inspector theo thứ tự: Window, Fullscreen, 1920:1080)
    public Slider mouseSpeedSlider;

    private void OnEnable()
    {
        if (SettingManager.Ins != null)
        {
            masterVolumeSlider.value = SettingManager.Ins.masterVolume;
            musicVolumeSlider.value = SettingManager.Ins.musicVolume;
            screenDropdown.value = SettingManager.Ins.screenModeIndex; // Đồng bộ index sang Dropdown
            mouseSpeedSlider.value = SettingManager.Ins.mouseSpeed;
        }

        masterVolumeSlider.onValueChanged.AddListener(SettingManager.Ins.SetMasterVolume);
        musicVolumeSlider.onValueChanged.AddListener(SettingManager.Ins.SetMusicVolume);
        screenDropdown.onValueChanged.AddListener(SettingManager.Ins.SetScreenMode); // Lắng nghe sự kiện Dropdown thay đổi
        mouseSpeedSlider.onValueChanged.AddListener(SettingManager.Ins.SetMouseSpeed);
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