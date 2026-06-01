using UnityEngine;

public class UpgradeableBuilding : MonoBehaviour
{
    [System.Serializable]
    public struct UpgradeCost
    {
        public int woodCost;
        public int stoneCost;
        public int foodCost;
    }

    [Header("Tên công trình")]
    public string buildingName = "Nhà Chính";

    [Header("Mảng chứa các Model Cấp 1, 2, 3...")]
    [SerializeField] private GameObject[] visualModels;

    [Header("Cấu hình chi phí nâng cấp (Phần tử 0 là từ Lv1 -> Lv2)")]
    [SerializeField] private UpgradeCost[] upgradeCosts;

    public int CurrentLevel { get; private set; } = 0; // Scene instance level
    public int MaxLevel => visualModels != null ? visualModels.Length : 0;

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
        // Trả về số 0 nếu lỡ vượt quá cấu hình hoặc nâng cấp miễn phí
        return new UpgradeCost { woodCost = 0, stoneCost = 0, foodCost = 0 }; 
    }

    /// <summary>
    /// Nâng cấp lên cấp tiếp theo trên Scene instance.
    /// </summary>
    public void NextLevel()
    {
        if (CurrentLevel < MaxLevel - 1)
        {
            // Ẩn model hiện tại
            SetActiveModel(CurrentLevel, false);

            // Tăng level
            CurrentLevel++;

            // Hiện model mới
            SetActiveModel(CurrentLevel, true);

            Debug.Log($"[{buildingName}] Nâng cấp lên Level {CurrentLevel + 1}");

            // Cập nhật UI panel (nếu đang mở)
            UIManager.Ins?.RefreshUpgradePanel(this);
        }
        else
        {
            Debug.Log($"[{buildingName}] Đã đạt cấp tối đa!");
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