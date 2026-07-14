using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/*
 * EndGameUI.cs
 * Folder: Scripts/UI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Tính năng: 
 * 1. Đọc chỉ số End Game trực tiếp từ JsonDataManager (JSON).
 * 2. Hiệu ứng chữ số chạy tăng dần (Count-up) sinh động.
 */

public class EndGameUI : MonoBehaviour
{
    public static EndGameUI Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject endGamePanelContainer;

    [Header("Thống kê Thành tích")]
    [SerializeField] private TMP_Text survivalDaysText;
    [SerializeField] private TMP_Text totalBuildingsText;
    [SerializeField] private TMP_Text rankEvaluationText; 

    [Header("Chi tiết Tài nguyên đã khai thác")]
    [SerializeField] private TMP_Text totalWoodText;
    [SerializeField] private TMP_Text totalStoneText;
    [SerializeField] private TMP_Text totalFoodText;
    [SerializeField] private TMP_Text totalGoldText;

    [Header("Hệ thống Điều hướng & Đóng Windows")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button closeWindowButton;

    [Header("Cấu hình Tốc độ Hiệu ứng")]
    [Tooltip("Thời gian chạy hiệu ứng đếm số (tính bằng giây)")]
    [SerializeField] private float countDuration = 1.5f;

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

        if (restartButton != null) restartButton.onClick.AddListener(OnClickRestart);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnClickMainMenu);
        if (closeWindowButton != null) closeWindowButton.onClick.AddListener(CloseEndGameWindow);
    }

    public void TriggerEndGame(int finalDaysSurvived)
    {
        if (endGamePanelContainer == null) return;

        // 1. Lưu số ngày sống sót vào file JSON
        JsonDataManager.RegisterStat_DaysSurvived(finalDaysSurvived);

        // 2. ĐỒNG BỘ NGAY LẬP TỨC tài nguyên từ HUD vào file JSON Endgame
        JsonDataManager.SaveFinalSessionStats();

        // Dừng thời gian thế giới 3D phía sau để tập trung xem UI
        Time.timeScale = 0f; 
        endGamePanelContainer.SetActive(true);

        // Kích hoạt chuỗi hiệu ứng đếm số và đánh giá danh hiệu
        StartCoroutine(AnimateEndGameUI(finalDaysSurvived));
    }

    /// <summary>
    /// Coroutine xử lý chạy số tăng dần từ file JSON của JsonDataManager
    /// </summary>
    private IEnumerator AnimateEndGameUI(int targetDays)
    {
        // 1. Đọc dữ liệu từ file JSON Endgame thông qua JsonDataManager
        JsonDataManager.EndGameStats stats = JsonDataManager.LoadEndGameStats();

        int targetBuildings = stats.totalBuildings;
        int targetWood = stats.totalWood;
        int targetStone = stats.totalStone;
        int targetFood = stats.totalFood;
        int targetGold = stats.totalGold;

        // Đặt toàn bộ văn bản về 0 trước khi chạy hiệu ứng
        SetTextValue(survivalDaysText, 0, " Ngày");
        SetTextValue(totalBuildingsText, 0);
        SetTextValue(totalWoodText, 0);
        SetTextValue(totalStoneText, 0);
        SetTextValue(totalFoodText, 0);
        SetTextValue(totalGoldText, 0);
        if (rankEvaluationText != null) rankEvaluationText.text = "...";

        // 2. Chạy hiệu ứng tăng số theo thời gian thực (Bỏ qua Time.timeScale = 0 bằng cách dùng UnscaledTime)
        float elapsed = 0f;
        while (elapsed < countDuration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float progress = Mathf.Clamp01(elapsed / countDuration);

            // Nội suy giá trị từ 0 đến đích dựa trên tiến độ
            SetTextValue(survivalDaysText, Mathf.FloorToInt(targetDays * progress), " Ngày");
            SetTextValue(totalBuildingsText, Mathf.FloorToInt(targetBuildings * progress));
            SetTextValue(totalWoodText, Mathf.FloorToInt(targetWood * progress));
            SetTextValue(stoneTextValid(totalStoneText) ? totalStoneText : null, Mathf.FloorToInt(targetStone * progress));
            SetTextValue(totalFoodText, Mathf.FloorToInt(targetFood * progress));
            SetTextValue(totalGoldText, Mathf.FloorToInt(targetGold * progress));

            yield return null; 
        }

        // 3. Đảm bảo gán chính xác giá trị đích cuối cùng
        SetTextValue(survivalDaysText, targetDays, " Ngày");
        SetTextValue(totalBuildingsText, targetBuildings);
        SetTextValue(totalWoodText, targetWood);
        SetTextValue(totalStoneText, targetStone);
        SetTextValue(totalFoodText, targetFood);
        SetTextValue(totalGoldText, targetGold);

        // 4. Cập nhật Text đánh giá dựa trên số ngày thực tế
        if (rankEvaluationText != null)
        {
            rankEvaluationText.text = GetRankEvaluation(targetDays);
        }
    }

    private string GetRankEvaluation(int days)
    {
        if (days >= 0 && days < 5)
        {
            return "<color=#FF5555>TẬP SỰ HOANG DÃ</color>\n(Kinh nghiệm còn non nớt, cần cố gắng hơn ở kiếp sau!)";
        }
        else if (days >= 5 && days < 10)
        {
            return "<color=#FFAA00>DÂN DU MỤC</color>\n(Bắt đầu quen với nhịp độ, bước đầu làm chủ vùng đất.)";
        }
        else if (days >= 10 && days < 20)
        {
            return "<color=#55FF55>NHÀ KIẾN THIẾT</color>\n(Khai phá xuất sắc! Bản làng của bạn đã vững mạnh.)";
        }
        else 
        {
            return "<color=#FFFF55> HUYỀN THOẠI KHẨN HOANG </color>\n(Kẻ chinh phục tối cao! Không gì có thể khuất phục bạn.)";
        }
    }

    private void SetTextValue(TMP_Text textComponent, int value, string suffix = "")
    {
        if (textComponent != null)
        {
            textComponent.text = value.ToString() + suffix;
        }
    }

    private bool stoneTextValid(TMP_Text txt) { return txt != null; }

    public void CloseEndGameWindow()
    {
        if (endGamePanelContainer != null)
        {
            endGamePanelContainer.SetActive(false);
            Debug.Log("[EndGameUI] ❌ Đã đóng bảng tổng kết. Chuyển sang chế độ xem Map.");
        }
    }

    private void OnClickRestart()
    {
        Time.timeScale = 1f;
        
        // Dọn dẹp file JSON lưu trữ thành tích cũ để chuẩn bị màn chơi mới
        JsonDataManager.ResetEndGameStats(); 

        SceneManager.LoadScene(gameplaySceneName);
    }

    private void OnClickMainMenu()
    {
        Time.timeScale = 1f;
        
        // Dọn dẹp file JSON lưu trữ thành tích cũ
        JsonDataManager.ResetEndGameStats(); 

        SceneManager.LoadScene(mainMenuSceneName);
    }
}