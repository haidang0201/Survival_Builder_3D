using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameplaySettingUI : MonoBehaviour
{
    [Header("UI Cấu Hình")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public TMP_Dropdown screenDropdown; 
    public Slider mouseSpeedSlider;

    [Header("Các Nút Chức Năng Gameplay")]
    public Button resumeButton;
    public Button mainMenuButton;
    public Button quitButton;
    public Button saveButton;
    public Button loadButton;

    private void OnEnable()
    {
        if (SettingManager.Ins != null)
        {
            masterVolumeSlider.value = SettingManager.Ins.masterVolume;
            musicVolumeSlider.value = SettingManager.Ins.musicVolume;
            screenDropdown.value = SettingManager.Ins.screenModeIndex;
            mouseSpeedSlider.value = SettingManager.Ins.mouseSpeed;
        }

        masterVolumeSlider.onValueChanged.AddListener(SettingManager.Ins.SetMasterVolume);
        musicVolumeSlider.onValueChanged.AddListener(SettingManager.Ins.SetMusicVolume);
        screenDropdown.onValueChanged.AddListener(SettingManager.Ins.SetScreenMode);
        mouseSpeedSlider.onValueChanged.AddListener(SettingManager.Ins.SetMouseSpeed);

        resumeButton.onClick.AddListener(ResumeGame);
        mainMenuButton.onClick.AddListener(BackToMainMenu);
        quitButton.onClick.AddListener(QuitGame);
        saveButton.onClick.AddListener(SaveGameData);
        loadButton.onClick.AddListener(LoadGameData);

        Time.timeScale = 0f; // Tự động pause game khi mở bảng setting
    }

    private void OnDisable()
    {
        masterVolumeSlider.onValueChanged.RemoveAllListeners();
        musicVolumeSlider.onValueChanged.RemoveAllListeners();
        screenDropdown.onValueChanged.RemoveAllListeners();
        mouseSpeedSlider.onValueChanged.RemoveAllListeners();

        resumeButton.onClick.RemoveAllListeners();
        mainMenuButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();
        saveButton.onClick.RemoveAllListeners();
        loadButton.onClick.RemoveAllListeners();

        PlayerPrefs.Save();
        Time.timeScale = 1f; // Chạy lại game bình thường khi đóng bảng
    }

    public void ResumeGame()
    {
        gameObject.SetActive(false);
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuScene"); // Đổi tên đúng theo Scene Menu của bạn
    }

    public void QuitGame()
    {
        Debug.Log("Thoát game!");
        Application.Quit();
    }

    public void SaveGameData()
    {
        // Gọi lưu công trình và tài nguyên vào Slot 1 qua BuildingSystem
        if (BuildingSystem.Ins != null)
        {
            BuildingSystem.Ins.SaveBuildingsToSlot(1);
            Debug.Log("[GameplaySettingUI] ✅ Đã lưu dữ liệu màn chơi JSON vào Slot 1 thành công!");
        }
    }

    public void LoadGameData()
    {
        // 1. Reset bộ đếm số lượng nhà hiện tại về 0 để chuẩn bị tính toán lại từ map được tải
        if (ConstructionManager.Ins != null)
        {
            ConstructionManager.Ins.ResetBuildingCounts();
        }

        // 2. Gọi hàm load Slot 1 từ BuildingSystem
        if (BuildingSystem.Ins != null)
        {
            BuildingSystem.Ins.LoadBuildingsFromSlot(1);

            // 3. Sau khi tải map thành công, ép các UI Text hiển thị giá tiền công trình cập nhật lại
            if (ConstructionManager.Ins != null)
            {
                ConstructionManager.Ins.UpdateAllCostUI();
            }
            
            Debug.Log("[GameplaySettingUI] ✅ Đã tải lại tiến trình chơi JSON từ Slot 1 thành công!");
            gameObject.SetActive(false); // Đóng bảng setting
        }
    }
}