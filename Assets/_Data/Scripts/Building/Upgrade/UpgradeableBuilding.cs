using UnityEngine;

public class UpgradeableBuilding : MonoBehaviour
{
    [Header("Tên công trình")]
    public string buildingName = "Nhà Chính";

    [Header("Mảng chứa các Model Cấp 1, 2, 3...")]
    [SerializeField] private GameObject[] visualModels;

    public int CurrentLevel { get; private set; } = 0; // Scene instance level
    public int MaxLevel => visualModels != null ? visualModels.Length : 0;

    private void Start()
    {
        UpdateVisualModel();
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

    /// <summary>
    /// Bật/Tắt model theo chỉ số index.
    /// </summary>
    private void SetActiveModel(int index, bool active)
    {
        if (visualModels == null || index < 0 || index >= visualModels.Length) return;
        if (visualModels[index] != null)
            visualModels[index].SetActive(active);
    }

    /// <summary>
    /// Đồng bộ trạng thái Scene instance khi Start.
    /// </summary>
    private void UpdateVisualModel()
    {
        for (int i = 0; i < visualModels.Length; i++)
        {
            SetActiveModel(i, i == CurrentLevel);
        }
    }
}