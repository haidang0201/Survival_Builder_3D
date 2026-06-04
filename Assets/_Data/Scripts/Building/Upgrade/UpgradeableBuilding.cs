using UnityEngine;
using System.Collections;

public class UpgradeableBuilding : MonoBehaviour
{
    [System.Serializable]
    public struct UpgradeCost
    {
        public int woodCost;
        public int stoneCost;
        public int foodCost;
        public float upgradeDuration; // Thời gian nâng cấp tính bằng giây
    }

    [Header("Tên công trình")]
    public string buildingName = "Nhà Chính";

    [Header("Mảng chứa các Model Cấp 1, 2, 3...")]
    [SerializeField] private GameObject[] visualModels;

    [Header("Cấu hình chi phí nâng cấp (Phần tử 0 là từ Lv1 -> Lv2)")]
    [SerializeField] private UpgradeCost[] upgradeCosts;

    public int CurrentLevel { get; private set; } = 0; // Level hiện tại của công trình
    public int MaxLevel => visualModels != null ? visualModels.Length : 0;

    // Trạng thái kiểm tra xem nhà có đang trong quá trình nâng cấp không
    public bool IsUpgrading { get; private set; } = false;

    private void Start()
    {
        UpdateVisualModel();
    }

    // Hàm lấy chi phí cần thiết để lên cấp tiếp theo
    public UpgradeCost GetNextUpgradeCost()
    {
        if (CurrentLevel < upgradeCosts.Length)
        {
            return upgradeCosts[CurrentLevel];
        }
        // Trả về mặc định nếu đạt cấp tối đa
        return new UpgradeCost { woodCost = 0, stoneCost = 0, foodCost = 0, upgradeDuration = 0f }; 
    }

    /// <summary>
    /// Kích hoạt tiến trình đếm ngược nâng cấp bằng Coroutine
    /// </summary>
    public void StartUpgradeProcess()
    {
        if (IsUpgrading || CurrentLevel >= MaxLevel - 1) return;
        
        UpgradeCost nextCost = GetNextUpgradeCost();
        StartCoroutine(UpgradeRoutine(nextCost.upgradeDuration));
    }

    private IEnumerator UpgradeRoutine(float duration)
    {
        IsUpgrading = true;
        float timer = 0f;

        // Nếu panel nâng cấp của nhà này đang mở trên UI, hiển thị slider/text thời gian
        if (UIManager.Ins != null)
        {
            UIManager.Ins.UpdateUpgradeProgress(0f, duration);
        }

        // Vòng lặp đếm ngược thời gian nâng cấp theo thời gian thực
        while (timer < duration)
        {
            timer += Time.deltaTime;
            
            // Cập nhật giá trị hiển thị lên UI liên tục mỗi khung hình
            if (UIManager.Ins != null)
            {
                UIManager.Ins.UpdateUpgradeProgress(timer, duration);
            }
            yield return null;
        }

        // Sau khi chạy hết thời gian chờ -> Tiến hành đổi cấp bậc hình ảnh
        ExecuteLevelUp();

        IsUpgrading = false;

        // TẮT TEXT VÀ SLIDER THỜI GIAN KHI NÂNG CẤP XONG
        if (UIManager.Ins != null)
        {
            UIManager.Ins.HideUpgradeProgress();
            UIManager.Ins.RefreshUpgradePanel(this);
        }
    }

    /// <summary>
    /// Thực hiện thay đổi cấp độ và model thực tế
    /// </summary>
    private void ExecuteLevelUp()
    {
        if (CurrentLevel < MaxLevel - 1)
        {
            // Ẩn model hiện tại
            SetActiveModel(CurrentLevel, false);

            // Tăng level
            CurrentLevel++;

            // Hiện model mới
            SetActiveModel(CurrentLevel, true);

            Debug.Log($"[{buildingName}] Đã hoàn tất nâng cấp lên Level {CurrentLevel + 1}");
        }
    }

    private void SetActiveModel(int index, bool active)
    {
        if (visualModels == null || index < 0 || index >= visualModels.Length) return;
        if (visualModels[index] != null)
            visualModels[index].SetActive(active);
    }

    public void UpdateVisualModel()
    {
        if (visualModels == null) return;
        for (int i = 0; i < visualModels.Length; i++)
        {
            if (visualModels[i] != null)
                visualModels[i].SetActive(i == CurrentLevel);
        }
    }
}