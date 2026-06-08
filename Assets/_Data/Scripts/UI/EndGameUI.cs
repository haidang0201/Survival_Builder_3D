using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/*
 * EndGameUI.cs
 * Folder: Scripts/UI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Tích hợp bổ sung: Tính năng CloseWindow đóng panel để quan sát map sau trận.
 */

public class EndGameUI : MonoBehaviour
{
    public static EndGameUI Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject endGamePanelContainer;

    [Header("Thống kê Thành tích")]
    [SerializeField] private TMP_Text survivalDaysText;
    [SerializeField] private TMP_Text totalBuildingsText;

    [Header("Chi tiết Tài nguyên đã khai thác")]
    [SerializeField] private TMP_Text totalWoodText;
    [SerializeField] private TMP_Text totalStoneText;
    [SerializeField] private TMP_Text totalFoodText;
    [SerializeField] private TMP_Text totalGoldText;

    [Header("Hệ thống Điều hướng & Đóng Windows")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button closeWindowButton; // <-- Nút tắt cửa sổ tổng kết

    [Header("Cấu hình Scene")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (endGamePanelContainer != null) endGamePanelContainer.SetActive(false);

        // Đăng ký sự kiện nút bấm
        if (restartButton != null) restartButton.onClick.AddListener(OnClickRestart);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnClickMainMenu);
        if (closeWindowButton != null) closeWindowButton.onClick.AddListener(CloseEndGameWindow);
    }

    public void TriggerEndGame(int finalDaysSurvived)
    {
        JsonDataManager.RegisterStat_DaysSurvived(finalDaysSurvived);

        if (endGamePanelContainer == null) return;

        Time.timeScale = 0f; // Dừng thời gian game
        endGamePanelContainer.SetActive(true);
        DisplayStats();
    }

    private void DisplayStats()
    {
        if (survivalDaysText != null)
            survivalDaysText.text = PlayerPrefs.GetInt("Stat_Survival_Days", 0).ToString() + " Ngày";

        if (totalBuildingsText != null)
            totalBuildingsText.text = PlayerPrefs.GetInt("Stat_Total_Buildings", 0).ToString();

        if (totalWoodText != null)
            totalWoodText.text = PlayerPrefs.GetInt("Stat_Total_Wood", 0).ToString();

        if (totalStoneText != null)
            totalStoneText.text = PlayerPrefs.GetInt("Stat_Total_Stone", 0).ToString();

        if (totalFoodText != null)
            totalFoodText.text = PlayerPrefs.GetInt("Stat_Total_Food", 0).ToString();

        if (totalGoldText != null)
            totalGoldText.text = PlayerPrefs.GetInt("Stat_Total_Gold", 0).ToString();
    }

    /// <summary>
    /// Hàm CloseWindow: Tắt bảng tổng kết để người chơi ngắm nhìn lại map chiến tích
    /// </summary>
    public void CloseEndGameWindow()
    {
        if (endGamePanelContainer != null)
        {
            endGamePanelContainer.SetActive(false);
            Debug.Log("[EndGameUI] ❌ Đã đóng bảng tổng kết. Người chơi đang ở chế độ xem Map.");
            
            // Tùy chọn: Bạn có thể cho phép thời gian chạy tiếp hoặc giữ nguyên đóng băng (Scale = 0)
            // Time.timeScale = 1f; 
        }
    }

    // ================= XỬ LÝ SỰ KIỆN ĐIỀU HƯỚNG =================

    private void OnClickRestart()
    {
        Time.timeScale = 1f;
        JsonDataManager.ResetEndGameStats();
        SceneManager.LoadScene(gameplaySceneName);
    }

    private void OnClickMainMenu()
    {
        Time.timeScale = 1f;
        JsonDataManager.ResetEndGameStats();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}