using UnityEngine;

public class UpgradeableBuilding : MonoBehaviour
{
    [Header("Tên công trình")]
    public string buildingName = "Nhà Chính";

    [Header("Mảng chứa các Model Cấp 1, Cấp 2, Cấp 3...")]
    [Tooltip("Element 0 kéo Model Cấp 1, Element 1 kéo Model Cấp 2...")]
    [SerializeField] private GameObject[] visualModels;

    public int CurrentLevel { get; private set; } = 1;

    private void Start()
    {
        UpdateVisualModel(); // Vào game tự bật Model Cấp 1, ẩn các cấp khác
    }

    /// <summary> Hàm thực hiện đổi Model 3D khi bấm nút nâng cấp từ UI </summary>
    public void NextLevel()
    {
        // Kiểm tra nếu còn Model cấp tiếp theo thì mới cho tăng cấp
        if (CurrentLevel < visualModels.Length)
        {
            CurrentLevel++;
            UpdateVisualModel();
            Debug.Log($"Đã nâng cấp {buildingName} lên Cấp: {CurrentLevel}");
        }
        else
        {
            Debug.Log($"{buildingName} đã đạt cấp tối đa!");
        }
    }

    /// <summary> Thuật toán ẩn/hiện Model theo cấp độ </summary>
    private void UpdateVisualModel()
    {
        if (visualModels == null || visualModels.Length == 0) return;

        int targetIndex = CurrentLevel - 1; // Cấp 1 tương ứng phần tử 0 trong mảng
        for (int i = 0; i < visualModels.Length; i++)
        {
            if (visualModels[i] != null)
            {
                visualModels[i].SetActive(i == targetIndex); // Chỉ bật duy nhất Model của cấp hiện tại
            }
        }
    }
}